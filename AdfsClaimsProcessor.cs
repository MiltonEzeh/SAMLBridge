// <copyright file="AdfsClaimsProcessor.cs" company="Fujitsu">
// (c) Copyright 2006, 2007. All rights reserved.
// </copyright>
// <summary>Claim transformation module.</summary>

#region Using statements
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.Security.Permissions;
using System.Web.Security.SingleSignOn;
using System.Web.Security.SingleSignOn.Authorization;
#endregion

namespace Fujitsu.SamlBridge.Cct
{
    /// <summary>
    /// At the claim pre-processing stage, AdfsClaimsProcessor processes the incoming claims,
	/// replacing the value of the UPN claim with the user's resource domain UPN. It also
	/// carries out certain character translations in all claim values, in order to work around
	/// an ADFS bug.
    /// </summary>
    public class AdfsClaimsProcessor : IClaimTransform
    {
        #region Static members
		/// <summary>
		/// The value of the resource domain directory path registry value.
		/// </summary>
		private static string s_sResourceDomainDirectoryPath = null;
		/// <summary>
		/// The value of the resource domain UPN suffix registry value.
		/// </summary>
		private static string s_sResourceDomainUpnSuffix = null;
		#endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the AdfsClaimsProcessor class.
        /// </summary>
        public AdfsClaimsProcessor() {}
        #endregion

        #region Private Properties
        /// <summary>
		/// Gets the value of the resource domain directory path registry value.
        /// </summary>
        private string ResourceDomainDirectoryPath
        {
            get
            {
				if (s_sResourceDomainDirectoryPath == null) ReadRegistry();
                return s_sResourceDomainDirectoryPath;
            }
        }

		/// <summary>
		/// Gets the value of the resource domain UPN suffix registry value.
		/// </summary>
		private string ResourceDomainUpnSuffix
		{
			get
			{
				if (s_sResourceDomainUpnSuffix == null) ReadRegistry();
				return s_sResourceDomainUpnSuffix;
			}
		}
		#endregion

        #region Public Methods
        /// <summary>
        /// Implementation of TransformClaims method defined in the IClaimTransform interface.
        /// </summary>
        /// <param name="incomingClaims">Collection of incoming claims.</param>
        /// <param name="corporateClaims">Collection of corporate claims.</param>
        /// <param name="outgoingClaims">Collection of outgoing claims.</param>
        /// <param name="transformStage">
		/// Current stage in the transform process (pre- or post-processing).
		/// </param>
        /// <param name="issuer">The issuer of the request.</param>
        /// <param name="target">The requested resource.</param>
        [DirectoryServicesPermission(SecurityAction.Demand)]
        public void TransformClaims(
			ref SecurityPropertyCollection incomingClaims,
			ref SecurityPropertyCollection corporateClaims,
			ref SecurityPropertyCollection outgoingClaims,
			ClaimTransformStage transformStage,
			string issuer,
			string target)
        {
			// Create the SamlBridge CTM's trace listener.
			TextWriterTraceListener twtlSamlBridgeCtmTraceListener =
				new TextWriterTraceListener(Constants.PATH_SAML_BRIDGE_CTM_TRACE_FILE);

			if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
				Fujitsu.SamlBridge.Cct.Trace.WriteLine(
					Constants.TRCI_CLAIM_TRANSFORMATION_MODULE_ENTERED);
			
			// We only perform any mapping action in the pre-processing stage, so return
			// immediately if we are not at this stage.
			if (transformStage != ClaimTransformStage.PreProcessing)
			{
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_NOT_PREPROCESSING_STAGE);
				return;
			}

			// If incomingClaims is null, then there is nothing to map. corporateClaims is
			// probably populated, in which case ADFS has previously authenticated the user and
			// is reusing the already-mapped SAML assertion from the user's cookie.
			if ((incomingClaims != null) && (incomingClaims.Count > 0))
			{
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_INCOMING_CLAIMS_FOUND);

				// Validate that the SAML is as expected, i.e. that we have a UPN in the
				// incoming claims.
				int iUpnClaimIndex = FindClaim(incomingClaims, WebSsoClaimType.Upn);
				if (iUpnClaimIndex == -1)
				{
					if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceError)
						Fujitsu.SamlBridge.Cct.Trace.WriteLine(
							Constants.TRCE_NO_INCOMING_UPN_CLAIM_FOUND);

					throw new SamlBridgeCctException(Constants.EXC_NO_INCOMING_UPN_CLAIM);
				}

				SecurityProperty spUpnClaim = incomingClaims[iUpnClaimIndex];

