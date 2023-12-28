using System;
using System.Threading;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Daemon for NOOP handling
		/// </summary>
		protected void Daemon() {

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Daemon initialized");

			Status.DaemonRunning = true;
			Status.DaemonCmdMode = true;
			Status.DaemonEnable = true;
			Status.DaemonAnyNoops = false;

			bool gotEx = false;

			do { // while(true)

				if (m_stream == null || !m_stream.IsConnected) {
					break;
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
						if (Status.DaemonCmdMode) {
							Log(FtpTraceLevel.Verbose, "Command:  " + rndCmd + " (daemon)");
						}
						else {
							((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Sending " + rndCmd + " (daemon)");
						}

						// send the random NOOP command
						try {
							m_stream.WriteLine(m_textEncoding, rndCmd);
						}
						catch (Exception ex) {
							((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#1): " + ex.Message + " (daemon)");
							gotEx = true;
							break;
						}

						LastCommandTimestamp = DateTime.UtcNow;

						// tell the outside world, NOOPs have actually been sent.
						Status.DaemonAnyNoops = true;

						// pick the command reply if this is just an idle control connection
						if (Status.DaemonCmdMode) {
							bool success = false;
							try {
								success = ((IInternalFtpClient)this).GetReplyInternal(rndCmd + " (daemon)", false, 10000, false).Success;
							}
							catch (Exception ex) {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#2): " + ex.Message + " (daemon)");
								gotEx = true;
								break;
							}

							// in case one of these commands was successfully issued, make sure we store that
							if (success) {
								if (rndCmd.StartsWith("TYPE I")) {
									Status.CurrentDataType = FtpDataType.Binary;
								}

								if (rndCmd.StartsWith("TYPE A")) {
									Status.CurrentDataType = FtpDataType.ASCII;
								}
							}
						}
					}
					catch (Exception ex) {
						((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#3): " + ex.Message + " (daemon)");
						gotEx = true;
						break;
					}

					m_sema.Release();
				}

				Thread.Sleep(100);

			} while (true);

			if (gotEx) {
				m_stream.Close();
			}

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Daemon terminated");
			Status.DaemonRunning = false;
			m_sema.Release();
		}
	}
}
