using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace System.Net.FtpClient {
	/// <summary>
	/// ResponseReceived delegate
	/// </summary>
	/// <param name="status">Status number</param>
	/// <param name="response">Status message</param>
	public delegate void ResponseReceived(string status, string response);

	/// <summary>
	/// The communication channel for the FTP server / used for issuing commands
	/// and controlling transactions.
	/// </summary>
	public class FtpCommandChannel : FtpChannel {
		/// <summary>
		/// Mutex used for locking the command channel while
		/// executing commands
		/// </summary>
		Mutex mCommandLock = new Mutex();

		FtpSslMode _sslMode = FtpSslMode.Explicit;
		/// <summary>
		/// Sets the type of SSL to use when the EnableSSL property is
		/// true. The default is Explicit, meaning SSL is negotiated
		/// after the initial connection, before credentials are sent.
		/// </summary>
		public FtpSslMode SslMode {
			get { return _sslMode; }
			set { _sslMode = value; }
		}

		bool _dataChanEncrypt = true;
		/// <summary>
		/// Enable or disable data channel encryption. This option is only
		/// applicable when the SslMode property is set to use encryption.
		/// The default value is true.
		/// </summary>
		public bool DataChannelEncryption {
			get { return _dataChanEncrypt; }
			set { _dataChanEncrypt = value; }
		}

		bool _enablePipelining = false;
		/// <summary>
		/// Gets / sets a value indicating if we can use pipelining techniques
		/// to talk to the server. If the server allows it, this will help
		/// improve performance on the command channel, considerably.
		/// </summary>
		public bool EnablePipelining {
			get { return _enablePipelining; }
			set { _enablePipelining = value; }
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
				if(_caps == FtpCapability.EMPTY) {
					this.LoadCapabilities();
				}

				return _caps;
			}

			private set {
				_caps = value;
			}
		}

		FtpDataMode _dataMode = FtpDataMode.ExtendedPassive;
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
				if(this.ResponseCode != null) {
					int i = int.Parse(this.ResponseCode[0].ToString());

					// 1xx, 2xx, 3xx indicate success
					// 4xx, 5xx are failures
					if(i >= 1 && i <= 3) {
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Acquire an exclusive lock on the command channel
		/// while executing/processing commands
		/// </summary>
		public void LockCommandChannel() {
			this.mCommandLock.WaitOne();
		}

		/// <summary>
		/// Acquire an exclusive lock on the command channel
		/// while executing/processing commands 
		/// </summary>
		/// <param name="timeout"></param>
		public void LockCommandChannel(int timeout) {
			this.mCommandLock.WaitOne(timeout);
		}

		/// <summary>
		/// Release the exclusive lock held on the command channel
		/// </summary>
		public void UnlockCommandChannel() {
			this.mCommandLock.ReleaseMutex();
		}

		/// <summary>
		/// Fires the response received event.
		/// </summary>
		/// <param name="status">Status code</param>
		/// <param name="response">Status message</param>
		protected void OnResponseReceived(string status, string response) {
			if(this._responseReceived != null) {
				this._responseReceived(status, response);
			}
		}

		/// <summary>
		/// Reads a line from the FTP channel socket. Use with discretion,
		/// can cause the code to freeze if you're trying to read data when no data
		/// is being sent.
		/// </summary>
		/// <returns></returns>
		protected virtual string ReadLine() {
			if(this.StreamReader != null) {
				string buf = this.StreamReader.ReadLine();
#if DEBUG
				Debug.WriteLine(string.Format("> {0}", buf));
#endif
				this.LastSocketActivity = DateTime.Now;

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
			if(this.BaseStream != null) {
				this.LastSocketActivity = DateTime.Now;
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
			if(this.BaseStream != null) {
				if(this.NeedsSocketPoll && !this.PollConnection()) {
					// we've been disconnected, try to reconnect
					this.Connect();
				}

				this.BaseStream.Write(buf, offset, count);
				this.LastSocketActivity = DateTime.Now;
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

		DateTime _lastSockActivity = DateTime.MinValue;
		/// <summary>
		/// Gets a the last time data was read or written to the socket.
		/// </summary>
		protected DateTime LastSocketActivity {
			get {
				if(_lastSockActivity == DateTime.MinValue) {
					// we just connected so set the
					// value to now
					_lastSockActivity = DateTime.Now;
				}

				return _lastSockActivity; 
			}
			private set { _lastSockActivity = value; }
		}

		/// <summary>
		/// Returns true if the last socket poll was 30 seconds ago. The last poll
		/// time gets updated everytime data is read or written to the socket.
		/// </summary>
		protected bool NeedsSocketPoll {
			get {
				DateTime lastPoll = this.LastSocketActivity;

				if(Math.Round(DateTime.Now.Subtract(lastPoll).TotalSeconds) > 30) {
					return true;
				}

				return false;
			}
		}

		/// <summary>
		/// Attempts to check our connectivity to the server
		/// with using Socket.Poll
		/// </summary>
		/// <returns>True if connected, false otherwise</returns>
		protected bool PollConnection() {
			if(this.Socket.Poll(500000, SelectMode.SelectRead) && this.Socket.Available == 0) {
				// we've been disconnected, probably due to inactivity
				return false;
			}

			return true;
		}

		private List<string> _execList = new List<string>();
		/// <summary>
		/// Gets a list of commands in the current pipeline
		/// </summary>
		protected List<string> ExecuteList {
			get { return _execList; }
			set { _execList = value; }
		}

		bool _pipelineInProgress = false;
		/// <summary>
		/// Gets a value indicating if a pipeline has been started
		/// </summary>
		public bool PipelineInProgress {
			get { return _pipelineInProgress; }
			private set { _pipelineInProgress = value; }
		}

		/// <summary>
		/// Starts a new pipeline of commands
		/// </summary>
		public void BeginExecute() {
			this.ExecuteList.Clear();
			this.PipelineInProgress = true;
		}

		/// <summary>
		/// Executes all of the commands in the pipeline list
		/// </summary>
		/// <returns></returns>
		public FtpCommandResult[] EndExecute() {
			FtpCommandResult[] results = new FtpCommandResult[this.ExecuteList.Count];

			for(int i = 0; i < this.ExecuteList.Count; i++) {
				if(this.ExecuteList[i] != null) {
					this.WriteLine(this.ExecuteList[i]);
				}
			}

			for(int i = 0; i < this.ExecuteList.Count; i++) {
				this.ReadResponse();
				results[i] = new FtpCommandResult(this);
			}

			this.ExecuteList.Clear();
			this.PipelineInProgress = false;

			return results;
		}

		/// <summary>
		/// Cancels the current pipeline
		/// </summary>
		public void CancelPipeline() {
			this.ExecuteList.Clear();
			this.PipelineInProgress = false;
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
		/// Executes a command on the server. If there is a pipeline in progress
		/// the command is queued and true is returned.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public bool Execute(string cmd) {
			if(!this.Connected) {
				this.Connect();
			}

			//if(this.Socket.Poll(500000, SelectMode.SelectRead) && this.Socket.Available == 0) {
				// we've been disconnected, probably due to inactivity
			//	this.Connect();
			//}

			if(this.PipelineInProgress) {
				this.ExecuteList.Add(cmd);
				return true;
			}
			else {
				this.WriteLine(cmd);
				return this.ReadResponse();
			}
		}

		/// <summary>
		/// Pipeline the given commands on the server / regardless of the current
		/// pipeline status.
		/// </summary>
		/// <param name="commands">If null value is passed, no attempt to execute is made but an attempt
		/// to performance a response read will be made regardless.</param>
		/// <returns>An array of FtpCommandResults</returns>
		public FtpCommandResult[] Execute(string[] commands) {
			FtpCommandResult[] results = new FtpCommandResult[commands.Length];

			for(int i = 0; i < commands.Length; i++) {
				if(commands[i] != null) {
					this.WriteLine(commands[i]);
				}
			}

			for(int i = 0; i < commands.Length; i++) {
				this.ReadResponse();
				results[i] = new FtpCommandResult(this);
			}

			return results;
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

			while((buf = this.ReadLine()) != null) {
				Match m = Regex.Match(buf, @"^(\d{3})\s(.*)$");

				if(m.Success) { // the server sent the final response message
					if(m.Groups.Count > 1) {
						this.ResponseCode = m.Groups[1].Value;
					}

					if(m.Groups.Count > 2) {
						this.ResponseMessage = m.Groups[2].Value;
					}

					if(messages.Count > 0) {
						this.Messages = messages.ToArray();
					}

					// check response
					if(this.ResponseCode != null) {
						this.ResponseType = (FtpResponseType)int.Parse(this.ResponseCode[0].ToString());
						this.OnResponseReceived(this.ResponseCode, this.ResponseMessage);
						return this.ResponseStatus;
					}

					throw new FtpException("Could not determine the response status");
				}
				else {
					this.OnResponseReceived("INFO", buf);
				}

				messages.Add(buf);
			}

			throw new FtpException("An unknown error occurred while executing the command");
		}

		/// <summary>
		/// Open a connection
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public virtual void Connect(string host, int port) {
			if(!this.Connected) {
				this.Server = host;
				this.Port = port;
				this.Connect();
			}
		}

		/// <summary>
		/// Open a connection
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		public virtual void Connect(IPAddress ip, int port) {
			if(!this.Connected) {
				this.Server = ip.ToString();
				this.Port = port;
				this.Connect();
			}
		}

		/// <summary>
		/// Open a connection
		/// </summary>
		/// <param name="ipep"></param>
		public virtual void Connect(IPEndPoint ipep) {
			if(!this.Connected) {
				this.Server = ipep.Address.ToString();
				this.Port = ipep.Port;
				this.Connect();
			}
		}

		/// <summary>
		/// Checks if the server supports the specified capability
		/// </summary>
		/// <param name="cap"></param>
		public bool HasCapability(FtpCapability cap) {
			return (this.Capabilities & cap) == cap;
		}

		/// <summary>
		/// Removes the specified capability from the list
		/// </summary>
		/// <param name="cap"></param>
		public void RemoveCapability(FtpCapability cap) {
			this.Capabilities &= ~(cap);
		}

		/// <summary>
		/// Loads the capabilities of this server
		/// </summary>
		private void LoadCapabilities() {
			if(this.Execute("FEAT")) {
				// some servers support EPSV but do not advertise it
				// in the FEAT list. for this reason, we assume EPSV
				// is supported and if we get a 500 reply then we fall back
				// to PASV.
				this.Capabilities = FtpCapability.EPSV | FtpCapability.EPRT;

				foreach(string feat in this.Messages) {
					if(feat.ToUpper().Contains("MLST") || feat.ToUpper().Contains("MLSD"))
						this.Capabilities |= FtpCapability.MLSD | FtpCapability.MLST;
					else if(feat.ToUpper().Contains("MDTM"))
						this.Capabilities |= (FtpCapability.MDTM | FtpCapability.MDTMDIR);
					else if(feat.ToUpper().Contains("REST STREAM"))
						this.Capabilities |= FtpCapability.REST;
					else if(feat.ToUpper().Contains("SIZE"))
						this.Capabilities |= FtpCapability.SIZE;
					else if(feat.ToUpper().Contains("EPSV") || feat.ToUpper().Contains("EPRT"))
						this.Capabilities |= FtpCapability.EPSV | FtpCapability.EPRT;
				}
			}
			else {
				this.Capabilities = FtpCapability.NONE;
			}
		}

		/// <summary>
		/// Set the transfer mode for the data channel
		/// </summary>
		/// <param name="xfer"></param>
		protected void SetTransferMode(FtpTransferMode xfer) {
			switch(xfer) {
				case FtpTransferMode.Binary:
					this.Execute("TYPE I");
					break;
				case FtpTransferMode.ASCII:
					this.Execute("TYPE A");
					break;
			}

			if(!this.ResponseStatus) {
				throw new FtpException(this.ResponseMessage);
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

			switch(mode) {
				case FtpDataMode.ExtendedPassive:
					ch = this.OpenExtendedPassiveChannel(xfer);
					break;
				case FtpDataMode.Passive:
					ch = this.OpenPassiveChannel(xfer);
					break;
				case FtpDataMode.ExtendedActive:
					ch = this.OpenExtendedActiveDataChannel(xfer);
					break;
				case FtpDataMode.Active:
					ch = this.OpenActiveChannel(xfer);
					break;
			}

			if(ch == null) {
				throw new FtpException("Unsupported data mode: " + mode.ToString());
			}

			// when the data channel is closed, we need to see if the associated
			// command status was successful or not. if it was, we need to be
			// expecting a response from the server.
			ch.ConnectionClosed += new FtpChannelDisconnected(OnDataChannelDisconnected);
			// If the data channel is using SSL and it fails verification, call this
			// objects invalid certificate handler
			ch.InvalidCertificate += new FtpInvalidCertificate(OnInvalidDataChannelCertificate);

			return ch;
		}

		void OnInvalidDataChannelCertificate(FtpChannel c, InvalidCertificateInfo e) {
			// redirect invalid data channel certificate errors to 
			// event handlers for the command channel
			this.OnInvalidSslCerticate(c, e);
		}

		/// <summary>
		/// Reads the response from the server after the data channel
		/// has been disconnected
		/// </summary>
		void OnDataChannelDisconnected(FtpChannel ch) {
			FtpDataChannel chan = (FtpDataChannel)ch;

			// if the associated command succeeded the
			// server will send a response when this data channel closes
			if(chan.AssociatedCommandStatus && !this.ReadResponse()) {
				// don't throw an exception if ignorestatus is true
				// this option has to be set manually
				if(!chan.IgnoreStatus) {
					throw new FtpException(this.ResponseMessage);
				}
			}
		}

		/// <summary>
		/// Opens a PASV data channel
		/// </summary>
		/// <returns></returns>
		private FtpDataChannel OpenPassiveChannel(FtpTransferMode xfer) {
			FtpDataChannel chan = new FtpDataChannel(this);
			Match m;

			if(this.EnablePipelining) {
				FtpCommandResult[] res = this.Execute(
					new string[]
					{
						string.Format("TYPE {0}", xfer == FtpTransferMode.ASCII ? "A" : "I"),
						"PASV"
					});

				// check result
				foreach(FtpCommandResult r in res) {
					if(!r.ResponseStatus) {
						throw new FtpException(r.ResponseMessage);
					}
				}
			}
			else {
				this.SetTransferMode(xfer);

				if(!this.Execute("PASV")) {
					throw new FtpException(this.ResponseMessage);
				}
			}

			// the last command executed should be the PASV command
			// so the command result should already be filled in here
			// no matter if pipelining is enabled or not.

			// parse pasv response
			m = Regex.Match(this.ResponseMessage, "([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");
			if(!m.Success || m.Groups.Count != 7) {
				throw new FtpException(string.Format("Malformed PASV response: {0}", this.ResponseMessage));
			}

			chan.Server = string.Format("{0}.{1}.{2}.{3}", m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);
			chan.Port = (int.Parse(m.Groups[5].Value) << 8) + int.Parse(m.Groups[6].Value);
			chan.Connect();

			return chan;
		}

		private FtpDataChannel OpenExtendedPassiveChannel(FtpTransferMode xfer) {
			FtpDataChannel chan = new FtpDataChannel(this);
			Match m;

			if(this.EnablePipelining) {
				FtpCommandResult[] res = this.Execute(
					new string[]
					{
						string.Format("TYPE {0}", xfer == FtpTransferMode.ASCII ? "A" : "I"),
						"EPSV"
					});

				// check result
				foreach(FtpCommandResult r in res) {
					if(!r.ResponseStatus) {
						throw new FtpException(r.ResponseMessage);
					}
				}
			}
			else {
				this.SetTransferMode(xfer);
				this.Execute("EPSV");

			}

			if(!this.ResponseStatus) {
				// the server doesn't support EPSV
				chan.Dispose();

				if(this.ResponseType == FtpResponseType.PermanentNegativeCompletion) {
					this.Capabilities &= ~(FtpCapability.EPSV | FtpCapability.EPRT);
					return this.OpenPassiveChannel(xfer);
				}

				throw new FtpException(this.ResponseMessage);
			}

			// according to RFC 2428, EPSV response must be exactly the
			// the same as EPRT response except the first two fields MUST BE blank
			// so that leaves us with (|||port_here|)
			m = Regex.Match(this.ResponseMessage, @"\(\|\|\|(\d+)\|\)");
			if(!m.Success) {
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
		private FtpDataChannel OpenActiveChannel(FtpTransferMode xfer) {
			FtpDataChannel dc = new FtpDataChannel(this);
			int port;

			dc.InitalizeActiveChannel();
			port = dc.LocalPort;

			if(this.EnablePipelining) {
				FtpCommandResult[] res = this.Execute(
					new string[]
					{
						string.Format("TYPE {0}", xfer == FtpTransferMode.ASCII ? "A" : "I"),
						string.Format("PORT {0},{1},{2}",
							dc.LocalIPAddress.ToString().Replace(".", ","),
							port / 256, port % 256)
					});

				// check results
				foreach(FtpCommandResult r in res) {
					if(!r.ResponseStatus) {
						throw new FtpException(r.ResponseMessage);
					}
				}
			}
			else {
				this.SetTransferMode(xfer);
				this.Execute("PORT {0},{1},{2}",
					dc.LocalIPAddress.ToString().Replace(".", ","),
					port / 256, port % 256);
			}

			if(!this.ResponseStatus) {
				dc.Dispose();
				throw new FtpException(this.ResponseMessage);
			}

			return dc;
		}

		private FtpDataChannel OpenExtendedActiveDataChannel(FtpTransferMode xfer) {
			FtpDataChannel dc = new FtpDataChannel(this);

			dc.InitalizeActiveChannel();

			if(this.EnablePipelining) {
				FtpCommandResult[] res = this.Execute(
					new string[]
					{
						string.Format("TYPE {0}", xfer == FtpTransferMode.ASCII ? "A" : "I"),
						string.Format("EPRT |1|{0}|{1}|", dc.LocalIPAddress.ToString(), dc.LocalPort)
					});

				// check results
				foreach(FtpCommandResult r in res) {
					if(!r.ResponseStatus) {
						throw new FtpException(r.ResponseMessage);
					}
				}
			}
			else {
				this.SetTransferMode(xfer);
				this.Execute("EPRT |1|{0}|{1}|", dc.LocalIPAddress.ToString(), dc.LocalPort);
			}

			// |1| is IPv4, need to support IPv6 at some point.
			if(!this.ResponseStatus) {
				dc.Dispose();

				if(this.ResponseType == FtpResponseType.PermanentNegativeCompletion) { // server doesn't support EPRT
					this.Capabilities &= ~(FtpCapability.EPSV | FtpCapability.EPRT);
					return this.OpenActiveChannel(xfer);
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
			if(this.Connected) {
				bool disconnected = (this.Socket.Poll(50000, SelectMode.SelectRead) && this.Socket.Available == 0);

				if(!disconnected && !this.Execute("QUIT")) {
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
		void OnChannelConnected(FtpChannel c) {
			if(this.SslMode == FtpSslMode.Implicit) {
				// The connection should already be encrypted
				// so authenticate the connection and then
				// try to read the initial greeting.
				this.AuthenticateConnection();
			}


			if(!this.ReadResponse()) {
				this.Disconnect();
				throw new FtpException(this.ResponseMessage);
			}

			if(this.SslMode == FtpSslMode.Explicit) {
				if(this.Execute("AUTH TLS") || this.Execute("AUTH SSL")) {
					this.AuthenticateConnection();
				}
			}

			if(this.SslEnabled && this.DataChannelEncryption) {
				if(!this.Execute("PBSZ 0")) {
					// do nothing? some severs don't even
					// care if you execute PBSZ however rfc 4217
					// says that PBSZ is required if you want
					// data channel security.
					//throw new FtpException(this.ResponseMessage);
#if DEBUG
					System.Diagnostics.Debug.WriteLine("PBSZ ERROR: " + this.ResponseMessage);
#endif
				}

				if(!this.Execute("PROT P")) { // turn on data channel protection.
					throw new FtpException(this.ResponseMessage);
				}
			}

			this.Capabilities = FtpCapability.EMPTY;
		}

		/// <summary>
		/// Initalize a new command channel object.
		/// </summary>
		public FtpCommandChannel() {
			this.ConnectionReady += new FtpChannelConnected(OnChannelConnected);
		}
	}
}
