//-----------------------------------------------------------------------
// <copyright file="SamlBridge.aspx.cs" company="Fujitsu">
//     (c) Copyright 2006.  All rights reserved.
// </copyright>
// <summary>
//     SamlBridge.aspx.cs summary comment.
// </summary>
//-----------------------------------------------------------------------
#region Compiler Settings
#define TRACE
#endregion

#region using Directives
using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
//using Fujitsu.SamlBridge.Web.Fujitsu.SamlBridge.Web;
using Fujitsu.SamlBridge.Web;
using Fujitsu.SamlBridge;
#endregion

    /// <summary>
    /// SamlBridge is responsible for obtaining the authenticated user's current token id using an ActiveX control and then forwarding
    /// it to the proxy bridge redirection page.
    /// </summary>
    public partial class SamlBridge : System.Web.UI.Page
    {
        private const string SignInJavaScript =
            "<script language=\"javascript\" type=\"text/javascript\">" +
            "var bSignIn = true;</script>";

        private const string SignOutJavaScript =
            "<script language=\"javascript\" type=\"text/javascript\">" +
            "var bSignIn = false;</script>";

        #region Protected Methods
        /// <summary>
        /// Handles the loading of the page
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        protected void Page_Load(object sender, EventArgs e)
        {
			if (Page.IsPostBack)
			{
				// Log BEGIN_REDIRECT_PAGE timing trace point.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCT_BEGIN_REDIRECT_PAGE);
			}
			else
			{
				// Log BEGIN_MAIN_PAGE timing trace point.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
					Fujitsu.SamlBridge.Trace.WriteLine(this, Constants.TRCT_BEGIN_MAIN_PAGE);
			}

            /* First of all we need to determine whether this is a signin or a signout request,
             * and set the action of the page accordingly. If it is a signin request, indicated
             * by a value of wsignin1.0 in the wa querystring parameter, then we invoke the SSO
             * process. Otherwise we return a blank page. */

			// TODO: consider moving this code to non-post-back path only.

            if
                ((Request.QueryString[Federation.RequestParameter] != null)
                &&
                (Request.QueryString[Federation.RequestParameter]
                    .Equals(Federation.SignInRequestV1_0)))
            {
                // Sign in request.
                if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCI_SIGN_IN_V1_0_REQUEST);

                this.ltrSignInFlag.Text = SignInJavaScript;
            }
            else
            {
                // Sign out request.
                if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
                    Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCI_SIGN_OUT_V1_0_REQUEST);

                this.ltrSignInFlag.Text = SignOutJavaScript;
				return;
            }

            if (Page.IsPostBack)
            {
				// This is a post-back request to the SamlBridge.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCI_POSTBACK_REQUEST_TO_MAIN_PAGE);
				
				// Get the viewstate expiry date and check that it has not passed.
				object oViewstateExpiryDate = this.ViewState["ExpiryDate"];
				DateTime dtViewstateExpiryDate = (DateTime) oViewstateExpiryDate;

				if (oViewstateExpiryDate != null)
				{
					if (DateTime.Now < dtViewstateExpiryDate)
					{
						// The viewstate expiry check has passed.
						if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
							Fujitsu.SamlBridge.Trace.WriteLine(
								this,
								Constants.TRCI_VIEWSTATE_EXPIRY_CHECK_PASSED,
								dtViewstateExpiryDate.ToString(Universal.DTFMT_YYYYMMDDHHMMSS));

						// Note that we will probably have to inspect any errors here, and post-back to the client again to
						// obtain the UID to support Spine fail-over.  The full requirements for this are not currently known.

						// The token id will have been posted to us, so transfer control over to the proxy bridge redirection page
						Server.Transfer("SamlBridgeRedirection.aspx", true);
					}
				}
				else
				{
					// We were unable to obtain the viewstate expiry date - log a warning.
					if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceWarning)
						Fujitsu.SamlBridge.Trace.WriteLine(
							this, Constants.TRCW_FAILED_TO_OBTAIN_VIEWSTATE_EXPIRY_DATE);
				}

                // The viewstate expiry check has failed.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceError)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this,
						Constants.TRCE_VIEWSTATE_EXPIRY_CHECK_FAILED,
						dtViewstateExpiryDate.ToString(Universal.DTFMT_YYYYMMDDHHMMSS));

				ReportError(
					SamlBridgeErrorCode.SamlBridgePostBackTimeoutExpired,
						"IP Address: "          +
						Request.UserHostAddress +
						"\r\nSupplied expiry:"  +
						oViewstateExpiryDate.ToString());
            }
            else
            {
				// This is an initial request to the SamlBridge.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgePath.TraceInfo)
					Fujitsu.SamlBridge.Trace.WriteLine(
						this, Constants.TRCI_INITIAL_REQUEST_TO_MAIN_PAGE);

				AppSettingsReader settingsReader = new AppSettingsReader();
                int expirySeconds = (int)settingsReader.GetValue("PostBackExpirySeconds", typeof(int));
                this.ViewState.Add("ExpiryDate", DateTime.Now.AddSeconds(expirySeconds));

				// Log END_MAIN_PAGE timing trace point.
				if (Fujitsu.SamlBridge.Trace.s_trswSamlBridgeTiming.Enabled)
					Fujitsu.SamlBridge.Trace.WriteLine(this, Constants.TRCT_END_MAIN_PAGE);
			}
        }

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
		#endregion
    }
