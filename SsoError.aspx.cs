using System;
using System.Data;
using System.Collections;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
//using Fujitsu.SamlBridge.Web.Fujitsu.SamlBridge.Web;

using Fujitsu.SamlBridge;
using Fujitsu.SamlBridge.Web;

public partial class AdfsError : System.Web.UI.Page 
{
	protected void Page_Load(object sender, EventArgs e)
    {
		// First check the source of the error.
		string sErrorSourceCode = Request.Form[Constants.HTMLFIELD_ERROR_SOURCE];

		if (sErrorSourceCode.Equals(Constants.ERRSRC_SAML_BRIDGE))
		{
			// The error has originated in the SamlBridge application, so enable the error code
			// diagnostics panel.
			this.pnlErrorCodeDiagnostics.Visible = true;

			// Build the error code table - first create the table row objects.
			TableRow tblrEcErrorSource = new TableRow();
			TableRow tblrEcErrorCode = new TableRow();
			TableRow tblrEcErrorDescription = new TableRow();
			TableRow tblrEcPossibleCausesCaption = new TableRow();
			TableRow tblrEcPossibleCausesData = new TableRow();
			TableRow tblrEcRemedialActionsCaption = new TableRow();
			TableRow tblrEcRemedialActionsData = new TableRow();

			// Create the cells of the error code table error source row.
			TableCell tblcEcErrorSourceCaption = new TableCell();
			TableCell tblcEcErrorSourceData = new TableCell();

			// Create the cells of the error code table error code row.
			TableCell tblcEcErrorCodeCaption = new TableCell();
			TableCell tblcEcErrorCodeData = new TableCell();

			// Create the cells of the error code table error description row.
			TableCell tblcEcErrorDescriptionCaption = new TableCell();
			TableCell tblcEcErrorDescriptionData = new TableCell();

			// Create the cells of the error code table possible causes rows.
			TableCell tblcEcPossibleCausesCaption = new TableCell();
			TableCell tblcEcPossibleCausesData = new TableCell();

			// Create the cells of the error code table remedial actions rows.
			TableCell tblcEcRemedialActionsCaption = new TableCell();
			TableCell tblcEcRemedialActionsData = new TableCell();

			// Make the possible causes and remedial actions cells span the full table width.
			tblcEcPossibleCausesCaption .ColumnSpan = 2;
			tblcEcPossibleCausesData    .ColumnSpan = 2;
			tblcEcRemedialActionsCaption.ColumnSpan = 2;
			tblcEcRemedialActionsData   .ColumnSpan = 2;

			// Attach the error code table error source cells to their row.
			tblrEcErrorSource.Cells.Add(tblcEcErrorSourceCaption);
			tblrEcErrorSource.Cells.Add(tblcEcErrorSourceData);

			// Attach the error code table error code cells to their row.
			tblrEcErrorCode.Cells.Add(tblcEcErrorCodeCaption);
			tblrEcErrorCode.Cells.Add(tblcEcErrorCodeData);

			// Attach the error code table error description cells to their row.
			tblrEcErrorDescription.Cells.Add(tblcEcErrorDescriptionCaption);
			tblrEcErrorDescription.Cells.Add(tblcEcErrorDescriptionData);

			// Attach the error code table possible causes caption cell to its row.
			tblrEcPossibleCausesCaption.Cells.Add(tblcEcPossibleCausesCaption);

			// Attach the error code table possible causes data cell to its row.
			tblrEcPossibleCausesData.Cells.Add(tblcEcPossibleCausesData);

			// Attach the error code table remedial actions caption cell to its row.
			tblrEcRemedialActionsCaption.Cells.Add(tblcEcRemedialActionsCaption);

			// Attach the error code table remedial actions data cell to its row.
			tblrEcRemedialActionsData.Cells.Add(tblcEcRemedialActionsData);

			// Add the error code table rows to the table.
			tblErrorCode.Rows.Add(tblrEcErrorSource);
			tblErrorCode.Rows.Add(tblrEcErrorCode);
			tblErrorCode.Rows.Add(tblrEcErrorDescription);
			tblErrorCode.Rows.Add(tblrEcPossibleCausesCaption);
			tblErrorCode.Rows.Add(tblrEcPossibleCausesData);
			tblErrorCode.Rows.Add(tblrEcRemedialActionsCaption);
			tblErrorCode.Rows.Add(tblrEcRemedialActionsData);

			// Apply the CSS styles to the error code table caption cells.
			tblcEcErrorSourceCaption.CssClass =
				"tableCell tableCaptionCell tableNotRightmostCell";

			tblcEcErrorCodeCaption.CssClass =
				"tableCell tableCaptionCell tableNotRightmostCell";

			tblcEcErrorDescriptionCaption.CssClass =
				"tableCell tableCaptionCell tableNotRightmostCell";

			tblcEcPossibleCausesCaption.CssClass =
				"tableCell tableCaptionCell tableRightmostCell";

			tblcEcRemedialActionsCaption.CssClass =
				"tableCell tableCaptionCell tableRightmostCell";

			// Apply the CSS styles to the error code table data cells.
			tblcEcErrorSourceData.CssClass =
				"tableCell tableTextCell tableRightmostCell";

			tblcEcErrorCodeData.CssClass =
				"tableCell tableTextCell tableRightmostCell";

			tblcEcErrorDescriptionData.CssClass = "tableCell tableTextCell tableRightmostCell";
			tblcEcPossibleCausesData.CssClass = "tableCell tableTextCell tableRightmostCell";
			tblcEcRemedialActionsData.CssClass = "tableCell tableTextCell tableRightmostCell";

			// Populate the caption cells of the error code table.
			tblcEcErrorSourceCaption.Text      = Constants.TBLCAP_ERROR_SOURCE;
			tblcEcErrorCodeCaption.Text        = Constants.TBLCAP_ERROR_CODE;
			tblcEcErrorDescriptionCaption.Text = Constants.TBLCAP_ERROR_DESCRIPTION;
			tblcEcPossibleCausesCaption.Text   = Constants.TBLCAP_POSSIBLE_CAUSES;
			tblcEcRemedialActionsCaption.Text  = Constants.TBLCAP_REMEDIAL_ACTIONS;

			// Extract the values of the error code form field.
			string sErrorCode = Request.Form[Constants.HTMLFIELD_ERROR_CODE];

			// Populate the error source and error code data cells of the error code table.
			tblcEcErrorSourceData.Text = Constants.ERRSRCTXT_SAML_BRIDGE;
			tblcEcErrorCodeData.Text = sErrorCode;

			// Convert the error code to a SamlBridgeErrorCode value.
			SamlBridgeErrorCode enumErrorCode = (SamlBridgeErrorCode) int.Parse(sErrorCode);

			// Populate the error description data cell of the error code table.
			tblcEcErrorDescriptionData.Text =
				SamlBridgeErrorCodeHelper.GetErrorMessage(enumErrorCode);

			// Populate the possible causes data cell of the error code table.
			tblcEcPossibleCausesData.Text = SamlBridgeErrorCodeHelper.AddHtmlTags(
				SamlBridgeErrorCodeHelper.GetErrorCauses(enumErrorCode));

			// Populate the remedial actions data cell of the error code table.
			tblcEcRemedialActionsData.Text = SamlBridgeErrorCodeHelper.AddHtmlTags(
				SamlBridgeErrorCodeHelper.GetErrorActions(enumErrorCode));
		}
		else if (sErrorSourceCode.Equals(Constants.ERRSRC_ADFS))
		{
			// The error has originated in the ADFS application, so enable the exception
			// diagnostics panel.
			this.pnlExceptionDiagnostics.Visible = true;
 
			// Build the exception diagnostics, starting with the transaction details header
			// table. First create the table row object.
			TableRow tblrTdh = new TableRow();

			// Create the only cell of the transaction details header table.
			TableCell tblcTdh = new TableCell();

			// Attach the transaction details header table cell to its row.
			tblrTdh.Cells.Add(tblcTdh);

			// Add the transaction details header table row to its table.
			tblTransactionDetailsHeader.Rows.Add(tblrTdh);

			// Apply the CSS styles to the transaction details header table cell.
			tblcTdh.CssClass = "tableCell tableCaptionCell";

			// Insert the transaction details header table cell caption.
			tblcTdh.Text = Constants.TBLCAP_TRANSACTION_DETAILS;

			// Build the transaction details table - first create the table row objects.
			TableRow tblrTdUserHostAddress = new TableRow();
			TableRow tblrTdMachineName = new TableRow();

			// Create the cells of the transaction details table user host address row.
			TableCell tblcTdUserHostAddressCaption = new TableCell();
			TableCell tblcTdUserHostAddressData = new TableCell();

			// Create the cells of the transaction details table machine name row.
			TableCell tblcTdMachineNameCaption = new TableCell();
			TableCell tblcTdMachineNameData = new TableCell();

			// Attach transaction details table user host address cells to their row.
			tblrTdUserHostAddress.Cells.Add(tblcTdUserHostAddressCaption);
			tblrTdUserHostAddress.Cells.Add(tblcTdUserHostAddressData);

			// Attach the machine name cells to their row.
			tblrTdMachineName.Cells.Add(tblcTdMachineNameCaption);
			tblrTdMachineName.Cells.Add(tblcTdMachineNameData);

			// Add the transaction details table rows to the table.
			tblTransactionDetails.Rows.Add(tblrTdUserHostAddress);
			tblTransactionDetails.Rows.Add(tblrTdMachineName);

			// Apply the CSS styles to the transaction details table cells.
			tblcTdUserHostAddressCaption.CssClass =
				"tableCell tableCaptionCell tableNotRightmostCell";

			tblcTdMachineNameCaption.CssClass =
				"tableCell tableCaptionCell tableNotRightmostCell";

			tblcTdUserHostAddressData.CssClass = "tableCell tableDataCell";
			tblcTdMachineNameData.CssClass = "tableCell tableDataCell";

			// Populate the caption cells of the transaction details table.
			tblcTdUserHostAddressCaption.Text = Constants.TBLCAP_USER_HOST_ADDRESS;
			tblcTdMachineNameCaption.Text = Constants.TBLCAP_MACHINE_NAME;

			// Extract the values of the user host address and machine name form fields.
			string sUserHostAddress = Request.Form[Constants.HTMLFIELD_USER_HOST_ADDRESS];
			string sMachineName = Request.Form[Constants.HTMLFIELD_MACHINE_NAME];

			// Populate the data cells of the transaction details table.
			tblcTdUserHostAddressData.Text = sUserHostAddress;
			tblcTdMachineNameData.Text = sMachineName;

			// Build the exception details header table - first create the table row object.
			TableRow tblrEdh = new TableRow();

			// Create the only cell of the exception details header table.
			TableCell tblcEdh = new TableCell();

			// Attach the exception details header table cell to its row.
			tblrEdh.Cells.Add(tblcEdh);

			// Add the exception details header table row to its table.
			tblExceptionDetailsHeader.Rows.Add(tblrEdh);

			// Apply the CSS styles to the exception details header table cell.
			tblcEdh.CssClass = "tableCell tableCaptionCell";

			// Insert the exception details header table cell caption.
			tblcEdh.Text = Constants.TBLCAP_EXCEPTION_DETAILS;

			// Build the exception details table - first create the header row object.
			TableRow tblrEdHeader = new TableRow();

			// Create the cells of the exception details table header row.
			TableCell tblcEdExceptionTypeHeader = new TableCell();
			TableCell tblcEdExceptionMessageHeader = new TableCell();

			// Attach the exception details table header cells to their row.
			tblrEdHeader.Cells.Add(tblcEdExceptionTypeHeader);
			tblrEdHeader.Cells.Add(tblcEdExceptionMessageHeader);

			// Add the exception details table header row to its table.
			tblExceptionDetails.Rows.Add(tblrEdHeader);

			// Apply the CSS styles to the exception details table header row cells.
			tblcEdExceptionTypeHeader.CssClass = "tableCell tableCaptionCell";
			tblcEdExceptionMessageHeader.CssClass = "tableCell tableCaptionCell";

			// Populate the exception details table header row cells.
			tblcEdExceptionTypeHeader.Text    = Constants.TBLCAP_EXCEPTION_TYPE;
			tblcEdExceptionMessageHeader.Text = Constants.TBLCAP_EXCEPTION_MESSAGE;

			// Iterate through the exception type form fields.
			string sThisExceptionType = string.Empty;
			string sThisExceptionMessage = string.Empty;

			int iExceptionIndex = 0;
			string sExceptionInputTags = string.Empty;

			TableRow tblrEdThisException = null;

			TableCell tblcEdThisExceptionType = null;
			TableCell tblcEdThisExceptionMessage = null;

			while (sThisExceptionType != null)
			{
				// Generate the exception type and message form field names.
				string sThisExceptionTypeField = string.Format(
					Constants.SFMT_HTMLFIELD_EXCEPTION_TYPE, iExceptionIndex.ToString());
				string sThisExceptionMessageField = string.Format(
					Constants.SFMT_HTMLFIELD_EXCEPTION_MESSAGE, iExceptionIndex.ToString());

				// Extract the values of the exception type and message form fields.
				sThisExceptionType = Request.Form[sThisExceptionTypeField];
				sThisExceptionMessage = Request.Form[sThisExceptionMessageField];

				// End the loop if no exception exists at this index.
				if (sThisExceptionType == null) continue;

				// We have an exception, so add a new table row for it.
				tblrEdThisException = new TableRow();

				// Create the cells of the new row.
				tblcEdThisExceptionType = new TableCell();
				tblcEdThisExceptionMessage = new TableCell();

				// Attach the cells to the row.
				tblrEdThisException.Cells.Add(tblcEdThisExceptionType);
				tblrEdThisException.Cells.Add(tblcEdThisExceptionMessage);

				// Attach the row to the table.
				tblExceptionDetails.Rows.Add(tblrEdThisException);

				// Apply the CSS styles to the cells.
				tblcEdThisExceptionType.CssClass = "tableCell tableDataCell tableNotRightmostCell";
				tblcEdThisExceptionMessage.CssClass = "tableCell tableDataCell";

				// Populate the cells.
				tblcEdThisExceptionType.Text = sThisExceptionType;
				tblcEdThisExceptionMessage.Text = sThisExceptionMessage;

				// Increment the exception index.
				++iExceptionIndex;
			}
		}
	}
}