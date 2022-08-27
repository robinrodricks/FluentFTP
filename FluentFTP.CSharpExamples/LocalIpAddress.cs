using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpExamples {
	using System.Net;
	using System.Net.Sockets;

	using FluentFTP;

	public static class LocalIpAddress {
		public static void LocaIpAddressExample() {

			// IP addresses for current host inside myprivatedomain
			var localIpAddresses = new[]
									   {
										   IPAddress.Parse("10.244.191.143"),
										   IPAddress.Parse("fcec:177:cfbd:6555:8f8c::1")
									   };

			foreach (var localIpAddress in localIpAddresses) {

				// let's say that ftp.myprivatedomain has ipv4 and ipv5 addresses
				using var f = new FtpClient("ftp.myprivatedomain", "test", "test");

				f.Config.InternetProtocolVersions = localIpAddress.AddressFamily == AddressFamily.InterNetworkV6 ? FtpIpVersion.IPv6 : FtpIpVersion.IPv4;

				// Equivalent to lftp's ftp:port-ipv[4|6] and net:socket-bind-ipv[4|6] (see http://manpages.org/lftp)
				f.Config.SocketLocalIp = localIpAddress;
				{
					f.Connect();

					Console.WriteLine($"Connected to {f.SocketRemoteEndPoint} from {f.SocketLocalEndPoint}");
					foreach (var file in f.GetListing()) {
						Console.Out.WriteLine(file);
					}

					f.Disconnect();
				}
			}
		}

	}
}