// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalPorts.cs" company="">
//   
// </copyright>
// <summary>
//   The local ports.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FluentFTP.Helpers
{
	#region Usings

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Net.NetworkInformation;

	#endregion

	/// <summary>
	/// The local ports.
	/// </summary>
	public static class LocalPorts
	{
		/// <summary>
		/// The r.
		/// </summary>
		internal static readonly Random R = new Random();

		/// <summary>
		/// Get available.
		/// </summary>
		/// <param name="localIpAddress">
		/// The local ip address.
		/// </param>
		/// <returns>
		/// The <see cref="int"/>.
		/// </returns>
		public static int GetRandomAvailable(IPAddress localIpAddress)
		{
			lock (R)
			{
				var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
				var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
				var inUsePorts = new HashSet<int>(
					tcpConnInfoArray.Where(ipEndPoint => localIpAddress.Equals(ipEndPoint.Address))
						.Select(ipEndPoint => ipEndPoint.Port));
				int localPort;
				do
				{
					localPort = 1025 + R.Next(32000);
				}
				while (inUsePorts.Contains(localPort));

				return localPort;
			}
		}
	}
}