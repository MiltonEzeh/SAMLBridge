// <copyright file="Certificates.cs" company="Fujitsu">
// (c) Copyright 2006, 2007. All rights reserved.
// </copyright>
// <summary>
// Provides certificate support to the SAML transformation engine.
// </summary>
namespace Fujitsu.SamlBridge.Transformation
{
	#region Using Statements
	using System;
	using System.Collections.Generic;
	using System.Security.Cryptography.X509Certificates;
	using System.Globalization;
	using System.Diagnostics;
	#endregion

	/// <summary>Implements a certificate cache.</summary>
	public static class Certificates
	{
		#region Private variables
		/// <summary>
		/// The collection of cached certificates.
		/// </summary>
		private static Dictionary<string, X509Certificate2> s_dctCertificateCache =
			new Dictionary<string, X509Certificate2>();

		/// <summary>
		/// The time of the next cache clearance.
		/// </summary>
		private static DateTime s_dtCacheExpiryTime = DateTime.MinValue;
		#endregion

		#region Public Methods
		/// <summary>
		/// Gets the specified certificate. If the certificate exists in the cache, it is
		/// retrieved from there. Otherwise, it is obtained from the local machine personal
		/// certificate store, and added to the cache.
		/// </summary>
		/// <param name="certificateSubject">
		/// The subject field of the required certificate.
		/// </param>
		/// <returns>The requested certificate.</returns>
		public static X509Certificate2 GetCachedCertificate(string p_sSubject)
		{
			// Clear the certificate cache if the expiry time has passed.
			lock (s_dctCertificateCache)
			{
				if (DateTime.Now > s_dtCacheExpiryTime)
				{
					s_dctCertificateCache.Clear();
					s_dtCacheExpiryTime = DateTime.Now.AddSeconds(
						Constants.CERTCACHE_CERTIFICATE_LIFETIME_SECONDS);
				}
			}

			// Attempt to get the certificate from the cache.
			X509Certificate2 x509c2 = null;
			lock (s_dctCertificateCache)
			{
				if (s_dctCertificateCache.ContainsKey(p_sSubject))
				{
					x509c2 = s_dctCertificateCache[p_sSubject];
				}

				if (x509c2 == null)
				{
					// Get the certificate from the personal machine store.
					x509c2 = GetCertificateFromMachineStore(p_sSubject);

					// Add the certificate to the cache.
					s_dctCertificateCache.Add(p_sSubject, x509c2);
				}
			}

			// Return the certificate to the caller.
			return x509c2;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Retrieves a certificate from the personal local machine store.
		/// </summary>
		/// <param name="certificateSubject">The subject field of the required certificate.
		/// </param>
		/// <returns>The required certificate.</returns>
		private static X509Certificate2 GetCertificateFromMachineStore (string p_sSubject)
		{
			X509Certificate2 x509c2 = null;
			X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
			store.Open(OpenFlags.ReadOnly);            

			// Loop through each of the certificates in the store until we match one with the
			// correct subject field.
			foreach (X509Certificate2 x509c2ThisCert in store.Certificates)
			{
				// Parse the expiration date of the certificate.
				DateTime dtExpirationTime = DateTime.Parse(
					x509c2ThisCert.GetExpirationDateString(), CultureInfo.CurrentCulture);

				if
					((x509c2ThisCert.Subject == p_sSubject)
					&&
					(dtExpirationTime > DateTime.Now))
				{
					// The certificate has the required subject and has not expired. Check
					// whether it is approaching its expiry time.
					TimeSpan tsRemainingLife = dtExpirationTime.Subtract(DateTime.Now);

					if
						(tsRemainingLife.TotalDays
						<
						Constants.SIGNING_CERTIFICATE_EXPIRY_WARNING_THRESHOLD_DAYS)
					{
						// The certificate has a limited life left - write a warning event to
						// the event log to alert the system administrator.
						EventLog.WriteEntry(
							Constants.EVLOG_EVENT_SOURCE_SAML_BRIDGE,
							string.Format(
								Constants.EVMSG_SIGNING_CERTIFICATE_EXPIRING,
								tsRemainingLife.TotalDays),
								EventLogEntryType.Warning);
					}

					x509c2 = x509c2ThisCert;
					break;
				}
			}

			if (x509c2 == null)
			{
				throw new TransformationException(String.Format(
					Constants.EXCMSG_CANNOT_FIND_CERTIFICATE, p_sSubject));
			}

			// Return the retrieved certificate to the caller.
			return x509c2;
		}
		#endregion
	}
}