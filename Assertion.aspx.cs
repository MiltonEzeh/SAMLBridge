//-----------------------------------------------------------------------
// <copyright file="Assertion.aspx.cs" company="Fujitsu">
//     (c) Copyright 2006.  All rights reserved.
// </copyright>
// <summary>
//     Assertion.aspx.cs summary comment.
// </summary>
//-----------------------------------------------------------------------

#region Using Statements
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Xml;
#endregion

/// <summary>
/// The spine test assertion page
/// </summary>
public partial class Assertion : System.Web.UI.Page
{
    #region Web Form Designer generated code
    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Init"></see> event to initialize the page.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
    protected override void OnInit(EventArgs e)
    {
        string path = "";
        string tokenId = "";
        string samlAssertion = "";

        try
        {
            path = "c:\\assertions\\";

            if (Request.Params.Count > 0)
            {
                XmlDocument doc = new XmlDocument();

                tokenId = Request.Params[0];

                doc.Load(path + tokenId + ".xml");

                samlAssertion = doc.OuterXml;

                Response.Write(samlAssertion);
                Response.ContentType = "Text/XML";
            }
        }
        catch (XmlException ex)
        {
            Response.Write(ex.Message + "; so unable to construct a token for tokenId=" + tokenId);
            Response.ContentType = "Text/HTML";
        }
    }
    #endregion
}