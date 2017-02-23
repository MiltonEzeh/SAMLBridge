//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Fujitsu">
//     (c) Copyright 2006.  All rights reserved.
// </copyright>
// <summary>
//     Program.cs summary comment.
// </summary>
//-----------------------------------------------------------------------

namespace Fujitsu.SamlBridge.Install
{
    #region Using Statements
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Runtime.InteropServices;
    using System.Configuration;
    using System.Diagnostics;
    using System.Security.Principal;
    #endregion

    /// <summary>
    /// The entry point for the application
    /// </summary>
    public static class Program
    {
        #region Main
        /// <summary>
        /// The application entry point
        /// </summary>
        public static void Main()
        {
            if (!EventLog.SourceExists("Fujitsu SamlBridge"))
            {
                EventLog.CreateEventSource("Fujitsu SamlBridge", "Application");
            }
        }
        #endregion
    }
}
