using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;
using System.Security.Authentication;
using FluentFTP.Proxy.SyncProxy;

namespace FluentFTP {
	public partial class FtpClient {


		/// <summary>
		/// Attempt to find out the servers SSL command limit.
		/// </summary>
		/// <param name="command">The command to issue</param>
		/// <param name="maxTrys">Maximum how many commands to issue</param>
		/// <returns>The detected command limit, 0 if infinite</returns>
		public int DiscoverSslSessionLength(string command = "PWD", int maxTrys = 2000) {
			if (!IsEncrypted) {
				return 0;
			}

			int connects = Status.ConnectCount;

			int oldLength = Config.SslSessionLength;

			Config.SslSessionLength = 0;

			for (int i = 0; i < maxTrys; i++) {
				Console.WriteLine("Try " + i);
				try {
					Execute(command);
				}
				catch {
					Log(FtpTraceLevel.Verbose, "Exception: ");
					break;
				}

				if (Status.ConnectCount > connects) {
					Log(FtpTraceLevel.Verbose, "Reconnect detected");
					break;
				}
			}

			Execute(command);

			if (Status.ConnectCount > connects) {
				Log(FtpTraceLevel.Verbose, "Failure ocurred at: " + m_stream.SslSessionLength);
				return m_stream.SslSessionLength;
			}

			Config.SslSessionLength = oldLength;

			return 0;
		}
	}
}