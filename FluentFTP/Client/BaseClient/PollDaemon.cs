using System;
using System.Diagnostics;
using System.Threading;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// PollDaemon for Poll handling
		/// </summary>
		protected void PollDaemon(CancellationToken ct) {

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "PollDaemon(" + this.ClientType + ") is initialized, PollIntervalControl = " + Config.PollIntervalControl + ", PollIntervalData = " + Config.PollIntervalData);

			Status.PollDaemonEnable = false;
			Status.PollDaemonAnyControlPolls = 0;
			Status.PollDaemonAnyDataPolls = 0;

			bool streamConnected = false;
			bool gotEx = false;

			do { // while(true)

				if (ct.IsCancellationRequested) {
					break;
				}

				if (!IsConnected) {
					continue;
				}

				if (Status.PollDaemonEnable) {

					m_daemonSemaphore.Wait(ct);

					if (Config.PollIntervalControl > 0 &&
						DateTime.UtcNow.Subtract(m_stream.m_lastActivity).TotalMilliseconds > Config.PollIntervalControl) {

						((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Invoking a socket.Poll(control) (<-PollDaemon)");


						// send the random Poll command
						try {
							streamConnected = m_stream.Poll();
							if (!streamConnected) {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got inactive control connection (PollDaemon)");
								gotEx = true;
							}
							else {
								// tell the outside world, NOOPs have actually been sent.
								Status.PollDaemonAnyControlPolls += 1;
							}
						}
						catch (Exception ex) {
							((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#1): " + ex.Message + " (PollDaemon)");
							gotEx = true;
						}
						finally {
							if (gotEx) {
								if (m_stream != null) {
									m_stream.Close();
									m_stream = null;
								}
							}
						}
					}

					m_daemonSemaphore.Release();

				} // if (Status.PollDaemonEnable)

				if (gotEx) {
					break;
				}


				if (m_datastream == null) {
					Thread.Sleep(250);
					continue;
				}

				if (Status.PollDaemonEnable) {

					m_daemonSemaphore.Wait(ct);

					if (Config.PollIntervalData > 0 &&
						DateTime.UtcNow.Subtract(m_datastream.m_lastActivity).TotalMilliseconds > Config.PollIntervalData) {


						((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Invoking a socket.Poll(data) (<-PollDaemon)");

						// send the random Poll command
						try {
							streamConnected = m_datastream.Poll();
							if (!streamConnected) {
								((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got inactive data connection (PollDaemon)");
								gotEx = true;
							}
							else {
								// tell the outside world, NOOPs have actually been sent.
								Status.PollDaemonAnyDataPolls += 1;
							}
						}
						catch (Exception ex) {
							((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "Got exception (#1): " + ex.Message + " (PollDaemon)");
							gotEx = true;
						}
						finally {
							if (gotEx) {
								if (m_datastream != null) {
									m_datastream.Close();
									m_datastream = null;
									gotEx = false;
								}
							}
						}
					}

					m_daemonSemaphore.Release();

				} // if (Status.PollDaemonEnable)

				Thread.Sleep(250);

			} while (true);

			string reason = string.Empty;
			if (gotEx) {
				reason = " due to a detected connection problem";
			}

			((IInternalFtpClient)this).LogStatus(FtpTraceLevel.Verbose, "PollDaemon terminated" + reason);
		}
	}
}