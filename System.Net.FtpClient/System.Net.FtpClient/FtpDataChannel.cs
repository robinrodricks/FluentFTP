using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace System.Net.FtpClient {
	public class FtpDataChannel : FtpChannel {
		FtpCommandChannel _cmdChan = null;
		/// <summary>
		/// The command channel that opened this data channel
		/// </summary>
		public FtpCommandChannel CommandChannel {
			get { return _cmdChan; }
			private set { _cmdChan = value; }
		}

		/// <summary>
		/// Closes the data channel and reads the final response from the command channel if
		/// the last command completed successfully
		/// </summary>
		public override void Disconnect() {
			bool connected = this.Connected;

			base.Disconnect();
			// if we were connected and the data channel was use successfully
			// the server will send a response when the data channel is closed.
			if (connected && this.CommandChannel.Connected && this.CommandChannel.ResponseStatus) {
				if (!this.CommandChannel.ReadResponse()) {
					throw new FtpException(this.CommandChannel.ResponseMessage);
				}
			}
		}

		/// <summary>
		/// Cleans up any resources the DataChannel was using, also
		/// terminates any active connections.
		/// </summary>
		public new void Dispose() {
			base.Dispose();
			this.CommandChannel = null;
		}

		/// <summary>
		/// Sets up the SSL stream for FTPS
		/// </summary>
		void SetupSsl() {
			if (this.CommandChannel.SslEnabled) {
				this.IgnoreInvalidSslCertificates = this.CommandChannel.IgnoreInvalidSslCertificates;
				this.SslEnabled = true;
			}
		}

		public FtpDataChannel(FtpCommandChannel cmdchan) {
			this.CommandChannel = cmdchan;
			this.ConnectionReady += new FtpChannelConnected(SetupSsl);
		}
	}
}
