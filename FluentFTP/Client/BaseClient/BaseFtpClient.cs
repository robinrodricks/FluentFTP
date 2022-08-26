using FluentFTP.Client.Modules;
using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient : IDisposable, IInternalFtpClient {


		#region Clone

		/// <summary>
		/// Clones the FTP client control connection. Used for opening multiple data streams.
		/// You will need to manually connect after cloning.
		/// </summary>
		/// <returns>A new FTP client connection with the same property settings as this one.</returns>
		public BaseFtpClient Clone() {
			var newClone = Create();

			newClone.m_isClone = true;

			CloneModule.Clone(this, newClone);

			newClone.CurrentDataType = CurrentDataType;
			newClone.ForceSetDataType = true;

			return newClone;
		}


		#endregion

		#region Constructor

		public BaseFtpClient() {
			m_listParser = new FtpListParser(this);
		}

		#endregion

		#region Destructor

		/// <summary>
		/// Disposes and disconnects this FTP client if it was auto-created for an internal operation.
		/// </summary>
		public void AutoDispose() {
			if (Status.AutoDispose) {
				Dispose();
			}
		}
		/// <summary>
		/// Check if the host parameter is valid
		/// </summary>
		/// <param name="host"></param>
		protected string ValidateHost(Uri host) {
			if (host == null) {
				throw new ArgumentNullException(nameof(host), "Host is required");
			}
#if !NETSTANDARD
			if (host.Scheme != Uri.UriSchemeFtp) {
				throw new ArgumentException("Host is not a valid FTP path");
			}
#endif
			return host.Host;
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected virtual BaseFtpClient Create() {
			return new BaseFtpClient();
		}

		/// <summary>
		/// Disconnects from the server, releases resources held by this
		/// object.
		/// </summary>
		public virtual void Dispose() {
			lock (m_lock) {
				if (IsDisposed) {
					return;
				}

				// Fix: Hard catch and suppress all exceptions during disposing as there are constant issues with this method
				try {
					LogFunc(nameof(Dispose));
					LogStatus(FtpTraceLevel.Verbose, "Disposing FtpClient object...");
				}
				catch (Exception ex) {
				}

				try {
					if (IsConnected) {
						((IInternalFtpClient)this).DisconnectInternal();
					}
				}
				catch (Exception ex) {
				}

				if (m_stream != null) {
					try {
						m_stream.Dispose();
					}
					catch (Exception ex) {
					}

					m_stream = null;
				}

				try {
					m_credentials = null;
					m_textEncoding = null;
					m_host = null;
				}
				catch (Exception ex) {
				}

				IsDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		void IInternalFtpClient.DisconnectInternal() {
		}

		void IInternalFtpClient.ConnectInternal() {
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~BaseFtpClient() {
			Dispose();
		}

		#endregion



	}
}
