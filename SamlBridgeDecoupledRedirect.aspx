<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SamlBridgeDecoupledRedirect.aspx.cs" Inherits="SamlBridgeDecoupledRedirect" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
	<head runat="server">
		<title>SamlBridge - Decoupled Mode Redirect</title>
		<link href="css/SsoError.css" rel="stylesheet" type="text/css" />
	</head>
	<body>
		<asp:Panel ID="pnlMain" runat="server">
			<asp:Literal ID="ltrForm" runat="server" />
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
			<script type="text/javascript" language="javascript">
				window.setTimeout('document.forms[0].submit()',0);
			</script>
		</asp:Panel>
	</body>
</html>
