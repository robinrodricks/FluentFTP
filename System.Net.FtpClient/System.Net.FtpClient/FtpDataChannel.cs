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
		/// The local port we are going out or listening on
		/// </summary>
		public Int32 LocalPort {
			get {
				// need to test if the socket has been created first!
				return ((IPEndPoint)this.Socket.LocalEndPoint).Port;
			}
		}

		/// <summary>
		/// The local IP address the socket is using
		/// </summary>
		public IPAddress LocalIPAddress {
			get {
				// need to test if the socket has been created first!
				return ((IPEndPoint)this.Socket.LocalEndPoint).Address;
			}
		}

		/// <summary>
		/// The report port we are connected to
		/// </summary>
		public Int32 RemotePort {
			get {
				if (this.Connected) {
					return ((IPEndPoint)this.Socket.RemoteEndPoint).Port;
				}

				return 0;
			}
		}

		/// <summary>
		/// The remote IP address this socket is connected to
		/// </summary>
		public IPAddress RemoteIPAddress {
			get {
				if (this.Connected) {
					return ((IPEndPoint)this.Socket.RemoteEndPoint).Address;
				}

				return null;
			}
		}

		/// <summary>
		/// Base network stream, could be NetworkStream or SslStream
		/// depending on if ssl is enabled.
		/// </summary>
		public override Stream BaseStream {
			get {
				// authenticate the stream if it isn't already
				if (this.CommandChannel.SslEnabled && !this.SecurteStream.IsAuthenticated) {
					this.IgnoreInvalidSslCertificates = this.CommandChannel.IgnoreInvalidSslCertificates;
					this.AuthenticateConnection();
				}

				return base.BaseStream;
			}
		}

		/// <summary>
		/// Connects active or passive channels
		/// </summary>
		public override void Connect() {
			if (!this.Connected) {
				if (this.CommandChannel.DefaultDataMode == FtpDataMode.Active) {
					this.ConnectActiveChannel();
				}
				else {
					base.Connect();
				}
			}
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
		/// Intializes the active channel socket
		/// </summary>
		public void InitalizeActiveChannel() {
			this.Socket.Bind(new IPEndPoint(((IPEndPoint)this.CommandChannel.LocalEndPoint).Address, 0));
			this.Socket.Listen(1);
#if DEBUG
			System.Diagnostics.Debug.WriteLine(string.Format("Active channel initalized and waiting: {0}:{1}",
				this.LocalIPAddress, this.LocalPort));
#endif
		}
				
		public void ConnectActiveChannel() {
			Socket s = this.Socket.Accept();

			this.Socket.Close();
			this.Socket = null;
			this.Socket = s;
			this.IgnoreInvalidSslCertificates = this.CommandChannel.IgnoreInvalidSslCertificates;
			this.AuthenticateConnection();
#if DEBUG
			System.Diagnostics.Debug.WriteLine(string.Format("Connected from: {0}:{1}",
				this.RemoteIPAddress, this.RemotePort));
#endif
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
		/// Sets up the SSL stream for FTPS. This will only be
		/// called on passive connections, port/eprt has to be
		/// setup after the server connects to us.
		/// </summary>
		void SetupSsl() {
			
		}

		public FtpDataChannel(FtpCommandChannel cmdchan) {
			this.CommandChannel = cmdchan;
			this.ConnectionReady += new FtpChannelConnected(SetupSsl);
		}
	}
}
