using System;
using System.Net.Sockets;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Open a local port on the given ActivePort or a random port.
		/// </summary>
		/// <param name="stream"></param>
		protected void StartListeningOnPort(FtpDataStream stream) {
			if (m_ActivePorts.IsBlank()) {
				// Use random port
				stream.Listen(m_stream.LocalEndPoint.Address, 0);
			}
			else {
				var success = false;

				// Use one of the specified ports
				foreach (var port in m_ActivePorts) {
					try {
						stream.Listen(m_stream.LocalEndPoint.Address, port);
						success = true;
						break;
					}
					catch (SocketException se) {
						if (se.SocketErrorCode != SocketError.AddressAlreadyInUse) {
							throw;
						}
					}
				}

				// No usable port found
				if (!success) {
					throw new Exception("No valid active data port available!");
				}
			}
		}


	}
}
