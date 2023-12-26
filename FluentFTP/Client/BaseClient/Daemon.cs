using System;
using System.Collections.Generic;
using System.Threading;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		protected void Daemon() {

			LogWithPrefix(FtpTraceLevel.Verbose, "Daemon initialized");

			Status.DaemonRunning = true;
			Status.DaemonCmdMode = true;
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
					string rndCmd = Status.DaemonCmdMode ?
						Config.NoopInactiveCommands[rnd.Next(Config.NoopInactiveCommands.Count)] :
						Config.NoopActiveCommands[rnd.Next(Config.NoopActiveCommands.Count)];

					m_sema.Wait();
					try {
						// only log this if we have an active data connection
						if (!Status.DaemonCmdMode) {
							LogWithPrefix(FtpTraceLevel.Verbose, "Sending " + rndCmd + " (daemon)");
						}

						// send the random NOOP command
						m_stream.WriteLine(m_textEncoding, rndCmd);

						LastCommandTimestamp = DateTime.UtcNow;

						// tell the outside world, NOOPs have actually been sent.
						Status.DaemonAnyNoops = true;

						// pick the command reply if this is just an idle control connection
						if (Status.DaemonCmdMode) {
							bool s = ((IInternalFtpClient)this).GetReplyInternal(rndCmd + " (daemon)", false, 10000, false).Success;

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
					finally
					{
						m_sema.Release();
					}
				}

				Thread.Sleep(100);

			} while (true);

		}

	}
}
