using System;
using System.Threading;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// NoopDaemon for NOOP handling
		/// </summary>
		protected void NoopDaemon() {

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "NoopDaemon is initialized");

			Status.NoopDaemonRunning = true;
			Status.NoopDaemonCmdMode = true;
			Status.NoopDaemonEnable = true;
			Status.NoopDaemonAnyNoops = false;

			bool gotEx = false;

			do { // while(true)

				if (!IsConnected) {
					break;
				}

				if (Status.NoopDaemonEnable) {

					Random rnd = new Random();

					// choose one of the normal or the safe commands
					string rndCmd = Status.NoopDaemonCmdMode ?
						Config.NoopInactiveCommands[rnd.Next(Config.NoopInactiveCommands.Count)] :
						Config.NoopActiveCommands[rnd.Next(Config.NoopActiveCommands.Count)];

					m_NoopSema.Wait();
					try {
						if (Config.NoopInterval > 0 && DateTime.UtcNow.Subtract(LastCommandTimestamp).TotalMilliseconds > Config.NoopInterval) {

							LastCommandTimestamp = DateTime.UtcNow;

							// only log this if we have an active data connection
							if (Status.NoopDaemonCmdMode) {
								Log(FtpTraceLevel.Verbose, "Command:  " + rndCmd + " (<-NoopDaemon)");
							}
							else {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Sending " + rndCmd + " (<-NoopDaemon)");
							}

							// send the random NOOP command
							try {
								m_stream.WriteLine(m_textEncoding, rndCmd);
							}
							catch (Exception ex) {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#1): " + ex.Message + " (NoopDaemon)");
								gotEx = true;
							}

							if (!gotEx) {

								// tell the outside world, NOOPs have actually been sent.
								Status.NoopDaemonAnyNoops = true;

								// pick the command reply if this is just an idle control connection
								if (Status.NoopDaemonCmdMode) {
									bool success = false;
									try {
										success = ((IInternalFtpClient)this).GetReplyInternal(rndCmd + " (<-NoopDaemon)", false, 10000, false).Success;
									}
									catch (Exception ex) {
										((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#2): " + ex.Message + " (NoopDaemon)");
										gotEx = true;
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
						}
					}
					catch (Exception ex) {
						((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#3): " + ex.Message + " (NoopDaemon)");
						gotEx = true;
					}
					finally {
						if (gotEx) {
							if (m_stream != null) {
								m_stream.Close();
								m_stream = null;
							}
						}
						m_NoopSema.Release();
					}
				}

				if (gotEx) {
					break;
				}

				Thread.Sleep(100);

			} while (true);

			string reason = string.Empty;
			if (gotEx) {
				reason = " due to detected connection problem";
			}

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "NoopDaemon terminated" + reason);
			Status.NoopDaemonRunning = false;
		}
	}
}
