<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SamlBridgeDecoupled.aspx.cs" Inherits="SamlBridgeDecoupled" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
	<head runat="server">
		<title>SamlBridge - Decoupled Mode</title>
		<link href="css/SsoError.css" rel="stylesheet" type="text/css" />
	</head>
	<body>
		<asp:Panel ID="pnlMain" runat="server">
			<object
				classid="clsid:F1B17D8F-F096-47E1-A4D0-21DB9D757780"
				codebase="IATicket.cab#version=1,0,0,0" id="objIATicket">
			</object>
			<form id="frmMain" runat="server">
				<asp:HiddenField ID="hdnChallenge" runat="server" />
				<asp:HiddenField ID="hdnSignedChallenge" runat="server" />
				<asp:HiddenField ID="TokenError" runat="server" />
				<asp:HiddenField ID="TokenErrorDescription" runat="server" />
			</form>
			<div id="AllowActiveX" style="display:none">
				<div>
					<img class="floatRight" src="images/FujitsuLogo.jpg" alt="Fujitsu"/>
					<img src="images/NHSCfHLogo.jpg" alt="NHS Connecting for Health"/>
				</div>
				<hr class="hr20"/>
				<div class="redHeading">Sign-On Error</div>
				<div class="text">
					<p>An error has occurred in the sign-on process, and it has not been
					possible to log you on to the system at this time.</p>
					<p>Please refer this incident to your system administrator. It will assist
					in the resolution of this problem if you are able to capture the diagnostic
					information that follows this message. This can be done by choosing
					<span class="textBold">File | Save As...</span> from the Internet Explorer
					menu.</p>
				</div>
				<hr class="hr20"/>
				<div class="greenHeading">Diagnostic Information</div>
				<div class="div10"></div>
				<table border="0">
					<tr>
						<td class="tableCell tableCaptionCell tableNotRightmostCell">Error Source</td>
						<td class="tableCell tableTextCell tableRightmostCell">SamlBridge</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableNotRightmostCell">Error Code</td>
						<td class="tableCell tableTextCell tableRightmostCell">13</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableNotRightmostCell">Error Description</td>
						<td class="tableCell tableTextCell tableRightmostCell">It was not possible to install or run the IATicket ActiveX object.</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableRightmostCell" colspan="2">Possible Causes</td>
					</tr>
					<tr>
						<td class="tableCell tableTextCell tableRightmostCell" colspan="2">
							<p>The single sign-on process depends upon the operation of the
							IATicket ActiveX object, and an attempt to install or run this
							object has failed. This may be due to inappropriate browser security
							settings, or the unavailability of the necessary run-time
							environment for the ActiveX object.</p>
						</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableRightmostCell" colspan="2">Remedial Actions</td>
					</tr>
					<tr>
						<td class="tableCell tableTextCell tableRightmostCell" colspan="2">
							<p>Review the Internet Explorer security settings against those
							defined in document <span class="textBold">FJA/006/INF/GUD/11578
							&quot;NHS Release 1 Client Installation&quot;</span>, and make any
							necessary corrections.</p>
							<p>Ensure that the software package Microsoft Visual C++ 2005
							Redistributable is installed on the workstation. Refer to document
							<span class="textBold">FJA/006/INF/GUD/11578 &quot;NHS Release 1
							Client Installation&quot;</span> for further details.</p>
						</td>
					</tr>
				</table>
			</div>    
			<noscript>
				<div>
					<img class="floatRight" src="images/FujitsuLogo.jpg" alt="Fujitsu"/>
					<img src="images/NHSCfHLogo.jpg" alt="NHS Connecting for Health"/>
				</div>
				<hr class="hr20"/>
				<div class="redHeading">Sign-On Error</div>
				<div class="text">
					<p>An error has occurred in the sign-on process, and it has not been
					possible to log you on to the system at this time.</p>
					<p>Please refer this incident to your system administrator. It will assist
					in the resolution of this problem if you are able to capture the diagnostic
					information that follows this message. This can be done by choosing
					<span class="textBold">File | Save As...</span> from the Internet Explorer
					menu.</p>
				</div>
				<hr class="hr20"/>
				<div class="greenHeading">Diagnostic Information</div>
				<div class="div10"></div>
				<table border="0">
					<tr>
						<td class="tableCell tableCaptionCell tableNotRightmostCell">Error Source</td>
						<td class="tableCell tableTextCell tableRightmostCell">SamlBridge</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableNotRightmostCell">Error Code</td>
						<td class="tableCell tableTextCell tableRightmostCell">14</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableNotRightmostCell">Error Description</td>
						<td class="tableCell tableTextCell tableRightmostCell">It was not possible to run JavaScript in the browser.</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableRightmostCell" colspan="2">Possible Causes</td>
					</tr>
					<tr>
						<td class="tableCell tableTextCell tableRightmostCell" colspan="2">
							<p>The single sign-on process depends upon the execution of
							JavaScript code within the client browser, and an attempt to do this
							has failed. The most likely cause is that the browser security
							settings are not currently configured to allow client script to be
							run.</p>
						</td>
					</tr>
					<tr>
						<td class="tableCell tableCaptionCell tableRightmostCell" colspan="2">Remedial Actions</td>
					</tr>
					<tr>
						<td class="tableCell tableTextCell tableRightmostCell" colspan="2">
							<p>Review the Internet Explorer security settings against those
							defined in document <span class="textBold">FJA/006/INF/GUD/11578
							&quot;NHS Release 1 Client Installation&quot;</span>, and make any
							necessary corrections.</p>
						</td>
					</tr>
				</table>
			</noscript>
			<script language="javascript" type="text/javascript">
			<!--
			// Wait for the window to load, then call the SignChallenge() function.
			window.setTimeout('SignChallenge()', 0);
		    
			function SignChallenge()
			{
				var E_ACTIVEX_NOT_INSTALLED = -2146827850;
				var E_FAIL = -2147467259;
				var E_TICKET_API_ERROR = -536608255;
			    
				var frmMain = window.document.all.item('frmMain');
			    
				try
				{
    				var sChallenge = window.document.all.item('hdnChallenge').value;    	    
					var sSignedChallenge = objIATicket.SignChallenge(sChallenge);
		            
					window.document.all.item('hdnSignedChallenge').value = sSignedChallenge;
				}
				catch (e)
				{
					// The ticket API ActiveX control call has failed - get the error code.
					frmMain.TokenError.value = e.number;
		            
					switch (e.number)
					{
						case E_ACTIVEX_NOT_INSTALLED:
							AllowActiveX.style.display = "block";
		                    
							// Return immediately to prevent the post-back occurring.
							return;
		                    
						case E_TICKET_API_ERROR:
							// An error was raised by the LIA, so we need to interrogate the
							// ActiveX object to get the specific LIA error details, as all we
							// have at the moment is the ActiveX COM error information.
							var errorNo = ticket.GetLastError();
							frmMain.TokenError.value = errorNo;
							frmMain.TokenErrorDescription.value = ticket.GetErrorDescription(errorNo);
							break;
		                    
						default:
							if (e.number == E_FAIL)
							{
								frmMain.TokenErrorDescription.value = 'ActiveX Internal error';
							}
							break;
					}
				}

				// Post the signed challenge to the server.
				document.forms[0].submit();
			}
			// -->
			</script>
		</asp:Panel>
	</body>
</html>
