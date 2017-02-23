using System;
using System.Collections.Generic;
using System.Text;

namespace Fujitsu.SamlBridge.Transformation
{
	class Constants
	{
		// Universal constants.
		public const int SECONDS_PER_MINUTE = 60;

		// Windows event log items.
		public const string EVLOG_EVENT_SOURCE_SAML_BRIDGE = "Fujitsu SamlBridge";

		// Certificate cache parameters.
		public const long CERTCACHE_CERTIFICATE_LIFETIME_SECONDS = 15 * SECONDS_PER_MINUTE;

		// Event log messages.
		public const string EVMSG_SIGNING_CERTIFICATE_EXPIRING =
			"The SamlBridge signing certificate will expire in {0} days. To avoid system" +
			" outage, please renew the certificate within this period.";

		// Certificate parameters.
		public const int SIGNING_CERTIFICATE_EXPIRY_WARNING_THRESHOLD_DAYS = 60;

		// Exception messages.
		public const string EXCMSG_CANNOT_FIND_CERTIFICATE =
			"Failed to find certificate with subject [{0}] in the personal local machine" +
			" certificate store";
	}
}
