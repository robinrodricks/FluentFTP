using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;

namespace System.Net.FtpClient {
	public delegate void ResponseReceived(string message);

	public class FtpCommandChannel : FtpChannel {
		private bool _dataChannelOpen = false;
		/// <summary>
		/// Gets a value indicating if a data channel is active.
		/// Only 1 data channel can be active per connection.
		/// </summary>
		public bool DataChannelOpen {
			get { return _dataChannelOpen; }
			private set { _dataChannelOpen = value; }
		}

		event ResponseReceived _responseReceived = null;
		/// <summary>
		/// Event is fired when a message is received from the server. Useful
		/// for logging the conversation with the server.
		/// </summary>
		public event ResponseReceived ResponseReceived {
			add { this._responseReceived += value; }
			remove { this._responseReceived -= value; }
		}

		FtpCapability _caps = FtpCapability.EMPTY;
		/// <summary>
		/// Capabilities of the server
		/// </summary>
		protected FtpCapability Capabilities {
			get {
				if (_caps == FtpCapability.EMPTY) {
					this.LoadCapabilities();
				}

				return _caps;
			}

			private set {
				_caps = value;
			}
		}

		FtpDataMode _dataMode = FtpDataMode.Passive;
		/// <summary>
		/// The default data mode used for data channels (default: Passive)
		/// </summary>
		public FtpDataMode DefaultDataMode {
			get { return _dataMode; }
			set { _dataMode = value; }
		}

		FtpResponseType _respType = FtpResponseType.None;
		/// <summary>
		/// The type of response received from the last command executed
		/// </summary>
		public FtpResponseType ResponseType {
			get { return _respType; }
			private set { _respType = value; }
		}

		string _respCode = null;
		/// <summary>
		/// The status code of the response
		/// </summary>
		public string ResponseCode {
			get { return _respCode; }
			private set { _respCode = value; }
		}

		string _respMessage = null;
		/// <summary>
		/// The message, if any, that the server sent with the response
		/// </summary>
		public string ResponseMessage {
			get { return _respMessage; }
			private set { _respMessage = value; }
		}

		string[] _messages = null;
		/// <summary>
		/// Other informational messages sent from the server
		/// that are not considered part of the response
		/// </summary>
		public string[] Messages {
			get { return _messages; }
			private set { _messages = value; }
		}

