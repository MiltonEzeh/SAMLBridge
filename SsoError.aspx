<%@ Page Language="C#" AutoEventWireup="true"  CodeFile="SsoError.aspx.cs" Inherits="AdfsError" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
	<head runat="server">
		<title>Sign-On Error Page</title>
		<link href="css/SsoError.css" rel="stylesheet" type="text/css" />
	</head>
	<body>
		<form id="frmMain" runat="server">
			<div>
				<img class="floatRight" src="images/FujitsuLogo.jpg" alt="Fujitsu"/>
				<img src="images/NHSCfHLogo.jpg" alt="NHS Connecting for Health"/>
			</div>
			<hr class="hr20"/>
			<div class="redHeading">Sign-On Error</div>
			<div class="text">
				<p>An error has occurred in the sign-on process, and it has not been possible to
				log you on to the system at this time.</p>
				<p>Please refer this incident to your system administrator. It will assist in
				the resolution of this problem if you are able to capture the diagnostic
				information that follows this message. This can be done by choosing
				<span class="textBold">File | Save As...</span> from the Internet Explorer
				menu.</p>
			</div>
			<hr class="hr20"/>
			<div class="greenHeading">Diagnostic Information</div>
			<div class="div10" />
			<asp:Panel runat="server" ID="pnlExceptionDiagnostics" Visible="false">
				<p><asp:Table ID="tblTransactionDetailsHeader" runat="server" /></p>
				<p><asp:Table ID="tblTransactionDetails" runat="server" /></p>
				<p><asp:Table ID="tblExceptionDetailsHeader" runat="server" /></p>
				<p><asp:Table ID="tblExceptionDetails" runat="server" /></p>
			</asp:Panel>
			<asp:Panel runat="server" ID="pnlErrorCodeDiagnostics" Visible="false">
				<p><asp:Table ID="tblErrorCode" runat="server" /></p>
			</asp:Panel>
		</form>
	</body>
</html>
