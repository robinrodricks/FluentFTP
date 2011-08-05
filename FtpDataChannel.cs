using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

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
				if (this.CommandChannel.DataChannelEncryption && this.CommandChannel.SslEnabled && !this.SecurteStream.IsAuthenticated) {
					this.AuthenticateConnection();
				}

				return base.BaseStream;
			}
		}

		bool _ignoreStatus = false;
		/// <summary>
		/// Gets a value indicating if an exception should be thrown
		/// if an error response is received on the command channel
		/// when disconnecting this data channel
		/// </summary>
		public bool IgnoreStatus {
			get { return _ignoreStatus; }
			set { _ignoreStatus = value; }
		}

		bool _associatedCommandStatus = false;
		/// <summary>
		/// Gets the status of the command associated with this data channel. If you open a
		/// data channel and execute a command that succeeds, you must set this property to
		/// true so that when this data channel closes the a response can be read!
		/// </summary>
		public bool AssociatedCommandStatus {
			get { return _associatedCommandStatus; }
			private set { _associatedCommandStatus = value; }
		}

		/// <summary>
		/// Executes a command on the command channel and sets a status
		/// in this data channel indicating if the command succeeded or failed
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool Execute(string cmd, params object[] args) {
			this.AssociatedCommandStatus = this.CommandChannel.Execute(string.Format(cmd, args));
			return this.AssociatedCommandStatus;
		}

		/// <summary>
		/// Reads a line from the FTP channel socket. Use with discretion,
		/// can cause the code to freeze if you're trying to read data when no data
		/// is being sent.
		/// </summary>
		/// <returns></returns>
		public string ReadLine() {
			if (!this.Connected) {
				this.Connect();
			}

			if (this.StreamReader != null) {
				string buf = this.StreamReader.ReadLine();
#if DEBUG
				Debug.WriteLine(string.Format("> {0}", buf));
#endif
				return buf;
			}

			throw new FtpException("The reader object is null. Are we connected?");
		}

		/// <summary>
		/// Reads bytes off the socket
		/// </summary>
		/// <param name="buf"></param>
		/// <param name="offset"></param>
		/// <param name="size"></param>
		public int Read(byte[] buf, int offset, int size) {
			if (!this.Connected) {
				this.Connect();
			}

			if (this.BaseStream != null) {
				return this.BaseStream.Read(buf, 0, size);
			}

			throw new FtpException("The network stream is null. Are we connected?");
		}

		/// <summary>
		/// Writes the specified byte array to the network stream
		/// </summary>
		public void Write(byte[] buf, int offset, int count) {
			if (!this.Connected) {
				this.Connect();
			}

			if (this.BaseStream != null) {
				this.BaseStream.Write(buf, offset, count);
			}
			else {
				throw new FtpException("The network stream is null. Are we connected?");
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

		public void Disconnect(bool ignorestatus) {
			this.IgnoreStatus = true;
			this.Disconnect();
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

		protected override string SslAuthTargetHost {
			get {
				if (this.CommandChannel != null)
					return this.CommandChannel.Server;
				return this.Server;
			}
		}

		private void ConnectActiveChannel() {
			Socket s = this.Socket.Accept();

			this.Socket.Close();
			this.Socket = null;
			this.Socket = s;

			if (this.CommandChannel.SslEnabled && this.CommandChannel.DataChannelEncryption) {
				this.AuthenticateConnection();
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

		public FtpDataChannel(FtpCommandChannel cmdchan)
			: base() {
			this.CommandChannel = cmdchan;
		}
	}
}
