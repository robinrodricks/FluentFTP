using FluentFTP.Helpers;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient : IDisposable, IInternalFtpClient {


		#region Constructor

		public BaseFtpClient(FtpConfig config) {
			CurrentListParser = new FtpListParser(this);
			Config = config ?? new FtpConfig();
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected virtual BaseFtpClient Create() {
			return new BaseFtpClient(null);
		}
		#endregion

		#region Clone

		/// <summary>
		/// Clones the FTP client control connection. Used for opening multiple data streams.
		/// You will need to manually connect after cloning.
		/// </summary>
		/// <returns>A new FTP client connection with the same property settings as this one.</returns>
		public BaseFtpClient Clone() {
			var newClone = Create();

			newClone.m_isClone = true;

			CloneClient(this, newClone);

			newClone.Status.CurrentDataType = FtpDataType.Unknown;

			return newClone;
		}

		private static void CloneClient(BaseFtpClient read, BaseFtpClient write) {

			// configure new connection as clone of self
			write.Host = read.Host;
			write.Port = read.Port;
			write.Credentials = read.Credentials;
			write.ServerHandler = read.ServerHandler;
			write.Encoding = read.Encoding;

			// copy config
			write.Config = read.Config.Clone();

			// copy capabilities
			try {
				write.SetFeatures(read.Capabilities);
			}
			catch {
			}

			// always accept certificate no matter what because if code execution ever
			// gets here it means the certificate on the control connection object being
			// cloned was already accepted.
			write.ValidateCertificate += new FtpSslValidation(
				delegate (BaseFtpClient obj, FtpSslValidationEventArgs e) { e.Accept = true; });

		}

		#endregion

		#region Destructor

		/// <summary>
		/// Disposes and disconnects this FTP client if it was auto-created for an internal operation.
		/// </summary>
		public void AutoDispose() {
			if (Status.AutoDispose) {
				if (this is AsyncFtpClient) {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

					Task.Run(() => ((IAsyncFtpClient)this).DisposeAsync()).Wait();
#else
					Task.Run(() => ((IAsyncFtpClient)this).Dispose()).Wait();
#endif
				}
				else {
					((IFtpClient)this).Dispose();
				}
			}
		}

		public void WaitForDaemonTermination() {
			if (Config.Noop) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Waiting for Daemon termination(" + this.ClientType + ")");
				Status.NoopDaemonTokenSource.Cancel();
				DateTime startTime = DateTime.UtcNow;
				while (Status.NoopDaemonTask != null && Status.NoopDaemonTask.Status == TaskStatus.Running &&
					DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < 20000) {
					Thread.Sleep(250);
				}
				LogWithPrefix(FtpTraceLevel.Verbose, "Daemon terminated");
			}
		}

		/// <summary>
		/// Disconnects from the server, releases resources held by this
		/// object.
		/// </summary>
		public virtual void Dispose() {
			if (IsDisposed) {
				return;
			}

			LogFunction(nameof(Dispose));
			LogWithPrefix(FtpTraceLevel.Verbose, "Disposing(sync) " + this.ClientType);

			((IInternalFtpClient)this).DisconnectInternal();

			if (m_stream != null) {
				m_stream.Dispose();
				m_stream = null;
			}

			m_credentials = null;
			m_textEncoding = null;
			m_host = null;

			WaitForDaemonTermination();

			IsDisposed = true;

			GC.SuppressFinalize(this);
		}

#endregion


	}
}
