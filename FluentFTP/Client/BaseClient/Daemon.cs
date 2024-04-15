using System;
using System.Diagnostics;
using System.Threading;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Daemon for NOOP handling
		/// </summary>
		protected void Daemon() {

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Daemon is initialized");

			Status.DaemonRunning = true;
			Status.DaemonCmdMode = true;
			Status.DaemonEnable = true;
			Status.DaemonAnyNoops = false;

			bool gotEx = false;

			do { // while(true)

				if (!IsConnected) {
					break;
				}

				if (Status.DaemonEnable) {

					Random rnd = new Random();

					// choose one of the normal or the safe commands
					string rndCmd = Status.DaemonCmdMode ?
						Config.NoopInactiveCommands[rnd.Next(Config.NoopInactiveCommands.Count)] :
						Config.NoopActiveCommands[rnd.Next(Config.NoopActiveCommands.Count)];

					m_sema.Wait();
					try {
						if (Config.NoopInterval > 0 && DateTime.UtcNow.Subtract(LastCommandTimestamp).TotalMilliseconds > Config.NoopInterval) {

							LastCommandTimestamp = DateTime.UtcNow;

							// only log this if we have an active data connection
							if (Status.DaemonCmdMode) {
								Log(FtpTraceLevel.Verbose, "Command:  " + rndCmd + " (<-Daemon)");
							}
							else {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Sending " + rndCmd + " (<-Daemon)");
							}

							// send the random NOOP command
							try {
								m_stream.WriteLine(m_textEncoding, rndCmd);
							}
							catch (Exception ex) {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#1): " + ex.Message + " (Daemon)");
								gotEx = true;
							}

							if (!gotEx) {

								// tell the outside world, NOOPs have actually been sent.
								Status.DaemonAnyNoops = true;

								// pick the command reply if this is just an idle control connection
								if (Status.DaemonCmdMode) {
									bool success = false;
									try {
										success = ((IInternalFtpClient)this).GetReplyInternal(rndCmd + " (<-Daemon)", false, 10000, false).Success;
									}
									catch (Exception ex) {
										((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#2): " + ex.Message + " (Daemon)");
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
						((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#3): " + ex.Message + " (Daemon)");
						gotEx = true;
					}
					finally {
						if (gotEx) {
							if (m_stream != null) {
								m_stream.Close();
								m_stream = null;
							}
						}
						m_sema.Release();
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

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Daemon terminated" + reason);
			Status.DaemonRunning = false;
		}
	}
}
