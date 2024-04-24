using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using FluentFTP.Exceptions;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {


		/// <summary>
		/// Parse the advertised port number from an EPSV response and derive an IPAD
		/// Handles (|||nnnn|) and (!!!nnnn!)
		/// </summary>
		protected void GetEnhancedPassivePort(FtpReply reply, out string derivedIpad, out int advertisedPort) {
			// Check the format of the EPSV response, respecting the code page problems of the vertical bar | vs. !
			var m = Regex.Match(reply.Message, @"\([!\|][!\|][!\|](?<advertisedPort>[0-9]+)[!\|]\)");

			if (!m.Success) {
				// In case EPSV responded with a regular "227 Entering Passive Mode" instead,
				// try parsing that before raising an exception
				/* Example:
				Command: EPSV
				Response: 227 Entering Passive Mode(XX, XX, XX, XX, 143, 225).
				*/
				try {
					GetPassivePort(FtpDataConnectionType.AutoPassive, reply, out derivedIpad, out advertisedPort);
					return;
				}
				catch {
					throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
				}
			}

			// Retrieve the advertised port
			advertisedPort = int.Parse(m.Groups["advertisedPort"].Value);

			/* Example:
			Command: EPSV
			Response: 229 Entering Extended Passive Mode(|||10016|)

			If the server (per hostname DNS query) has multiple IPAD's we will **NOT** possibly end up with a wrong IPAD,
			because:
			On establishing the control connection we stored the successfully used IPAD in our hostname/IPAD cache,
			therby replacing the list of IPADs that DNS gave us, with the single IPAD that we successfully connected to.

			Therefore any subsequent connects will use this single stored IPAD.
			*/

			// Derive the IPAD
			derivedIpad = m_host; // m_host is the original connect dnsname/IPAD for the control connection
		}

		/// <summary>
		/// Parse the advertised IPAD and advertised port number from a PASV response and derive the final IPAD
		/// </summary>
		protected void GetPassivePort(FtpDataConnectionType type, FtpReply reply, out string host, out int port) {
			// Check the format of the PASV response
			var m = Regex.Match(reply.Message, @"(?<quad1>[0-9]+)," + @"(?<quad2>[0-9]+)," + @"(?<quad3>[0-9]+)," + @"(?<quad4>[0-9]+)," + @"(?<port1>[0-9]+)," + @"(?<port2>[0-9]+)");

			if (!m.Success || m.Groups.Count != 7) {
				throw new FtpException("Malformed PASV response: " + reply.Message);
			}

			// retrieve the advertised advertised port
			port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);

			// PASVEX mode ignores the IPAD advertised in the PASV response and overrides all other concerns
			if (type == FtpDataConnectionType.PASVEX) {
				LogWithPrefix(FtpTraceLevel.Verbose, "PASV advertised IPAD ignored (PASVEX). Using original connect dnsname/IPAD");
				host = m_host; // m_host is the original connect dnsname/IPAD for the control connection
				return;
			}

			// Pick up the IPAD the server advertises
			host = m.Groups["quad1"].Value + "." + m.Groups["quad2"].Value + "." + m.Groups["quad3"].Value + "." + m.Groups["quad4"].Value;

			// Is the advertised IPAD routable?
			var mP = Regex.Match(host, @"(^10\.)|(^172\.1[6-9]\.)|(^172\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)|(^127\.0\.0\.1)|(^0\.0\.0\.0)");
			if (!mP.Success) {
				// Routable IPAD, simply use the IPAD advertised by the PASV response
				return;
			}

			// PASVUSE mode forces the advertised IPAD to be used, even if not routable,
			// which can be useful for connections WITHIN private networks, or with proxys
			if (type == FtpDataConnectionType.PASVUSE) {
				LogWithPrefix(FtpTraceLevel.Verbose, "PASV advertised non-routable IPAD will be force-used (PASVUSE)");
				return;
			}

			// Non-routable PASV IPAD advertisement so ignore the advertised IPAD,
			// use the original connect dnsname/IPAD for the connection
			// This VERY OFTEN works (often also with the help of DNS), but not always.
			// If you NEED to connect via the non-routable IPAD, you must use "PASVUSE" mode
			LogWithPrefix(FtpTraceLevel.Verbose, "PASV advertised a non-routable IPAD. Using original connect dnsname/IPAD");
			host = m_host;
		}

		/// <summary>
		/// Returns the IPAD to be sent to the server for the active connection.
		/// </summary>
		/// <param name="ipad"></param>
		/// <returns></returns>
		protected string GetLocalAddress(IPAddress ipad) {

			// Use resolver
			if (Config.AddressResolver != null) {
				return m_Address ?? (m_Address = Config.AddressResolver());
			}

			// Use supplied IPAD
			return ipad.ToString();
		}
	}
}
