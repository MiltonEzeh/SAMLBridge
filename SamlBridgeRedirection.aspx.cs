//-----------------------------------------------------------------------
// <copyright file="SamlBridgeRedirection.aspx.cs" company="Fujitsu">
//     (c) Copyright 2006.  All rights reserved.
// </copyright>
// <summary>
//     SamlBridgeRedirection.aspx.cs summary comment.
// </summary>
//-----------------------------------------------------------------------
#region Compiler Settings
#define TRACE
#endregion

#region using Directives
using System;
using System.Text;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Net;
using System.Diagnostics;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Xml;
using System.Collections.ObjectModel;
using System.Threading;
using Fujitsu.SamlBridge.Web;
//using Fujitsu.SamlBridge.Web.Fujitsu.SamlBridge.Web;
using Fujitsu.SamlBridge.Transformation;
using Fujitsu.SamlBridge.Transformation.Token;
using Fujitsu.SamlBridge;
#endregion
    /// <summary>
    /// ProxyBridgeRedirection is responsible for obtaining the SAML from the spine, transforming it into ADFS compliant
    /// SAML, and returning it to the resource partner's ADFS server.
    /// </summary>
    public partial class SamlBridgeRedirection : System.Web.UI.Page
    {
        #region Private Variables
        /// <summary>
        /// The settings reader
        /// </summary>
        private static AppSettingsReader s_appsr = new AppSettingsReader();

        /// <summary>
        /// Whether or not the token id is available.  If it is not available and the UID is, then calls to the spine
        /// will be bypassed and the fail-over LDAP store will be used.
        /// </summary>
        private bool tokenIdAvailable;
        #endregion

        #region Private Properties

        /// <summary>
        /// Gets the token id of the currently logged on user.
        /// </summary>
        /// <value>The user's token id.</value>
        private string TokenId
        {
            get {return Request.Form[Constants.HTMLFIELD_TOKEN_ID];}
        }

        /// <summary>
        /// Gets the error number reported by the ActiveX control, if one was raised.
        /// </summary>
        /// <value>The error number.</value>
        private string ErrorNumber
        {
            get {return Request.Form[Constants.HTMLFIELD_TOKEN_ERROR];}
        }

        /// <summary>
        /// Gets the description of the error reported by the ActiveX control, if one was raised.
        /// </summary>
        /// <value>The error description.</value>
        private string ErrorDescription
        {
            get {return Request.Form[Constants.HTMLFIELD_TOKEN_ERROR_DESCRIPTION];}
        }

        /// <summary>
        /// Gets the realm for the WS-Federation request.
        /// </summary>
        /// <value>The WS-Federation realm.</value>
        private string FederationRealm
        {
            get {return Request.QueryString[Federation.RealmParameter];}
        }

        /// <summary>
        /// Gets the realm for the WS-Federation context.
        /// </summary>
        /// <value>The WS-Federation context.</value>
        private string FederationContext
        {
            get {return Request.QueryString[Federation.ContextParameter];}
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Handles the loading of the page
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        protected void Page_Load(object sender, EventArgs e)
        {
			try
			{
				// Retrieve the parameters from the request's QueryString and Form information
				if (!this.ExtractParameters())
				{
					return;
				}

				string saml = null;
				if (this.tokenIdAvailable)
				{
					// Attempt to get the SAML from the Spine - this method will fail over if
					// necessary.
					saml = this.ObtainAndTransformSpineSaml();
				}
				else
				{
					/*
					 * No token ID is available. This implies that the IATicket object received
					 * a response from the LIA ticket API indicating that the LIA was unable to
					 * contact the Spine. This means we are now in failover mode 3. Therefore
					 * we invoke decoupled operation by transferring control to
					 * SamlBridgeDecoupled.aspx.
					 */
					Server.Transfer(Constants.ASPX_SAML_BRIDGE_DECOUPLED, true);
					return;
				}

				if (saml != null)
				{
					this.ReturnSaml(saml);
				}
				else
				{
					this.ReportError(
						SamlBridgeErrorCode.SamlBridgeInternalError,
						SamlBridgeErrorCodeHelper.ERRINFO_SPINE_FAILOVER_FAILED);
				}
			}
			catch (SpineException ex)
			{
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this,
						Constants.TRCE_SPINE_RETURNED_FAULT_RESPONSE,
						ex.FaultCode,
						ex.FaultDescription);

				ReportError(SamlBridgeErrorCode.SamlBridgeSpineFaultResponse, ex.Message);
			}
			catch (TransformationXPathException ex)
			{
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_SAML_TRANSFORMATION_XPATH_EXCEPTION, ex.XPath);

				string additionalInformation = string.Format(
					SamlBridgeErrorCodeHelper.ERRINFO_TRANSFORMATION_XPATH_EXCEPTION,
					ex.ToString(),
					Environment.NewLine,
					ex.XPath,
					ex.SpineSamlToken);

				this.ReportError(SamlBridgeErrorCode.SamlTransformationError, additionalInformation);
			}
			catch (TransformationException ex)
			{
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_SAML_TRANSFORMATION_TRANSFORMATION_EXCEPTION);

				this.ReportError(
					SamlBridgeErrorCode.SamlTransformationError,
					string.Format(
						SamlBridgeErrorCodeHelper.ERRINFO_TRANSFORMATION_EXCEPTION,
						ex.ToString(),
						Environment.NewLine,
						ex.SpineSamlToken));
			}
			catch (ThreadAbortException)
			{
				/* The use of the Server.Transfer() method results in a ThreadAbortException
				 * being raised in the page being transferred from. This is documented in
				 * Microsoft Knowledge Base article number 312629. We therefore catch this
				 * exception here to prevent it generating a spurious error report from the
				 * handler for generic exceptions. */
			}
			catch (Exception ex)
			{
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_REDIRECT_PAGE_EXCEPTION, ex.Message);

				this.ReportError(SamlBridgeErrorCode.SamlBridgeInternalError, ex.ToString());
			}
			finally
			{
				// Log END_REDIRECT_PAGE timing trace point.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
					Fujitsu.SamlBridge.Trace.WriteLine(this, Constants.TRCT_END_REDIRECT_PAGE);
			}
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Reports the given error to the client, and logs it to the event log.
        /// </summary>
        /// <param name="errorCode">The error code to report.</param>
        private void ReportError(SamlBridgeErrorCode errorCode)
        {
            this.ReportError(errorCode, null);
        }

        /// <summary>
        /// Reports the given error to the client, and logs it to the event log.
        /// </summary>
		/// <param name="p_enumErrorCode">The error code to report.</param>
		/// <param name="p_sAdditionalInfo">
		///	Any additional information to be presented. This will only be stored in the event
		///	log, not be sent back to the user.
		/// </param>
        private void ReportError(SamlBridgeErrorCode p_enumErrorCode, string p_sAdditionalInfo)
        {
			// First log the error in the Windows event log.
			SamlBridgeErrorCodeHelper.LogErrorInEventLog(p_enumErrorCode, p_sAdditionalInfo);

			// Build the URL of the SamlBridge error page.
			string sSamlBridgeErrorPageUrl =
				Constants.ASPX_SAML_BRIDGE_ERROR +
				Universal.URL_QUERYSTRING_PARAMETER_LIST_SEPARATOR +
				Constants.QSPARAM_ERROR_SOURCE +
				Universal.URL_QUERYSTRING_PARAMETER_VALUE_SEPARATOR +
				Constants.ERRSRC_SAML_BRIDGE +
				Universal.URL_QUERYSTRING_PARAMETER_SEPARATOR +
				Constants.QSPARAM_ERROR_CODE +
				Universal.URL_QUERYSTRING_PARAMETER_VALUE_SEPARATOR +
				p_enumErrorCode.ToString(Universal.NFMT_DECIMAL);

			// Transfer control to the SamlBridge error page.
			Server.Transfer(sSamlBridgeErrorPageUrl);
        }

		/// <summary>Writes a SamlBridge warning entry to the event log.</summary>
		/// <param name="p_enumErrorCode">The SamlBridge error code to report.</param>
		/// <param name="p_sAdditionalInfo">Any additional information to be logged.</param>
		private void ReportWarning(SamlBridgeErrorCode p_enumErrorCode)
		{
			// Log the warning in the Windows event log.
			SamlBridgeErrorCodeHelper.LogWarningEvent(p_enumErrorCode);
		}

        /// <summary>
        /// Extracts into private fields the parameters of the request from the querystring and form information
        /// </summary>
        /// <returns>Whether or not the requred parameters were specified.</returns>
        private bool ExtractParameters()
        {
            // Check we have a token id
            if (String.IsNullOrEmpty(this.TokenId))
            {
				// No token ID is was obtained from the client.
				string errorNumber = this.ErrorNumber;

				// Log a warning to the trace log.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceWarning)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCW_NO_TOKEN_ID_OBTAINED_FROM_CLIENT, errorNumber);

				this.tokenIdAvailable = false;

				List<int> lstDecoupledModeSignalCodes =
					Fujitsu.SamlBridge.Configuration.GetDecoupledModeSignalCodes();

				if (lstDecoupledModeSignalCodes == null)
				{
					// A null value returned from GetDecoupledSignalCodes indicates an invalid
					// value in the DecoupleModeSignalCodes application setting. Log a warning
					// to the trace log, but do not abort the application.
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceWarning)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCW_DECOUPLED_MODE_SIGNAL_CODES_INVALID);
				}

				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				{
					// Trace the list of decoupled mode signal codes obtained.
					foreach (int iThisCode in lstDecoupledModeSignalCodes)
					{
						Fujitsu.SamlBridge.Trace.WriteLine(
							this,
							Constants.TRCI_DECOUPLED_MODE_SIGNAL_CODE,
							iThisCode.ToString());
					}
				}

                if
					((errorNumber != null)
					&&
					(errorNumber != string.Empty)
					&&
					(lstDecoupledModeSignalCodes.Contains(int.Parse(errorNumber))))
                {
                    // The ticket API has returned a result code that signals decoupled mode.
					// Allow processing to continue. Failover will be invoked when the lack of
					// a token is detected.
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCW_DECOUPLED_MODE_SIGNAL_CODE_RECEIVED);
				}
                else
                {
                    // Some other error was reported by the activex control, so report it to the user
                    this.ReportMissingTokenInformation();
                    return false;
                }
            }
            else
            {
				// A token ID was obtained from the client.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCI_TOKEN_ID_OBTAINED_FROM_CLIENT, TokenId);

				// Setting this flag to true indicates to the code that we can try communicating with the spine.
                this.tokenIdAvailable = true;
            }

            // Check that a federation realm was obtained from the querystring.
            if (String.IsNullOrEmpty(this.FederationRealm))
            {
                /* No federation realm is present, implying that the request hasn't come from an
				 * ADFS resource. Log to the trace log and raise a SamlBridge error. */
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_NO_FEDERATION_REALM_IN_QUERYSTRING);

				this.ReportError(SamlBridgeErrorCode.AdfsQueryStringNotPresent);
                return false;
            }

			// Check that a federation context was obtained from the querystring.
			if (String.IsNullOrEmpty(this.FederationContext))
			{
				/* No federation context is present, implying that the request hasn't come from
				 * an ADFS resource. Log to the trace log and raise a SamlBridge error. */
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_NO_FEDERATION_CONTEXT_IN_QUERYSTRING);

				this.ReportError(SamlBridgeErrorCode.AdfsQueryStringNotPresent);
				return false;
			}

            // All the required parameters were found.
            return true;
        }

        /// <summary>
        /// Builds the hidden HTML fields required for posting back to the ADFS resource server
        /// </summary>
        /// <param name="spineSaml">The saml returned by the spine</param>
        private void ReturnSaml(string spineSaml)
        {
            string returnAddress;

            try
            {
                // Read the return address from the app settings, based on the name of the realm the request came from
                returnAddress = (string) s_appsr.GetValue(this.FederationRealm, typeof(string));
            }
            catch (InvalidOperationException ex)
            {
				/* No return URL corresponding to the federation realm of the current request is
				 * configured in the application settings. Log this to the trace log. */
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_UNABLE_TO_DETERMINE_RETURN_URL, FederationRealm);

				// Raise a SamlBridge error.
				this.ReportError(
					SamlBridgeErrorCode.UnableToDetermineReturnAddress,
					string.Format(this.FederationRealm, Environment.NewLine, ex.ToString()));

				return;
            }

            // Output the form HTML.
            this.RedirectFormPlaceholder.Text = string.Format(
				Constants.HTMLTAG_SAMLBRIDGEREDIRECTION_ASPX_FORM,
                returnAddress,
                HttpUtility.HtmlEncode(spineSaml),
                this.FederationContext);
        }

        /// <summary>
        /// Reports the fact that the token id information was not posted to the page
        /// </summary>
        private void ReportMissingTokenInformation()
        {
            string errorDescription = this.ErrorDescription;
            
			errorDescription =
				(String.IsNullOrEmpty(errorDescription)
				? Constants.ERRDESC_NO_ERROR_INFORMATION_AVAILABLE : errorDescription);

            string errorNumber = this.ErrorNumber;
            if (errorNumber != null)
            {
                if (errorNumber == Constants.IATICKET_E_FAILED_TO_LOAD_TICKET_API)
                {
                    /* This error code indicates that the local identity agent is not installed
					 * on the client. In this case we redirect the user to the manual sign-on
					 * application environment. */
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceWarning)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCW_LOCAL_IDENTITY_AGENT_NOT_DETECTED);

					Response.Redirect(
						(string) s_appsr.GetValue(
							Constants.APPKEY_MANUAL_SIGNON_ADDRESS, typeof(string)));

                    return;
                }
				else if (errorNumber == Constants.IATICKET_E_FAILED_TO_INITIALISE_TICKET_API)
				{
					/* This error code indicates that the ticket API on the client machine could
					 * not be initialised In this case we redirect the user to the manual
					 * sign-on application environment. */
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceWarning)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCW_FAILED_TO_INITIALIZE_TICKET_API);

					Response.Redirect(
						(string) s_appsr.GetValue(
							Constants.APPKEY_MANUAL_SIGNON_ADDRESS, typeof(string)));

					return;
				}
                else
                {
                    // Concatenate the error number and description together.
					errorDescription = string.Format(
						Constants.SFMT_ERROR_NUMBER_DESCRIPTION,
						errorNumber.ToString(),
						errorDescription);
                }
            }

            this.ReportError(SamlBridgeErrorCode.UnableToDetermineTokenId, errorDescription);
        }

        /// <summary>
        /// Downloads the SAML for a given tokenId from the spine
        /// </summary>
        /// <param name="tokenId">The id of the token to download</param>
        /// <returns>The downloaded saml, or null if an error occurred</returns>
        private string DownloadSpineSaml(string tokenId)
        {
            // Get the address of the spine assertion page
            string spineAssertionAddress =
				(string) s_appsr.GetValue(
					Constants.APPKEY_SPINE_ASSERTION_ADDRESS, typeof(string));

            string spineSaml = null;
            int attemptCount = 0;

            int maxAttempts =
				(int) s_appsr.GetValue(
					Constants.APPKEY_MAX_SPINE_CONNECTION_ATTEMPTS, typeof(int));

            StringBuilder errorBuilder = null;
            bool spineSuccess = false;

            /* The token ID may contain characters that are invalid within a URL. Therefore we
             * need to URL-encode the token ID before building it into the Spine URL. */
            string sUrlEncodedTokenId = HttpUtility.UrlEncode(tokenId);
			
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCI_URL_ENCODED_TOKEN_ID, sUrlEncodedTokenId);

            // Build the URL to be used to access the Spine assertion service.
            string sSpineAssertionUrl =
                spineAssertionAddress                               +
                Universal.URL_QUERYSTRING_PARAMETER_LIST_SEPARATOR  +
                Constants.QSPARAM_SPINEASSERTIONSERVICE_TOKEN       +
                Universal.URL_QUERYSTRING_PARAMETER_VALUE_SEPARATOR +
                sUrlEncodedTokenId;
			
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCI_SPINE_ASSERTION_SERVICE_URL, sSpineAssertionUrl);

            do
            {
				// Increment the connection attempt counter.
				++attemptCount;

                // Use a web client to download the result of the spine assertion page
                using (WebClient webClient = new WebClient())
                {
                    try
                    {
						// Log BEGIN_SPINE_REQUEST timing trace point.
						if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
							Fujitsu.SamlBridge.Trace.WriteLine(
								this, Constants.TRCT_BEGIN_SPINE_REQUEST);

						// Send SAML request to the Spine.
						spineSaml = webClient.DownloadString(sSpineAssertionUrl);

						// Log END_SPINE_REQUEST timing trace point.
						if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
							Fujitsu.SamlBridge.Trace.WriteLine(
								this, Constants.TRCT_END_SPINE_REQUEST);

						spineSuccess = true;
                    }
                    catch (WebException ex)
                    {
						if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceWarning)
							Fujitsu.SamlBridge.Trace.WriteLine(
								this,
								Constants.TRCW_SPINE_ASSERTION_SERVICE_REQUEST_FAILED,
								((int) attemptCount).ToString(Universal.NFMT_DECIMAL));

						if (errorBuilder == null)
                        {
                            errorBuilder = new StringBuilder(ex.Message.Length * 4);
                        }

                        errorBuilder.AppendFormat(
							Constants.SFMT_SPINE_CONNECTION_ATTEMPT, attemptCount);

                        errorBuilder.AppendLine(ex.Message);
                    }
                }
            }
            while (!spineSuccess && (attemptCount < maxAttempts));

            if (spineSuccess)
            {
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				{
					// Log the fact that the assertion service request succeeded.
					Fujitsu.SamlBridge.Trace.WriteLine(
						this,
						Constants.TRCI_SPINE_ASSERTION_SERVICE_REQUEST_SUCCEEDED,
						((int) (attemptCount)).ToString(Universal.NFMT_DECIMAL));

					// Log the SAML received from the Spine.
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCI_RECEIVED_SPINE_SAML, spineSaml);
				}				
                return spineSaml;
            }
            else
            {
				// We were unable to obtain a SAML token from the Spine after the configured
				// maximum number of attempts. Log an error trace message to record this.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this,
						Constants.TRCE_FAILED_TO_OBTAIN_SPINE_SAML,
						maxAttempts.ToString());

				// Now determine whether to attempt local SDS failover. If a local SDS host is
				// configured in the application configuration file, then we will attempt Spine
				// failover, otherwise we will simply report the error back to the user.
				string sLocalSDSHost = GetApplicationSetting(
					Constants.APPKEY_LOCAL_SDS_HOST, typeof(string), false);

				if ((sLocalSDSHost != null) && (sLocalSDSHost.Length != 0))
				{
					// A local SDS is configured, so we will attempt failover.
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCI_ATTEMPTING_SPINE_FAILOVER);

					// Report a SamlBridge warning to record the Spine access failure in the
					// Windows event log.
					ReportWarning(SamlBridgeErrorCode.UnableToContactSpine);

					// Return null to force Spine failover.
					return null;
				}
				else
				{
					// No local SDS is configured, so report a SamlBridge error to the user.
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCE_UNABLE_TO_ATTEMPT_SPINE_FAILOVER);

					// Report the error to the user - this will invoke a server transfer to the
					// SamlBridge error page.
					ReportError(
						SamlBridgeErrorCode.UnableToContactSpine, errorBuilder.ToString());

					// Return null to satisfy the compiler.
					return null;
				}
            }
        }

        /// <summary>
        /// Obtains and transforms the spine SAML to ADFS compliant SAML.  If the spine cannot be contacted
        /// then the LDAP fail-over will automatically be contacted.
        /// </summary>
        /// <returns>The user's SAML, or null if none could be obtained.</returns>
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand)]
        private string ObtainAndTransformSpineSaml()
        {
            // Fetch the SAML from the spine
            string spineSaml = this.DownloadSpineSaml(this.TokenId);

            if (spineSaml != null)
            {
				// Get the Spine error response settings from the configuration file.
				string sSpineErrorNodeXPath = GetApplicationSetting(
						Constants.APPKEY_SPINE_ERROR_NODE_XPATH, typeof(string), false);

				string sSpineErrorCodeXPath = GetApplicationSetting(
						Constants.APPKEY_SPINE_ERROR_CODE_XPATH, typeof(string), false);
				
				string sSpineErrorDescXPath = GetApplicationSetting(
						Constants.APPKEY_SPINE_ERROR_DESC_XPATH, typeof(string), false);

				// Log BEGIN_SAML_TRANSFORM timing trace point.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCT_BEGIN_SAML_TRANSFORM);

				// Transform the Spine SAML into ADFS (SAML 1.1) compliant SAML.
				string sAdfsSaml = TokenTransformer.Transform(
                    spineSaml,
                    this.FederationRealm,
                    (string) s_appsr.GetValue(
						Constants.APPKEY_ACCOUNT_URI, typeof(string)),
                    (string) s_appsr.GetValue(
						Constants.APPKEY_AUTHENTICATION_METHOD, typeof(string)),
                    (string) s_appsr.GetValue(
						Constants.APPKEY_ACCOUNT_SIGNING_CERTIFICATE, typeof(string)),
                    (string) s_appsr.GetValue(
						Constants.APPKEY_UID_XPATH, typeof(string)),
                    (string) s_appsr.GetValue(
						Constants.APPKEY_ATTRIBUTES_ROOT_XPATH, typeof(string)),
                    (long) s_appsr.GetValue(
						Constants.APPKEY_TOKEN_LIFETIME_IN_SECONDS, typeof(long)),
					sSpineErrorNodeXPath,
					sSpineErrorCodeXPath,
					sSpineErrorDescXPath);

				// Log END_SAML_TRANSFORM timing trace point.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCT_END_SAML_TRANSFORM);

				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCI_ADFS_SAML, sAdfsSaml);

				return sAdfsSaml;
            }
            else
            {
                /* We were unable to contact the spine, so we are now in failover mode 2. We
                 * therefore invoke decoupled operation by transferring control to
                 * SamlBridgeDecoupled.aspx. */
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCI_TRANSFERRING_TO_DECOUPLED_PAGE);

				Server.Transfer(Constants.ASPX_SAML_BRIDGE_DECOUPLED, true);
                return null;
            }
        }

		/// <summary>
		///		Gets the value of an application setting from the Web.config file.
		/// </summary>
		/// <param name="p_sKey">
		///		The application setting to retrieve.
		/// </param>
		/// <param name="p_typType">
		///		Type of the object to be returned.
		/// </param>
		/// <param name="p_bMandatory">
		///		Whether the setting is mandatory.
		/// </param>
		/// <returns>
		///		The value of the specified application setting.
		///	</returns>
		private string GetApplicationSetting(string p_sKey, Type p_typType, bool p_bMandatory)
		{
			string sValue = Universal.S_EMPTY;
			try
			{
				sValue = (string) s_appsr.GetValue(p_sKey, p_typType);
			}
			catch (InvalidOperationException) { }

			if (p_bMandatory && sValue.Length == 0)
			{
				// Missing mandatory application setting - log to event log.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_MISSING_MANDATORY_APPLICATION_SETTING);

				// Raise a SamlBridge error.
				ReportError(SamlBridgeErrorCode.SamlBridgeAppSettingMissing, p_sKey);
			}

			return sValue;
		}
        #endregion
    }