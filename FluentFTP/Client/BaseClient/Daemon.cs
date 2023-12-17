using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		static List<string> rndNormalCmds = new List<string> { "NOOP", "PWD", "TYPE I", "TYPE A" };
		static List<string> rndSafeCmds = new List<string> { "NOOP" };

		public void Daemon() {

			LogWithPrefix(FtpTraceLevel.Verbose, "Daemon initialized");
			Status.DaemonRunning = true;
			Status.DaemonGetReply = true;
			Status.DaemonEnable = true;
			Status.DaemonAnyNoops = false;

			do {

				if (m_stream == null || !m_stream.IsConnected) {
					LogWithPrefix(FtpTraceLevel.Verbose, "Daemon terminated");
					Status.DaemonRunning = false;
					return;
				}

				if (Status.DaemonEnable && Config.NoopInterval > 0 && DateTime.UtcNow.Subtract(LastCommandTimestamp).TotalMilliseconds > Config.NoopInterval) {

					Random rnd = new Random();

					string rndCmd = Status.DaemonGetReply ? rndNormalCmds[rnd.Next(rndNormalCmds.Count)] : rndNormalCmds[rnd.Next(rndSafeCmds.Count)];

					if (!Status.DaemonGetReply) {
						LogWithPrefix(FtpTraceLevel.Verbose, "Sending " + rndCmd + " (daemon)");
					}

					m_stream.WriteLine(m_textEncoding, rndCmd);

					LastCommandTimestamp = DateTime.UtcNow;

					Status.DaemonAnyNoops = true;

					if (Status.DaemonGetReply) {
						bool s = GetReplyInternal(rndCmd + " (daemon)", false, 10000).Success;

						if (s) {
							if (rndCmd.StartsWith("Type I")) {
								Status.CurrentDataType = FtpDataType.Binary;
							}

							if (rndCmd.StartsWith("Type A")) {
								Status.CurrentDataType = FtpDataType.ASCII;
							}
						}
					}

				}

				Thread.Sleep(100);

			} while (true);

		}

	}
}
