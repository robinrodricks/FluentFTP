using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using FluentFTP.Rules;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {


		/// <summary>
		/// Parse the host and port number from an EPSV response
		/// Handles (|||nnnn|) and (!!!nnnn!)
		/// </summary>
		protected void GetEnhancedPassivePort(FtpReply reply, out string host, out int port) {
			var m = Regex.Match(reply.Message, @"\([!\|][!\|][!\|](?<port>\d+)[!\|]\)");
			if (!m.Success) {
				// In the case that ESPV is responded with a regular "Entering Passive Mode" instead, we'll try that parsing before we raise the exception
				/* Example:
				Command: EPSV
				Response: 227 Entering Passive Mode(XX, XX, XX, XX, 143, 225).
				*/

				try {
					GetPassivePort(FtpDataConnectionType.AutoPassive, reply, out host, out port);
					return;
				}
				catch {
					throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
				}
			}
			// If ESPV is responded with Entering Extended Passive. The IP must remain the same.
			/* Example:
			Command: EPSV
			Response: 229 Entering Extended Passive Mode(|||10016|)

			If we set the host to ftp.host.com and ftp.host.com has multiple ip's we may end up with the wrong ip.
			Making sure that we use the same IP.
			host = m_host; 
			*/
			host = SocketRemoteEndPoint.Address.ToString();
			port = int.Parse(m.Groups["port"].Value);
		}

		/// <summary>
		/// Parse the host and port number from an PASV or PASVEX response
		/// </summary>
		protected void GetPassivePort(FtpDataConnectionType type, FtpReply reply, out string host, out int port) {
			var m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

			if (!m.Success || m.Groups.Count != 7) {
				throw new FtpException("Malformed PASV response: " + reply.Message);
			}

			// PASVEX mode ignores the host supplied in the PASV response
			if (type == FtpDataConnectionType.PASVEX) {
				host = m_host;
			}
			else {
				host = m.Groups["quad1"].Value + "." + m.Groups["quad2"].Value + "." + m.Groups["quad3"].Value + "." + m.Groups["quad4"].Value;
			}

			port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);

			// Fix #409 for BlueCoat proxy connections. This code replaces the name of the proxy with the name of the FTP server and then nothing works.
			if (!IsProxy()) {
				//use host ip if server advertises a non-routable IP
				m = Regex.Match(host, @"(^10\.)|(^172\.1[6-9]\.)|(^172\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)|(^127\.0\.0\.1)|(^0\.0\.0\.0)");

				if (m.Success) {
					host = m_host;
				}
			}
		}

		/// <summary>
		/// Returns the IP address to be sent to the server for the active connection.
		/// </summary>
		/// <param name="ip"></param>
		/// <returns></returns>
		protected string GetLocalAddress(IPAddress ip) {

			// Use resolver
			if (Config.AddressResolver != null) {
				return m_Address ?? (m_Address = Config.AddressResolver());
			}

			// Use supplied IP
			return ip.ToString();
		}

	}
}
