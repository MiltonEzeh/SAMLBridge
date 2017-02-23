#region Compiler Settings
#define TRACE
#endregion

#region using Directives
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Diagnostics;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using Fujitsu.SamlBridge;
#endregion

//using Fujitsu.SamlBridge.Web.Fujitsu.SamlBridge.Web;
using Fujitsu.SamlBridge.Web;

using ServerLA;

public partial class SamlBridgeDecoupled : System.Web.UI.Page
{
    //private ServerLA.ServerLA m_objLAServer = new ServerLA.ServerLA();

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!this.IsPostBack)
        {
            /*
             * This is the first request to the page. This means we must initiate the decoupled
             * process. The first step is to obtain a challenge from the LA server module, and
             * write it back to the client in HTML form field hdnChallenge.
             */
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCI_INITIAL_REQUEST_TO_DECOUPLED_PAGE);

			this.hdnChallenge.Value = GetChallenge();
        }
        else
        {
            /*
             * This is a post-back request to the page, which implies that we are at the second
             * step of the decoupled process. A challenge has been passed to the client, and the
             * client should have signed it and posted it back to the server in HTML form field
             * hdnSignedChallenge.
             */
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCI_POSTBACK_REQUEST_TO_DECOUPLED_PAGE);

			string sSignedChallenge = this.hdnSignedChallenge.Value;

            // Check that the client has passed us a signed challenge.
            if (sSignedChallenge.Length == 0)
            {
				// No signed challenge has been received.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCE_NO_SIGNED_CHALLENGE_RECEIVED_FROM_CLIENT);

				// Raise a SamlBridge error.
				ReportError(
					SamlBridgeErrorCode.SamlBridgeNoSignedChallenge,
					"IP address: " + Request.UserHostAddress);

				// No further processing can be done without a signed challenge, so return.
				return;
            }

            /*
             * The signed challenge received from the client LA module is a base-64 encoded
             * string with line breaks. The line breaks are 0x0a characters. When IE executes
             * the JavaScript code that copies the signed challenge string into the hidden form
             * field in which it is passed to the server, it replaces the 0x0a linefeed
             * characters with Windows linefeeds, viz. 0x0d0a characters. Left like this, the
             * signed challenge would not be accepted by the client LA module. We must
             * therefore convert the linefeeds back to 0x0a characters. Luckily, 0x0d is not a
             * valid character in a base-64 encoded string (outside of the linefeeds, anyway),
             * so we can achieve the conversion simply by removing all 0x0d's from the string.
             */
            Regex rgx = new Regex("\x0d");
            string sConvertedSignedChallenge = rgx.Replace(sSignedChallenge, "");

			// Log the converted signed challenge to the trace log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this,
					Constants.TRCI_SIGNED_CHALLENGE_RECEIVED_FROM_CLIENT,
					sConvertedSignedChallenge);

            // We have a signed challenge, so use it to obtain the user's UID.
            string sUid = this.GetUid(sConvertedSignedChallenge);

            /*
             * Check whether we have successfully obtained a UID. If so, we need to use the UID
             * in a lookup on the local directory to obtain the SAML token. We can then redirect
             * back to ADFS and rejoin the vanilla code path. If no UID was obtained, then we
             * return and allow this page to render an error message to the browser.
             */
			if (sUid.Length == 0) return;

			// We have a UID - log it to the trace log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this,
					Constants.TRCI_UID_OBTAINED_FROM_LOCAL_AUTHENTICATOR,
					sUid);

			/* Add the UID to the querystring, and transfer control to the decoupled mode
			 * redirection page, SamlBridgeDecoupledRedirect.aspx. */
			string sUrl = "SamlBridgeDecoupledRedirect.aspx?UID=" + sUid;

			string[] arrParameters = Request.QueryString.AllKeys;

			for (int i = 0; i < arrParameters.Length; i++)
			{
				string sThisParameter = arrParameters[i];
				string sThisValue = Request.QueryString[sThisParameter];

				sUrl += "&" + sThisParameter + "=" + sThisValue;
			}

			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCI_TRANSFERRING_CONTROL_TO_URL, sUrl);

			Server.Transfer(sUrl, true);
        }
    }

    protected string GetChallenge()
    {
        // Get a reference to the LA server module object from session state.
        ServerLA.ServerLA objLAServer = (ServerLA.ServerLA) Session["LAServerModule"];

        // Call the ServerLA object's GetChallenge() method to get a challenge string.
        string sChallenge = objLAServer.GetChallenge();

        if ((sChallenge == null || sChallenge.Length == 0))
        {
			// No challenge was obtained - log this to the trace log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCE_NO_CHALLENGE_OBTAINED_FROM_LOCAL_AUTHENTICATOR);

			// Raise a SamlBridge error.
			ReportError(
				SamlBridgeErrorCode.SamlBridgeNoChallenge,
				"IP address: " + Request.UserHostAddress);

            return string.Empty;
        }
        
        // A challenge was obtained, so return it.
        return sChallenge;
    }

    protected string GetUid(string sSignedChallenge)
    {
        // Get a reference to the LA server module object from session state.
        ServerLA.ServerLA objLAServer = (ServerLA.ServerLA)Session["LAServerModule"];

        // Pass the signed challenge to the LA server module's GetUID() method.
        string sUid = objLAServer.GetUID(sSignedChallenge);

        // Check that a UID was returned.
        if ((sUid == null) || (sUid.Length == 0))
        {
			// No UID was returned by the local authenticator - log this to the trace log.
			if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
				Fujitsu.SamlBridge.Trace.WriteLine(
					this, Constants.TRCE_NO_UID_OBTAINED_FROM_LOCAL_AUTHENTICATOR);

			// Get the error code and last error string from the local authenticator.
			int iLaError = objLAServer.LastError;
            string sLaError = objLAServer.LastErrorString;

            // Raise a SamlBridge error.
			string sAdditionalInfo =
                "IP address: " + Request.UserHostAddress +
	            Environment.NewLine + "LASM error code: " + iLaError.ToString() +
		        Environment.NewLine + "LASM error text: " + sLaError;

			ReportError(SamlBridgeErrorCode.SamlBridgeNoUid, sAdditionalInfo);
            return string.Empty;
        }

        // A UID was returned, so pass it back to the caller.
        return sUid;
    }

	private void ReportError(SamlBridgeErrorCode p_enumErrorCode, string p_sAdditionalInfo)
	{
		// First log the error in the Windows event log.
		SamlBridgeErrorCodeHelper.LogErrorInEventLog(p_enumErrorCode, p_sAdditionalInfo);

		// Build the URL of the SamlBridge error page.
		string sSamlBridgeErrorPageUrl =
			Constants.ASPX_SAML_BRIDGE_ERROR                    +
			Universal.URL_QUERYSTRING_PARAMETER_LIST_SEPARATOR  +
			Constants.QSPARAM_ERROR_SOURCE                      +
			Universal.URL_QUERYSTRING_PARAMETER_VALUE_SEPARATOR +
			Constants.ERRSRC_SAML_BRIDGE                        +
			Universal.URL_QUERYSTRING_PARAMETER_SEPARATOR       +
			Constants.QSPARAM_ERROR_CODE                        +
			Universal.URL_QUERYSTRING_PARAMETER_VALUE_SEPARATOR +
			p_enumErrorCode.ToString(Universal.NFMT_DECIMAL);

		// Transfer control to the SamlBridge error page.
		Server.Transfer(sSamlBridgeErrorPageUrl);
	}
}