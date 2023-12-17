using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		// same as what FileZilla does
		static List<string> rndNormalCmds = new List<string> { "NOOP", "PWD", "TYPE I", "TYPE A" };
		// only NOOP for now
		static List<string> rndSafeCmds = new List<string> { "NOOP" };

		public void Daemon() {

			LogWithPrefix(FtpTraceLevel.Verbose, "Daemon initialized");

			Status.DaemonRunning = true;
			Status.DaemonGetReply = true;
			Status.DaemonEnable = true;
			Status.DaemonAnyNoops = false;

			do { // while(true)

				if (m_stream == null || !m_stream.IsConnected) {
					LogWithPrefix(FtpTraceLevel.Verbose, "Daemon terminated");
					Status.DaemonRunning = false;
					return;
				}

				if (Status.DaemonEnable && Config.NoopInterval > 0 && DateTime.UtcNow.Subtract(LastCommandTimestamp).TotalMilliseconds > Config.NoopInterval) {

					Random rnd = new Random();

					// choose one of the normal or the safe commands
					string rndCmd = Status.DaemonGetReply ?
						rndNormalCmds[rnd.Next(rndNormalCmds.Count)] :
						rndNormalCmds[rnd.Next(rndSafeCmds.Count)];

					// only log this if we have an active data connection
					if (!Status.DaemonGetReply) {
						LogWithPrefix(FtpTraceLevel.Verbose, "Sending " + rndCmd + " (daemon)");
					}

					// send the random NOOP command
					m_stream.WriteLine(m_textEncoding, rndCmd);

					LastCommandTimestamp = DateTime.UtcNow;

					// tell the outside world, NOOPs have actually been sent.
					Status.DaemonAnyNoops = true;

					// pick the command reply if this is just an idle control connection
					if (Status.DaemonGetReply) {

						bool s = GetReplyInternal(rndCmd + " (daemon)", false, 10000).Success;

						// in case one of these commands is issued, make sure we store that

						if (s) {
							if (rndCmd.StartsWith("TYPE I")) {
								Status.CurrentDataType = FtpDataType.Binary;
							}

							if (rndCmd.StartsWith("TYPE A")) {
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