				// Read out the UID from the UPN claim
				string sUid = spUpnClaim.Value;

				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_INCOMING_UPN_CLAIM_VALUE, sUid);

				// Remove the UPN claim from the collection - we will replace it with the real
				// UPN claim later.
				incomingClaims.RemoveAt(iUpnClaimIndex);

				#region ADFS AMPERSAND WORKAROUND
				// At the time of writing there appears to be a problem in ADFS whereby any
				// ampersand, less than, greater than, quote or apostrophe character contained
				// in the value of a claim in the authenticated SAML causes the ADFS Web Agent
				// to throw an exception.
				//
				// The following temporary workaround replaces any such characters in custom
				// claim values with a plain text equivalent, or, in the case of the quote and
				// apostrophe, nothing.
				for (int i = 0; i < incomingClaims.Count; i++)
				{
					SecurityProperty spThisClaim = incomingClaims[i];
					string sThisClaimValue = spThisClaim.Value;
					if (sThisClaimValue.IndexOf("&") != -1)
					{
						if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
							Fujitsu.SamlBridge.Cct.Trace.WriteLine(
								Constants.TRCI_PERFORMING_CLAIM_VALUE_SUBSTITUTION, "&",
								spThisClaim.Name);

						sThisClaimValue = sThisClaimValue.Replace("&", "and");
					}

					if (sThisClaimValue.IndexOf("<") != -1)
					{
						if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
							Fujitsu.SamlBridge.Cct.Trace.WriteLine(
								Constants.TRCI_PERFORMING_CLAIM_VALUE_SUBSTITUTION, "<",
								spThisClaim.Name);

						sThisClaimValue = sThisClaimValue.Replace("<", "lessthan");
					}

					if (sThisClaimValue.IndexOf(">") != -1)
					{
						if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
							Fujitsu.SamlBridge.Cct.Trace.WriteLine(
								Constants.TRCI_PERFORMING_CLAIM_VALUE_SUBSTITUTION, ">",
								spThisClaim.Name);

						sThisClaimValue = sThisClaimValue.Replace(">", "greaterthan");
					}

					if (sThisClaimValue.IndexOf("\"") != -1)
					{
						if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
							Fujitsu.SamlBridge.Cct.Trace.WriteLine(
								Constants.TRCI_PERFORMING_CLAIM_VALUE_SUBSTITUTION, "\"",
								spThisClaim.Name);

						sThisClaimValue = sThisClaimValue.Replace("\"", "");
					}

					if (sThisClaimValue.IndexOf("'") != -1)
					{
						if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
							Fujitsu.SamlBridge.Cct.Trace.WriteLine(
								Constants.TRCI_PERFORMING_CLAIM_VALUE_SUBSTITUTION, "'",
								spThisClaim.Name);

						sThisClaimValue = sThisClaimValue.Replace("'", "");
					}

					if (spThisClaim.Value != sThisClaimValue)
					{
						SecurityProperty spNewProperty =
							SecurityProperty.CreateCustomClaimProperty(
								spThisClaim.Name, sThisClaimValue);

						incomingClaims[i] = spNewProperty;
					}
				}
				#endregion

				// Determine how the user's UPN should be obtained. If the SamlBridge registry
				// value ResourceDomainDirectoryPath exists and has a non-empty value, then the
				// UPN is obtained by performing a local LDAP search based on the path defined
				// by the value of ResourceDomainDirectoryPath for a user whose UID corresponds
				// to the UID extracted from the incoming claim, and taking the UPN of this
				// user.
				//
				// Otherwise, if ResourceDomainDirectoryPath does not exist or has an empty
				// value, then the value of registry value ResourceDomainUpnSuffix is used to
				// create the UPN directly from the UID. If ResourceDomainUpnSuffix exists and
				// has a non-empty value, the UPN is generated by appending an ampersand
				// followed by the value of ResourceDomainUpnSuffix to the UID. If
				// ResourceDomainUpnSuffix does not exist or is empty, the unadorned UID is used
				// as the UPN.
				string sUpn = string.Empty;
				if (ResourceDomainDirectoryPath.Length != 0)
				{
					if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
						Fujitsu.SamlBridge.Cct.Trace.WriteLine(
							Constants.TRCI_UPN_BY_DIRECTORY_LOOKUP);

					sUpn = GetUpnByUid(sUid, incomingClaims);
				}
				else
				{
					if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
						Fujitsu.SamlBridge.Cct.Trace.WriteLine(
							Constants.TRCI_UPN_BY_UID_SUFFIXING, ResourceDomainUpnSuffix);

					if (ResourceDomainUpnSuffix.Length != 0)
					{
						sUpn = string.Format(
							Universal.SFMT_USER_PRINCIPAL_NAME,
							sUid,
							Universal.UPN_SUFFIX_PREFIX,
							ResourceDomainUpnSuffix);
					}
					else
					{
						sUpn = sUid;
					}
				}

				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_OUTGOING_RESOURCE_DOMAIN_UPN, sUpn);

				// Insert a new UPN claim into the incoming claims collection, containing the
				// newly generated UPN.
				incomingClaims.Add(SecurityProperty.CreateUserPrincipalNameProperty(sUpn));
			}
			else
			{
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceWarning)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCW_NO_INCOMING_CLAIMS_FOUND);
			}
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Searches a claims collection for a claim.
        /// </summary>
		/// <param name="p_spcollClaims">The claims collection to search.</param>
		/// <param name="p_wssoctClaim">The claim to search for.</param>
		/// <returns>
		/// The index of the requested claim, or CLAIM_INDEX_NULL if the claim could not be
		/// found.
		/// </returns>
        private static int FindClaim(
			SecurityPropertyCollection p_spcollClaims, WebSsoClaimType p_wssoctClaim)
        {
            for (int iThisClaim = 0; iThisClaim < p_spcollClaims.Count; iThisClaim++)
            {
                if (p_spcollClaims[iThisClaim].ClaimType == p_wssoctClaim)
                {
					return iThisClaim;
                }
            }
            return Constants.CLAIM_INDEX_NULL;
        }

		/// <summary>
		/// Searches a claims collection for a custom claim.
		/// </summary>
		/// <param name="p_spcollClaims">The claims collection to search.</param>
		/// <param name="p_sClaimName">The name of the custom claim to search for.</param>
		/// <returns>
		/// The index of the requested custom claim, or CLAIM_INDEX_NULL if the custom claim
		/// could not be found.
		/// </returns>
		private static int FindCustomClaim(
			SecurityPropertyCollection p_spcollClaims, string p_sClaimName)
		{
			for (int iThisClaim = 0; iThisClaim < p_spcollClaims.Count; iThisClaim++)
			{
				if
					((p_spcollClaims[iThisClaim].ClaimType == WebSsoClaimType.Custom)
					&&
					(p_spcollClaims[iThisClaim].Name.Equals(p_sClaimName)))
				{
					return iThisClaim;
				}
			}
			return Constants.CLAIM_INDEX_NULL;
		}

        /// <summary>
        /// Performs a lookup in the local LDAP directory base on the path contained in property
		/// ResourceDomainDirectoryPath. Retrieves the UPN of the user whose UID attribute
		/// matches that supplied.
        /// </summary>
        /// <param name="uid">The UID of the user whose UPN is required.</param>
        /// <returns>The UPD of the user with the specified UID.</returns>
        private string GetUpnByUid(
			string p_sUid, SecurityPropertyCollection p_spcollIncomingClaims)
        {
			// Find the claim index of the nhsIdCode custom claim.
			int iNhsIdCodeClaim =
				FindCustomClaim(p_spcollIncomingClaims, Constants.CUSTOMCLAIM_NHS_ID_CODE);

			// Get the value of the nhsIdCode claim - this will identify the resource domain AD
			// container in which the user resides.
			string sNhsIdCodeClaim = string.Empty;

			if (iNhsIdCodeClaim != Constants.CLAIM_INDEX_NULL)
			{
				sNhsIdCodeClaim = p_spcollIncomingClaims[iNhsIdCodeClaim].Value;

				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_NHS_ID_CODE_CLAIM_VALUE, sNhsIdCodeClaim);
			}
			else
			{
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceWarning)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCW_NO_NHS_ID_CODE_CLAIM_FOUND);
			}

			// Build the LDAP URL at which the search will be performed. This is based on the
			// path specified in the ResourceDomainDirectoryPath registry setting, further
			// restricted by the OU defined by the value of the nhsIdCode custom claim, if any.
			string sLdapUrl = Constants.PROTOCOL_LDAP + Constants.URL_SCHEME_TERMINATOR;

			// Add the OU restriction, if an nhsIdCode value was obtained.
			if (sNhsIdCodeClaim.Length != 0)
				sLdapUrl += string.Format(Constants.LDAPPATH_OU, sNhsIdCodeClaim);

			// Append the configured LDAP path.
			sLdapUrl += ResourceDomainDirectoryPath;

			if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
				Fujitsu.SamlBridge.Cct.Trace.WriteLine(
					Constants.TRCI_DIRECTORY_SEARCH_URL, sLdapUrl);

			// Create a DirectoryEntry object based on this LDAP URL.
			DirectoryEntry de = new DirectoryEntry(sLdapUrl);

			// Create a DirectorySearcher object based on this DirectoryEntry.
            using (DirectorySearcher ds = new DirectorySearcher(de))
            {
				// Set the search scope.
                ds.SearchScope = SearchScope.Subtree;

				// Set up a filter based on the required UID.
				string sSearchFilter = string.Format(Constants.LDAPFILTER_USER_UID, p_sUid);

				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_DIRECTORY_SEARCH_FILTER, sSearchFilter);
				
				ds.Filter = sSearchFilter;

				// Log BEGIN_DIRECTORY_SEARCH timing trace point.
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmTiming.Enabled)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCT_BEGIN_DIRECTORY_SEARCH);

				// Perform the search.
				SearchResult sr = ds.FindOne();

				// Log END_DIRECTORY_SEARCH timing trace point.
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmTiming.Enabled)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCT_END_DIRECTORY_SEARCH);

                if (sr == null)
                {
					if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceError)
						Fujitsu.SamlBridge.Cct.Trace.WriteLine(
							Constants.TRCE_NO_USER_FOUND_WITH_UID,
							p_sUid,
							sLdapUrl,
							sSearchFilter);

					string sExceptionMessage = string.Format(
						Constants.EXC_FAILED_TO_FIND_USER_WITH_SPECIFIED_UID,
						p_sUid,
						sLdapUrl,
						sSearchFilter);

                    throw new SamlBridgeCctException(sExceptionMessage);
                }
                else
                {
					// Extract the UPN from the search result.
					string sResourceDomainUpn =
						sr.Properties[Universal.ADFSCLAIM_USER_PRINCIPAL_NAME][0].ToString();

					if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
						Fujitsu.SamlBridge.Cct.Trace.WriteLine(
							Constants.TRCI_RESOURCE_DOMAIN_UPN_FROM_DIRECTORY,
							sResourceDomainUpn);

                    // Return the UPN.
					return sResourceDomainUpn;
                }
            }
        }

		private void ReadRegistry()
		{
			RegistryKey rkeySamlBridge = null;
			try
			{
				// Open the SamlBridge registry subkey.
				rkeySamlBridge =
					Registry.LocalMachine.OpenSubKey(Constants.REGKEY_SAMLBRIDGE, false);
			}
			catch (Exception eOpenKey)
			{
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceError)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCE_FAILED_TO_OPEN_REGISTRY_KEY,
						Constants.REGKEY_SAMLBRIDGE);

				throw new SamlBridgeCctException(
					Constants.EXC_FAILED_TO_OPEN_REGISTRY_KEY, eOpenKey);
			}

			try
			{
				// Read the resource domain directory path registry value.
				s_sResourceDomainDirectoryPath = (string) rkeySamlBridge.GetValue(
					Constants.REGVAL_SAMLBRIDGE_RESOURCE_DOMAIN_DIRECTORY_PATH,
					Constants.REGDEF_SAMLBRIDGE_RESOURCE_DOMAIN_DIRECTORY_PATH);

				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_RESOURCE_DOMAIN_DIRECTORY_PATH,
						s_sResourceDomainDirectoryPath);
			}
			catch (Exception eResourceDomainDirectoryPath)
			{
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceError)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCE_FAILED_TO_READ_REGISTRY_VALUE,
						Constants.REGVAL_SAMLBRIDGE_RESOURCE_DOMAIN_DIRECTORY_PATH,
						Constants.REGKEY_SAMLBRIDGE);

				string sResourceDomainDirectoryPathExcMsg = string.Format(
					Constants.EXC_FAILED_TO_READ_REGISTRY_VALUE,
					Constants.REGVAL_SAMLBRIDGE_RESOURCE_DOMAIN_DIRECTORY_PATH);

				throw new SamlBridgeCctException(
					sResourceDomainDirectoryPathExcMsg, eResourceDomainDirectoryPath);
			}

			try
			{
				s_sResourceDomainUpnSuffix = (string) rkeySamlBridge.GetValue(
					Constants.REGVAL_SAMLBRIDGE_RESOURCE_DOMAIN_UPN_SUFFIX,
					Constants.REGDEF_SAMLBRIDGE_RESOURCE_DOMAIN_UPN_SUFFIX);

				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceInfo)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCI_RESOURCE_DOMAIN_UPN_SUFFIX,
						s_sResourceDomainUpnSuffix);
			}
			catch (Exception eResourceDomainUpnSuffix)
			{
				if (Fujitsu.SamlBridge.Cct.Trace.s_trswSamlBridgeCtmPath.TraceError)
					Fujitsu.SamlBridge.Cct.Trace.WriteLine(
						Constants.TRCE_FAILED_TO_READ_REGISTRY_VALUE,
						Constants.REGVAL_SAMLBRIDGE_RESOURCE_DOMAIN_UPN_SUFFIX,
						Constants.REGKEY_SAMLBRIDGE);

				string sResourceDomainUpnSuffixExcMsg = string.Format(
					Constants.EXC_FAILED_TO_READ_REGISTRY_VALUE,
					Constants.REGVAL_SAMLBRIDGE_RESOURCE_DOMAIN_UPN_SUFFIX);

				throw new SamlBridgeCctException(
					sResourceDomainUpnSuffixExcMsg, eResourceDomainUpnSuffix);
			}

			// Close the registry key.
			rkeySamlBridge.Close();
		}
        #endregion
    }
}