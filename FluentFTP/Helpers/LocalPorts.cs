namespace FluentFTP.Helpers {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Net.NetworkInformation;

	/// <summary>
	/// The local ports.
	/// </summary>
	internal static class LocalPorts {
#if ASYNC && !CORE14 && !CORE16
		internal static readonly Random randomGen = new Random();

		/// <summary>
		/// Get random local port for the given local IP address
		/// </summary>
		public static int GetRandomAvailable(IPAddress localIpAddress) {
			lock (randomGen) {
				var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
				var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
				var inUsePorts = new HashSet<int>(
					tcpConnInfoArray.Where(ipEndPoint => localIpAddress.Equals(ipEndPoint.Address))
						.Select(ipEndPoint => ipEndPoint.Port));
				int localPort;
				do {
					localPort = 1025 + randomGen.Next(32000);
				}
				while (inUsePorts.Contains(localPort));

				return localPort;
			}
		}
#endif
	}
}