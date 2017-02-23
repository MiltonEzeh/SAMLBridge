//-----------------------------------------------------------------------
// <copyright file="CustomSignedXml.cs" company="Fujitsu">
//     (c) Copyright 2006.  All rights reserved.
// </copyright>
// <summary>
//     CustomSignedXml.cs summary comment.
// </summary>
//-----------------------------------------------------------------------

namespace Fujitsu.SamlBridge.Transformation
{
    #region Using Statements
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Security.Cryptography.Xml;
    using System.Xml;
    #endregion

    /// <summary>
    /// A overrided class for SignedXml
    /// </summary>
    public class CustomIdSignedXml : SignedXml
    {
        #region Private Variables
        /// <summary>
        /// The list attributes that can be used to determine the id element text
        /// </summary>
        private static readonly string[] idattrs = new string[] { "AssertionID" };
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="T:CustomIdSignedXml"/> class.
        /// </summary>
        /// <param name="doc">The xml document.</param>
        public CustomIdSignedXml(XmlDocument doc)
            : base(doc)
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the id element.
        /// </summary>
        /// <param name="document">The doc.</param>
        /// <param name="idValue">The id value.</param>
        /// <returns>A new XmlElement</returns>
        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            // check to see if it's a standard ID reference
            XmlElement idElem = base.GetIdElement(document, idValue);
            if (idElem != null)
            {
                return idElem;
            }

            // if not, search for custom ids
            foreach (string idattr in idattrs)
            {
                idElem = document.SelectSingleNode("//*[@" + idattr + "=\"" + idValue + "\"]") as XmlElement;
                if (idElem != null)
                {
                    break;
                }
            }

            return idElem;
        }
        #endregion
    }
}
