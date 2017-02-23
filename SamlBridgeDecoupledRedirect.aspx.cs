#region Compiler Settings
#define TRACE
#endregion

#region using Directives
using System;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Web;
using System.DirectoryServices;
using Fujitsu.SamlBridge.Web;
using Fujitsu.SamlBridge;
#endregion

public partial class SamlBridgeDecoupledRedirect : System.Web.UI.Page
{
    /// <summary>
    /// Gets the realm for the WS-Federation request.
    /// </summary>
    /// <value>The WS-Federation realm.</value>
    private string FederationRealm
    {
        get { return Request.QueryString[Federation.RealmParameter]; }
    }

    /// <summary>
    /// Gets the realm for the WS-Federation context.
    /// </summary>
    /// <value>The WS-Federation context.</value>
    private string FederationContext
    {
        get { return Request.QueryString[Federation.ContextParameter]; }
    }

    // Create an application settings reader object.
    private static AppSettingsReader s_appsr = new AppSettingsReader();

    protected void Page_Load(object sender, EventArgs e)
    {
        // Get a SAML token from the failover directory.
        string sSamlToken = this.CreateSamlAssertion();

        // Forward the SAML token to the ADFS server.
        this.ReturnSaml(sSamlToken);
    }

    // <summary>
    // Obtains the fail over SAML by querying the the failover LDAP store with the UID and
    // building a new token.
    // </summary>
    // <returns>The SAML token constructed from information found for the user in the failover
    // LDAP store. Null if the user could not be located in the failover LDAP store.
    // </returns>

    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand)]

    private string CreateSamlAssertion()
    {
        // Get the user ID from the querystring and save it in a local variable.
        string sUid =
			Request.QueryString.Get(Constants.QSPARAM_SAMLBRIDGEDECOUPLEDREDIRECTASPX_UID);

		// Look up the specified UID in the local SDS.
        DirectoryEntry deNhsPerson = GetLocalSDSEntry(sUid);

		// If the user could not be located in the local SDS, return immediately.
		if (deNhsPerson == null) return null;

		// Create an attribute collection from which to build the SAML assertion.
		Collection<Fujitsu.SamlBridge.Transformation.Token.Attribute> collSamlAttributes =
			new Collection<Fujitsu.SamlBridge.Transformation.Token.Attribute>();

		// Add a UID attribute to the attribute collection.
		collSamlAttributes.Add(
			new Fujitsu.SamlBridge.Transformation.Token.Attribute(
				Constants.SAMLATTR_UID, sUid));

		// Get the nhsPerson object's property collection.
		PropertyCollection pcollNhsPersonProperties = deNhsPerson.Properties;

		// Get the values of the nhsPerson object's nhsOcsPrCode property.
		PropertyValueCollection pvcollNhsPersonOcsPrCode = (PropertyValueCollection)
			(pcollNhsPersonProperties[Constants.SDSPROP_NHS_OCS_PR_CODE]);

		// Create SAML attributes for the nhsOcsPrCode property values.
		AddSamlAttributes(
			collSamlAttributes, Constants.SAMLATTR_NHS_OCS_PR_CODE, pvcollNhsPersonOcsPrCode);

		// Get the child of the nhsPerson object - this is the nhsOrgPerson object.
		IEnumerator ienumNhsPersonChildren = deNhsPerson.Children.GetEnumerator();

		DirectoryEntry deNhsOrgPerson = null;
		if (ienumNhsPersonChildren.MoveNext())
		{
			deNhsOrgPerson = (DirectoryEntry) ienumNhsPersonChildren.Current;

			PropertyCollection pcollNhsOrgPersonProperties = deNhsOrgPerson.Properties;

			// Get the value of the nhsOrgPerson object's nhsIdCode property.
			PropertyValueCollection pvcollNhsOrgPersonNhsIdCode = (PropertyValueCollection)
				(pcollNhsOrgPersonProperties[Constants.SDSPROP_NHS_ID_CODE]);

			// Create SAML attributes for the nhsIdCode property values.
			AddSamlAttributes(
				collSamlAttributes,
				Constants.SAMLATTR_NHS_ID_CODE,
				pvcollNhsOrgPersonNhsIdCode);

			// Get the child of the nhsOrgPerson object - this is the nhsOrgPersonRole object.
			IEnumerator ienumNhsOrgPersonChildren = deNhsOrgPerson.Children.GetEnumerator();

			DirectoryEntry deNhsOrgPersonRole = null;
			if (ienumNhsOrgPersonChildren.MoveNext())
			{
				deNhsOrgPersonRole = (DirectoryEntry) ienumNhsOrgPersonChildren.Current;

				PropertyCollection pcollNhsOrgPersonRoleProperties =
					deNhsOrgPersonRole.Properties;

				// Get the value of the nhsOrgPersonRole object's nhsAreaOfWork property.
				PropertyValueCollection pvcollNhsOrgPersonRoleNhsAreaOfWork =
					(PropertyValueCollection)
						(pcollNhsOrgPersonRoleProperties[Constants.SDSPROP_NHS_AREA_OF_WORK]);

				// Create SAML attributes for the nhsAreaOfWork property values.
				AddSamlAttributes(
					collSamlAttributes,
					Constants.SAMLATTR_NHS_AREA_OF_WORK,
					pvcollNhsOrgPersonRoleNhsAreaOfWork);

				// Get the value of the nhsOrgPersonRole object's nhsJobRole property.
				PropertyValueCollection pvcollNhsOrgPersonRoleNhsJobRole =
					(PropertyValueCollection)
						(pcollNhsOrgPersonRoleProperties[Constants.SDSPROP_NHS_JOB_ROLE]);

				// Create SAML attributes for the nhsJobRole property values.
				AddSamlAttributes(
					collSamlAttributes,
					Constants.SAMLATTR_NHS_JOB_ROLE,
					pvcollNhsOrgPersonRoleNhsJobRole);
			}
		}

        // Use the SAML factory to create the SAML token.
		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			System.Diagnostics.Trace.Write(Constants.TRCI_CREATING_SAML_TOKEN);
		
		XmlDocument token = Fujitsu.SamlBridge.Transformation.Token.SamlFactory.Create(
            new Uri(this.FederationRealm),
            new Uri((string) s_appsr.GetValue(Constants.APPKEY_ACCOUNT_URI, typeof(string))),
            (string) s_appsr.GetValue(Constants.APPKEY_AUTHENTICATION_METHOD, typeof(string)),
            sUid,
			collSamlAttributes,
            (string) s_appsr.GetValue(
                Constants.APPKEY_ACCOUNT_SIGNING_CERTIFICATE, typeof(string)),
            (long) s_appsr.GetValue(Constants.APPKEY_TOKEN_LIFETIME_IN_SECONDS, typeof(long)));

		string sSamlToken = token.OuterXml;

		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			System.Diagnostics.Trace.Write(Constants.TRCI_CREATED_SAML_TOKEN, sSamlToken);
		
		return sSamlToken;
    }

    /// <summary>
    /// Looks up a UID in the local SDS and returns the corresponding directory entry.
    /// </summary>
    /// <param name="p_sUid">The UID to look up.</param>
    /// <returns>
	/// A DirectoryEntry object corresponding to the nhsPerson object in the local SDS whose UID
	/// was specified, or null if no such object could be found.
    ///</returns>
    private DirectoryEntry GetLocalSDSEntry(string p_sUid)
    {
        // Get the set of application settings relating to the local SDS.
        string sLocalSDSHost = GetApplicationSetting(
            Constants.APPKEY_LOCAL_SDS_HOST, typeof(string), true);
        string sLocalSDSPort = GetApplicationSetting(
            Constants.APPKEY_LOCAL_SDS_PORT, typeof(string), true);
        string sLocalSDSPath = GetApplicationSetting(
            Constants.APPKEY_LOCAL_SDS_PATH, typeof(string), true);
		string sLocalSDSFilter = GetApplicationSetting(
			Constants.APPKEY_LOCAL_SDS_FILTER, typeof(string), true);
		string sLocalSDSUsername = GetApplicationSetting(
            Constants.APPKEY_LOCAL_SDS_USERNAME, typeof(string), true);
        string sLocalSDSPassword = GetApplicationSetting(
            Constants.APPKEY_LOCAL_SDS_PASSWORD, typeof(string), true);
        string sLocalSDSAuthType = GetApplicationSetting(
            Constants.APPKEY_LOCAL_SDS_AUTH_TYPE, typeof(string), true);

		// Insert the UID into the LDAP search filter string.
		sLocalSDSFilter = string.Format(sLocalSDSFilter, p_sUid);

		// Log the LDAP search filter to be used.
		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			Fujitsu.SamlBridge.Trace.WriteLine(
				this, Constants.TRCI_LOCAL_SDS_SEARCH_FILTER, sLocalSDSFilter);

        // Now assemble the complete LDAP URL.
        string sLocalSDSUrl =
            Universal.URL_PROTOCOL_LDAP     +
            Universal.URL_SCHEME_TERMINATOR +
            sLocalSDSHost                   +
            Universal.URL_PORT_INITIATOR    +
            sLocalSDSPort                   +
            Universal.URL_HOST_TERMINATOR   +
			sLocalSDSPath;

		// Log the LDAP URL to be used.
		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			Fujitsu.SamlBridge.Trace.WriteLine(
				this, Constants.TRCI_LOCAL_SDS_URL, sLocalSDSUrl);

        // Set up the specified authentication type.
        AuthenticationTypes authtyp = AuthenticationTypes.Secure;

        if (sLocalSDSAuthType.Equals(Universal.AUTHTYPE_ANONYMOUS))
            authtyp = AuthenticationTypes.Anonymous;
        else if (sLocalSDSAuthType.Equals(Universal.AUTHTYPE_NONE))
            authtyp = AuthenticationTypes.None;

		// Log the credentials we are using to access the local SDS.
		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			Fujitsu.SamlBridge.Trace.WriteLine(
				this,
				Constants.TRCI_ACCESSING_LOCAL_SDS_WITH_CREDENTIALS,
				sLocalSDSUsername,
				sLocalSDSPassword);

		// Create a DirectoryEntry object around the base LDAP URL.
		DirectoryEntry deBase =
			new DirectoryEntry(sLocalSDSUrl, sLocalSDSUsername, sLocalSDSPassword, authtyp);

		// Log the authentication type being used.
		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			Fujitsu.SamlBridge.Trace.WriteLine(
				this,
				Constants.TRCI_LOCAL_SDS_AUTH_TYPE,
				deBase.AuthenticationType.ToString());

		// Create a DirectorySearcher object based on the base path and the filter.
		DirectorySearcher ds = new DirectorySearcher(deBase, sLocalSDSFilter);

		// Set the search scope to one level.
		ds.SearchScope = SearchScope.OneLevel;

		// Perform the search.
		SearchResult srNhsPerson = null;
		try
		{
			srNhsPerson = ds.FindOne();
		}
		catch (Exception e)
		{
			// Log the exception in the trace log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this,
					Constants.TRCE_LOCAL_SDS_SEARCH_EXCEPTION,
					e.GetType().Name,
					e.Message);
		}

		if (srNhsPerson == null)
		{
			// Log the error in the trace log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCE_UID_NOT_FOUND_IN_LOCAL_SDS, p_sUid);

			// Raise a SamlBridge error.
			string sErrorInfo = string.Format(
				SamlBridgeErrorCodeHelper.ERRINFO_USER_HOST_ADDRESS,
				Request.UserHostAddress);

			ReportError(SamlBridgeErrorCode.SamlBridgeUidNotFound, sErrorInfo);
			return null;
		}

		// Log the fact that the search returned a result.
		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			Fujitsu.SamlBridge.Trace.WriteLine(
				this, Constants.TRCI_LOCAL_SDS_SEARCH_RESULT_OBTAINED);

		// Get a DirectoryEntry object from the search result.
		DirectoryEntry deNhsPerson = srNhsPerson.GetDirectoryEntry();

		// Check for a null DirectoryEntry object.
		if (deNhsPerson == null)
		{
			// Log the error in the trace log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCE_FAILED_TO_CREATE_RESULT_ENTRY, p_sUid);

			// Raise a SamlBridge error.
			string sErrorInfo = string.Format(
				SamlBridgeErrorCodeHelper.ERRINFO_USER_HOST_ADDRESS_UID,
				Request.UserHostAddress,
				p_sUid);

			ReportError(SamlBridgeErrorCode.SamlBridgeNoDirectoryEntry, sErrorInfo);

			return null;
		}

		// Log the fact that we created a directory entry.
		if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
			Fujitsu.SamlBridge.Trace.WriteLine(
				this, Constants.TRCI_LOCAL_SDS_DIRECTORY_ENTRY_CREATED);

		return deNhsPerson;
    }

    /// <summary>
    /// Builds the hidden HTML fields required for posting back to the ADFS resource server
    /// </summary>
    /// <param name="spineSaml">The saml returned by the spine</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)")]
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
            this.ReportError(SamlBridgeErrorCode.UnableToDetermineReturnAddress, "ResourceUri was: " + this.FederationRealm + Environment.NewLine + "Exception Details:" + ex.ToString());
            return;
        }

        // Output the form html
        this.ltrForm.Text = String.Format(
            @"<form method='post' action='{0}' >
                    <input type='hidden' name='wa' value='wsignin1.0' />
                    <input type='hidden' name='wresult' value='{1}' />
                    <input type='hidden' name='wctx' value='{2}' />
                    </form>",
            returnAddress,
            HttpUtility.HtmlEncode(spineSaml),
            this.FederationContext);
        
        this.pnlMain .Visible = true;
    }

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
	/// Any additional information to be presented. This will only be stored in the event log,
	/// not be sent back to the user.
	/// </param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
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
    /// <summary>
    ///	For each value in the supplied PropertyValueCollection, this method adds a new Attribute
	///	object to the supplied attribute collection. The attribute name is specified by the
	/// p_sAttributeName parameter, and the attribute's value is taken from the corresponding
	///	value in the PropertyValueCollection.
    /// </summary>
    /// <param name="p_collAttributes">
	///	The collection of Attribute objects to be appended to.
	/// </param>
	/// <param name="p_sAttributeName">
	///	The name of the attributes to be added.
	/// </param>                
	/// <param name="p_pvcollPropertyValues">
	///	The property value collection whose values are to be processed.
	/// </param>
    private void AddSamlAttributes(
		Collection<Fujitsu.SamlBridge.Transformation.Token.Attribute> p_collAttributes,
		string p_sAttributeName,
		PropertyValueCollection p_pvcollPropertyValues)
    {
		foreach (string sThisValue in p_pvcollPropertyValues)
        {
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this,
					Constants.TRCI_CREATING_SAML_ATTRIBUTE,
					p_sAttributeName,
					sThisValue);

			p_collAttributes.Add(
				new Fujitsu.SamlBridge.Transformation.Token.Attribute(
					p_sAttributeName, sThisValue));
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
    private string GetApplicationSetting (string p_sKey, Type p_typType, bool p_bMandatory)
    {
        string sValue = Universal.S_EMPTY;
        try
        {
            sValue = (string) s_appsr.GetValue(p_sKey, p_typType);
        }
        catch (InvalidOperationException) {}

		if (p_bMandatory && sValue.Length == 0)
		{
			// Missing mandatory application setting - log to event log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCE_MISSING_MANDATORY_APPLICATION_SETTING, p_sKey);

			// Raise a SamlBridge error.
			ReportError(SamlBridgeErrorCode.SamlBridgeAppSettingMissing, p_sKey);
		}

        return sValue;
    }
}