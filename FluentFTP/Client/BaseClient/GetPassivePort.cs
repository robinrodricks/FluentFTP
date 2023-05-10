using System.Net;
using System.Text.RegularExpressions;
using FluentFTP.Exceptions;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {


		/// <summary>
		/// Parse the advertisedIpad and advertisedPort number from an EPSV response
		/// Handles (|||nnnn|) and (!!!nnnn!)
		/// </summary>
		protected void GetEnhancedPassivePort(FtpReply reply, out string advertisedIpad, out int advertisedPort) {
			// Check the format of the EPSV response, respecting the code page problems of the vertical bar ! vs. |
			var m = Regex.Match(reply.Message, @"\([!\|][!\|][!\|](?<advertisedPort>\d+)[!\|]\)");

			if (!m.Success) {
				// In the case that ESPV is responded with a regular "Entering Passive Mode" instead, we'll try that parsing before we raise the exception
				/* Example:
				Command: EPSV
				Response: 227 Entering Passive Mode(XX, XX, XX, XX, 143, 225).
				*/
				try {
					GetPassivePort(FtpDataConnectionType.AutoPassive, reply, out advertisedIpad, out advertisedPort);
					return;
				}
				catch {
					throw new FtpException("Failed to get the EPSV advertisedPort from: " + reply.Message);
				}
			}

			// retrieve the advertised advertisedPort
			advertisedPort = int.Parse(m.Groups["advertisedPort"].Value);

			// If ESPV is responded with Entering Extended Passive, the IPAD must remain the same as the control connection.

			/* Example:
			Command: EPSV
			Response: 229 Entering Extended Passive Mode(|||10016|)

			If the server (per hostname DNS query) has multiple IPAD's we will **NOT** end up with the wrong IPAD, because:
			On the connect we have stored the previously used IPAD in the hostname/IPAD cache, replacing the list of IPADs
			that DNS gave us by the single one that we successfully connected to.

			Therefore the subsequent connects will use this single stored IPAD instead of querying DNS and getting a list of IPADs.
			*/

			// Pick up the IPAD the server advertises
			advertisedIpad = m_host; // m_host is the original connect dnsname/IPAD for the control connection
		}

		/// <summary>
		/// Parse the advertisedIpad and advertisedPort number from an PASV or PASVEX response
		/// </summary>
		protected void GetPassivePort(FtpDataConnectionType type, FtpReply reply, out string host, out int port) {
			// Check the format of the PASV response
			var m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

			if (!m.Success || m.Groups.Count != 7) {
				throw new FtpException("Malformed PASV response: " + reply.Message);
			}

			// retrieve the advertised advertisedPort
			port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);

			// PASVEX mode ignores the IPAD advertised in the PASV response and overrides all other concerns
			if (type == FtpDataConnectionType.PASVEX) {
				host = m_host; // m_host is the original connect dnsname/IPAD for the control connection
				return;
			}

			// Pick up the IPAD the server advertises
			host = m.Groups["quad1"].Value + "." + m.Groups["quad2"].Value + "." + m.Groups["quad3"].Value + "." + m.Groups["quad4"].Value;

			// Is the PASV IPAD routable?
			var mP = Regex.Match(host, @"(^10\.)|(^172\.1[6-9]\.)|(^172\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)|(^127\.0\.0\.1)|(^0\.0\.0\.0)");
			if (!mP.Success) {
				return; // Routable IPAD, this is the best scenario, simply use the IPAD advertised by the PASV reply
			}

			// PASVUSE mode forces the advertised IPAD to be used, even if not routable - useful for connections: WITHIN private networks, or with proxys
			if (type == FtpDataConnectionType.PASVUSE) {
				return;
			}

			// Not routable? Use the original connect dnsname/IPAD
			host = m_host;			
		}

		/// <summary>
		/// Returns the IP address to be sent to the server for the active connection.
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