		/// <summary>
		/// General success or failure of the last command executed
		/// </summary>
		public bool ResponseStatus {
			get {
				if (this.ResponseCode != null) {
					int i = int.Parse(this.ResponseCode[0].ToString());

					// 1xx, 2xx, 3xx indicate success
					// 4xx, 5xx are failures
					if (i >= 1 && i <= 3) {
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Fires the response received event.
		/// </summary>
		/// <param name="message"></param>
		protected void OnResponseReceived(string message) {
			if (this._responseReceived != null) {
				this._responseReceived(message);
			}
		}

		/// <summary>
		/// Enables SSL or TLS if they are available. Returns true
		/// if SSL has been enabled, false otherwise.
		/// </summary>
		protected bool EnableSsl() {
			// try TLS first, then SSL.
			if (this.Execute("AUTH TLS") || this.Execute("AUTH SSL")) {
				this.AuthenticateConnection();

				if (!this.Execute("PBSZ 0")) {
					// do nothing? some severs don't even
					// care if you execute PBSZ however rfc 4217
					// says that PBSZ is required if you want
					// data channel security.
					//throw new FtpException(this.ResponseMessage);
#if DEBUG
					System.Diagnostics.Debug.WriteLine("PBSZ ERROR: " + this.ResponseMessage);
#endif
				}

				if (!this.Execute("PROT P")) { // turn on data channel protection.
					throw new FtpException(this.ResponseMessage);
				}
			}

			return this.SslEnabled;
		}

		/// <summary>
		/// Reads a line from the FTP channel socket. Use with discretion,
		/// can cause the code to freeze if you're trying to read data when no data
		/// is being sent.
		/// </summary>
		/// <returns></returns>
		protected virtual string ReadLine() {
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
		protected virtual int Read(byte[] buf, int offset, int size) {
			if (this.BaseStream != null) {
				return this.BaseStream.Read(buf, 0, size);
			}

			throw new FtpException("The network stream is null. Are we connected?");
		}

		/// <summary>
		/// Writes the specified byte array to the network stream
		/// </summary>
		/// <param name="buf"></param>
		protected virtual void Write(byte[] buf) {
			this.Write(buf, 0, buf.Length);
		}

		/// <summary>
		/// Writes the specified byte array to the network stream
		/// </summary>
		protected virtual void Write(byte[] buf, int offset, int count) {
			if (this.BaseStream != null) {
				this.BaseStream.Write(buf, offset, count);
			}
			else {
				throw new FtpException("The network stream is null. Are we connected?");
			}
		}

		/// <summary>
		/// Writes a line to the channel with the correct line endings.
		/// </summary>
		/// <param name="line">Format</param>
		/// <param name="args">Parameters</param>
		protected virtual void WriteLine(string line, params object[] args) {
			this.WriteLine(line, args);
		}

		/// <summary>
		/// Writes a line to the channel with the correct line endings.
		/// </summary>
		/// <param name="line">The line to write</param>
		protected virtual void WriteLine(string line) {
			this.Write(string.Format("{0}\r\n", line));
		}

		/// <summary>
		/// Writes the specified data to the network stream in the proper encoding
		/// </summary>
		protected virtual void Write(string format, params object[] args) {
			this.Write(string.Format(format, args));
		}

		/// <summary>
		/// Writes the specified data to the network stream in the proper encoding
		/// </summary>
		/// <param name="data"></param>
		protected virtual void Write(string data) {
#if DEBUG
			Debug.WriteLine(string.Format("< {0}", data.Trim('\n').Trim('\r')));
#endif
			this.Write(Encoding.ASCII.GetBytes(data));
		}

		/// <summary>
		/// Executes a command on the server
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool Execute(string cmd, params object[] args) {
			return this.Execute(string.Format(cmd, args));
		}

		/// <summary>
		/// Executes a command on the server
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool Execute(string cmd) {
			if (!this.Connected) {
				this.Connect();
			}

			if (this.Socket.Poll(50000, SelectMode.SelectRead) && this.Socket.Available == 0) {
				// we've been disconnected, probably due to inactivity
				this.Connect();
			}

			this.WriteLine(cmd);

			return this.ReadResponse();
		}

		/// <summary>
		/// Reads and parses the response a command that was executed. Do not call this
		/// unless you just executed a command, will cause code to freeze waiting for the
		/// server to send data that is never comming.
		/// </summary>
		/// <returns></returns>
		protected bool ReadResponse() {
			string buf;
			List<string> messages = new List<string>();

			this.ResponseType = FtpResponseType.None;
			this.ResponseCode = null;
			this.ResponseMessage = null;
			this.Messages = null;

			while ((buf = this.ReadLine()) != null) {
				Match m = Regex.Match(buf, @"^(\d{3})\s(.*)$");

				this.OnResponseReceived(buf);

				if (m.Success) { // the server sent the final response message
					if (m.Groups.Count > 1) {
						this.ResponseCode = m.Groups[1].Value;
					}

					if (m.Groups.Count > 2) {
						this.ResponseMessage = m.Groups[2].Value;
					}

					if (messages.Count > 0) {
						this.Messages = messages.ToArray();
					}

					// check response
					if (this.ResponseCode != null) {
						this.ResponseType = (FtpResponseType)int.Parse(this.ResponseCode[0].ToString());
						return this.ResponseStatus;
					}

					throw new FtpException("Could not determine the response status");
				}

				messages.Add(buf);
			}

			throw new FtpException("An unknown error occurred while executing the command");
		}

		/// <summary>
		/// Checks if the server supports the specified capability
		/// </summary>
		/// <param name="cap"></param>
		public bool HasCapability(FtpCapability cap) {
			return (this.Capabilities & cap) == cap;
		}

		/// <summary>
		/// Loads the capabilities of this server
		/// </summary>
		private void LoadCapabilities() {
			if (this.Execute("FEAT")) {
				// some servers support EPSV but do not advertise it
				// in the FEAT list. for this reason, we assume EPSV
				// is supported and if we get a 500 reply then we fall back
				// to PASV.
				this.Capabilities = FtpCapability.EPSV | FtpCapability.EPRT;

				foreach (string feat in this.Messages) {
					if (feat.ToUpper().Contains("MLST") || feat.ToUpper().Contains("MLSD"))
						this.Capabilities |= FtpCapability.MLSD | FtpCapability.MLST;
					else if (feat.ToUpper().Contains("MDTM"))
						this.Capabilities |= FtpCapability.MDTM;
					else if (feat.ToUpper().Contains("REST STREAM"))
						this.Capabilities |= FtpCapability.REST;
					else if (feat.ToUpper().Contains("SIZE"))
						this.Capabilities |= FtpCapability.SIZE;
					else if (feat.ToUpper().Contains("EPSV") || feat.ToUpper().Contains("EPRT"))
						this.Capabilities |= FtpCapability.EPSV | FtpCapability.EPRT;
				}
			}
			else {
				this.Capabilities = FtpCapability.NONE;
			}
		}

		/// <summary>
		/// Opens a passive/binary data channel
		/// </summary>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel() {
			return this.OpenDataChannel(this.DefaultDataMode, FtpTransferMode.Binary);
		}

		/// <summary>
		/// Opens a passive channel of the specified FtpTransferMode
		/// </summary>
		/// <param name="xfer"></param>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel(FtpTransferMode xfer) {
			return this.OpenDataChannel(this.DefaultDataMode, xfer);
		}

		/// <summary>
		/// Opens the specified data channel type with a binary transfer mode
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel(FtpDataMode mode) {
			return this.OpenDataChannel(mode, FtpTransferMode.Binary);
		}

		/// <summary>
		/// Opens a data channel setup by the parameters specified
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="xfer"></param>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel(FtpDataMode mode, FtpTransferMode xfer) {
			FtpDataChannel ch = null;

			if (this.DataChannelOpen) {
				throw new FtpException("Only 1 data channel can be opened per connection. " +
					"Create more connections if you want to perform operations in parallel.");
			}

			switch (xfer) {
				case FtpTransferMode.Binary:
					this.Execute("TYPE I");
					break;
				case FtpTransferMode.ASCII:
					this.Execute("TYPE A");
					break;
			}

			if (!this.ResponseStatus) {
				throw new FtpException(this.ResponseMessage);
			}

			switch (mode) {
				case FtpDataMode.Passive:
					if (this.HasCapability(FtpCapability.EPSV)) {
						ch = this.OpenExtendedPassiveChannel();
					}
					else {
						ch = this.OpenPassiveChannel();
					}
					break;
				case FtpDataMode.Active:
					if (this.HasCapability(FtpCapability.EPRT)) {
						ch = this.OpenExtendedActiveDataChannel();
					}
					else {
						ch = this.OpenActiveChannel();
					}
					break;
			}

			if (ch == null) {
				throw new FtpException("Unsupported data mode: " + mode.ToString());
			}

			this.DataChannelOpen = true;
			ch.ConnectionClosed += new FtpChannelDisconnected(OnDataChannelDisconnected);
			ch.Diposed += new FtpChannelDisposed(OnDataChannelDisposed);
			ch.InvalidCertificate += new FtpInvalidCertificate(OnInvalidDataChannelCertificate);

			return ch;
		}

		void OnInvalidDataChannelCertificate(FtpChannel c, InvalidCertificateInfo e) {
			// redirect invalid data channel certificate errors to 
			// event handlers for the command channel
			this.OnInvalidSslCerticate(c, e);
		}

		/// <summary>
		/// Set value indicating the data channel has been disposed
		/// so that more can be opened.
		/// </summary>
		void OnDataChannelDisposed() {
			this.DataChannelOpen = false;
		}

		/// <summary>
		/// Reads the response from the server after the data channel
		/// has been disconnected
		/// </summary>
		void OnDataChannelDisconnected() {
			if (this.ResponseStatus) {
				// when the data channel is disconnected after 
				// a successful command the server will send a
				// response to us.
				if (!this.ReadResponse()) {
					throw new FtpException(this.ResponseMessage);
				}
			}
		}

		/// <summary>
		/// Opens a PASV data channel
		/// </summary>
		/// <returns></returns>
		private FtpDataChannel OpenPassiveChannel() {
			FtpDataChannel chan = new FtpDataChannel(this);
			Match m;

			if (!this.Execute("PASV")) {
				throw new FtpException(this.ResponseMessage);
			}

			// parse pasv response
			m = Regex.Match(this.ResponseMessage, "([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");
			if (!m.Success || m.Groups.Count != 7) {
				throw new FtpException(string.Format("Malformed PASV response: {0}", this.ResponseMessage));
			}

			chan.Server = string.Format("{0}.{1}.{2}.{3}", m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);
			chan.Port = (int.Parse(m.Groups[5].Value) << 8) + int.Parse(m.Groups[6].Value);
			chan.Connect();

			return chan;
		}

		private FtpDataChannel OpenExtendedPassiveChannel() {
			FtpDataChannel chan = new FtpDataChannel(this);
			Match m;

			if (!this.Execute("EPSV")) {
				// the server doesn't support EPSV
				chan.Dispose();

				if (this.ResponseType == FtpResponseType.PermanentNegativeCompletion) {
					this.Capabilities &= ~(FtpCapability.EPSV | FtpCapability.EPRT);
					return this.OpenPassiveChannel();
				}

				throw new FtpException(this.ResponseMessage);
			}

			// according to RFC 2428, EPSV response must be exactly the
			// the same as EPRT response except the first two fields MUST BE blank
			// so that leaves us with (|||port_here|)
			m = Regex.Match(this.ResponseMessage, @"\(\|\|\|(\d+)\|\)");
			if (!m.Success) {
				throw new FtpException("Failed to get the EPSV port from: " + this.ResponseMessage);
			}

			chan.Server = this.Server;
			chan.Port = int.Parse(m.Groups[1].Value);
			chan.Connect();

			return chan;
		}

		/// <summary>
		/// Opens a PORT data channel
		/// </summary>
		/// <returns></returns>
		private FtpDataChannel OpenActiveChannel() {
			FtpDataChannel dc = new FtpDataChannel(this);
			int port;

			dc.InitalizeActiveChannel();
			port = dc.LocalPort;

			if (!this.Execute("PORT {0},{1},{2}",
				dc.LocalIPAddress.ToString().Replace(".", ","),
				port / 256, port % 256)) {
				dc.Dispose();
				throw new FtpException(this.ResponseMessage);
			}

			return dc;
		}

		private FtpDataChannel OpenExtendedActiveDataChannel() {
			FtpDataChannel dc = new FtpDataChannel(this);
			int port;

			dc.InitalizeActiveChannel();
			port = dc.LocalPort;

			// |1| is IPv4, need to support IPv6 at some point.
			if (!this.Execute("EPRT |1|{0}|{1}|",
				dc.LocalIPAddress.ToString(), dc.LocalPort)) {
				dc.Dispose();

				if (this.ResponseType == FtpResponseType.PermanentNegativeCompletion) { // server doesn't support EPRT
					this.Capabilities &= ~(FtpCapability.EPSV | FtpCapability.EPRT);
					return this.OpenActiveChannel();
				}

				throw new FtpException(this.ResponseMessage);
			}

			return dc;
		}

		/// <summary>
		/// Terminates ftp session and cleans up the resources
		/// being used.
		/// </summary>
		public override void Disconnect() {
			if (this.Connected) {
				bool disconnected = (this.Socket.Poll(50000, SelectMode.SelectRead) && this.Socket.Available == 0);

				if (!disconnected && !this.Execute("QUIT")) {
					// we don't want to do this, the user is 
					// trying to terminate the connection.
					//throw new FtpException(this.ResponseMessage);
				}
			}

			base.Disconnect();
		}

		/// <summary>
		/// Upon the initial connection, we will be presented with a banner and status
		/// </summary>
		void OnChannelConnected() {
			if (!this.ReadResponse()) {
				this.Disconnect();
				throw new FtpException(this.ResponseMessage);
			}

			this.Capabilities = FtpCapability.EMPTY;
		}

		public FtpCommandChannel() {
			this.ConnectionReady += new FtpChannelConnected(OnChannelConnected);
		}
	}
}
