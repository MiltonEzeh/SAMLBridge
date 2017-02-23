using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using Fujitsu.SamlBridge;

public partial class SamlBridgeError : System.Web.UI.Page
{
	/// <summary>AppSettingsReader object.</summary>
	private static AppSettingsReader s_appsr = new AppSettingsReader();

	protected void Page_Load(object sender, EventArgs e)
	{
		// Get the access gateway root URL.
		string sAccessGatewayRootUrl = this.GetApplicationSetting(
			Constants.APPKEY_ACCESS_GATEWAY_ROOT_URL, typeof(string), true);

		// Build the URL of the SamlBridge SSO error page.
		string sSsoErrorPageUrl = sAccessGatewayRootUrl;

		// Add SamlBridge application.
		sSsoErrorPageUrl += Constants.WEB_SAML_BRIDGE;
		sSsoErrorPageUrl += Universal.URL_APPLICATION_TERMINATOR;

		// Add SamlBridge SSO error page.
		sSsoErrorPageUrl += Constants.ASPX_SSO_ERROR;

		// Generate the HTML form start tag.
		string sFormStartTag = string.Format(Universal.SFMT_HTML_FORM_START, sSsoErrorPageUrl);

		// Generate the HTML input tag for the error source.
		string sInputTagErrorSource = string.Format(
			Universal.SFMT_HTML_INPUT,
			Constants.HTMLFIELD_ERROR_SOURCE,
			HttpUtility.HtmlEncode(Request.Params[Constants.QSPARAM_ERROR_SOURCE]));

		// Generate the HTML input tag for the error code.
		string sInputTagErrorCode = string.Format(
			Universal.SFMT_HTML_INPUT,
			Constants.HTMLFIELD_ERROR_CODE,
			HttpUtility.HtmlEncode(Request.Params[Constants.QSPARAM_ERROR_CODE]));

		// Populate the form element placeholder.
		this.ltrFormElement.Text =
			sFormStartTag + sInputTagErrorSource + sInputTagErrorCode + Universal.HTML_FORM_END;
	}

	/// <summary>Gets the value of an application setting from the Web.config file.</summary>
	/// <param name="p_sKey">The application setting to retrieve.</param>
	/// <param name="p_typType">Type of the object to be returned.</param>
	/// <param name="p_bMandatory">Whether the setting is mandatory.</param>
	/// <returns>The value of the specified application setting.</returns>
	private string GetApplicationSetting(string p_sKey, Type p_typType, bool p_bMandatory)
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

			// Can't raise another SamlBridge error as we are already processing one. Just
			// return an empty string.
			return string.Empty;
		}
		return sValue;
	}
}
