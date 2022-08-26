using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using FluentFTP.Proxy;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;
using System.Text.RegularExpressions;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

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

		public BaseFtpClient(){
			m_listParser = new FtpListParser(this);
		}

		#endregion

		#region Destructor

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

		#region FTPS

		/// <summary>
		/// Catches the socket stream ssl validation event and fires the event handlers
		/// attached to this object for validating SSL certificates
		/// </summary>
		/// <param name="stream">The stream that fired the event</param>
		/// <param name="e">The event args used to validate the certificate</param>
		protected void FireValidateCertficate(FtpSocketStream stream, FtpSslValidationEventArgs e) {
			OnValidateCertficate(e);
		}

		/// <summary>
		/// Fires the SSL validation event
		/// </summary>
		/// <param name="e">Event Args</param>
		protected void OnValidateCertficate(FtpSslValidationEventArgs e) {

			// automatically validate if ValidateAnyCertificate is set
			if (ValidateAnyCertificate) {
				e.Accept = true;
				return;
			}

			// fallback to manual validation using the ValidateCertificate event
			m_ValidateCertificate?.Invoke(this, e);

		}

		#endregion


	}
}