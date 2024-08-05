using System;
using System.Threading;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// NoopDaemon for NOOP handling
		/// </summary>
		protected void NoopDaemon(CancellationToken ct) {

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "NoopDaemon(" + this.ClientType + ") is initialized, NoopInterval = " + Config.NoopInterval + "ms");

			Random rnd = new Random();

			Status.NoopDaemonEnable = true;
			Status.NoopDaemonAnyNoops = 0;
			Status.NoopDaemonCmdMode = true;

			do { // while(true)

				if (ct.IsCancellationRequested) {
					break;
				}

				if (m_stream != null &&
					m_stream.ConnectionState == FtpConnectionState.Connected &&
					Status.NoopDaemonEnable) {

					bool gotEx = false;

					if (Config.NoopInterval > 0 &&
						DateTime.UtcNow.Subtract(LastCommandTimestamp).TotalMilliseconds > Config.NoopInterval) {

						m_daemonSemaphore.Wait(ct);

						try {
							// choose one of the normal or the safe commands
							string rndCmd = Status.NoopDaemonCmdMode ?
								Config.NoopInactiveCommands[rnd.Next(Config.NoopInactiveCommands.Count)] :
								Config.NoopActiveCommands[rnd.Next(Config.NoopActiveCommands.Count)];

							// only log this if we have an active data connection
							if (Status.NoopDaemonCmdMode) {
								Log(FtpTraceLevel.Verbose, "Command:  " + rndCmd + " (<-NoopDaemon)");
							}
							else {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Sending " + rndCmd + " (<-NoopDaemon)");
							}

							LastCommandTimestamp = DateTime.UtcNow;

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
								Status.NoopDaemonAnyNoops += 1;

								// pick the command reply if this is just an idle control connection
								if (Status.NoopDaemonCmdMode) {
									bool success = false;

									m_stream.ConnectionState = FtpConnectionState.Unknown;

									try {
										success = ((IInternalFtpClient)this).GetReplyInternal(rndCmd + " (<-NoopDaemon)", false, 10000, false).Success;
									}
									catch (Exception ex) {
										((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#2): " + ex.Message + " (NoopDaemon)");
										gotEx = true;
									}
									finally {
										LastCommandTimestamp = DateTime.UtcNow;
									}

									if (success) {
										m_stream.ConnectionState = FtpConnectionState.Connected;

										// in case one of these commands was successfully issued, make sure we store that
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
						catch (Exception ex) {
							((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#3): " + ex.Message + " (NoopDaemon)");
							gotEx = true;
						}

						m_daemonSemaphore.Release();
					}

					if (gotEx) {
						((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Indicating connection lost (NoopDaemon)");
						Status.NoopDaemonEnable = false;
						m_stream.ConnectionState = FtpConnectionState.PendingDisconnect;
					}


				}

				Thread.Sleep(250);

			} while (true);

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "NoopDaemon terminated");
		}
	}
}
