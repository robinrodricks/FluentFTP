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
		/// improve performance on the command channel with large command transactions.
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

		FtpDataChannelType _dataChanType = FtpDataChannelType.ExtendedPassive;
		/// <summary>
		/// The default data channel type to use (default: ExtendedPassive)
		/// </summary>
		public FtpDataChannelType DataChannelType {
			get { return _dataChanType; }
			set { _dataChanType = value; }
		}

		FtpDataMode _dataMode = FtpDataMode.Stream;
		/// <summary>
		/// Gets / Sets a value indicating if data transfers should be done
		/// in stream or block mode. Stream is the fastest but can leave a lot
		/// of sockets in a linger state when a large number of fast transfers
		/// take place. Default mode is stream.
		/// </summary>
		public FtpDataMode DataChannelMode {
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

				WriteLineToLogStream(string.Format("> {0}", buf));

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
					this.Disconnect();
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
			string traceout = null;

			if(data.ToUpper().StartsWith("PASS")) {
				traceout = "< PASS [omitted for security]";
			}
			else {
				traceout = string.Format("< {0}", data.Trim('\n').Trim('\r'));
			}

			WriteLineToLogStream(traceout);

#if DEBUG
			Debug.WriteLine(traceout);
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

		uint _maxExecute = 20;
		/// <summary>
		/// Gets or sets the maximum number of commands that can be
		/// executed at a time in a pipeline. Once this number is exceeded,
		/// execution stops and the responses are read. The process repeats
		/// itself until all of the pending commands have been executed. Setting
		/// this value to 0 means there is no limit.
		/// </summary>
		public uint MaxPipelineExecute {
			get { return _maxExecute; }
			set { _maxExecute = value; }
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
		/// <returns>An array of FtpCommandResult objects. The order of the objects relates
		/// to the order that commands were executed.</returns>
		public FtpCommandResult[] EndExecute() {
			FtpCommandResult[] results = new FtpCommandResult[this.ExecuteList.Count];
			int reslocation = 0;

			this.LockCommandChannel();

			try {
				MemoryStream cmdstream = new MemoryStream();
				byte[] buf = new byte[this.SendBufferSize];
				int read = 0;

				WriteLineToLogStream("*** BEGIN PIPELINE");

				for(int i = 0; i < this.ExecuteList.Count; i++) {
					if(this.ExecuteList[i] != null) {
						//this.WriteLine(this.ExecuteList[i]);
						string traceout;
						byte[] cmd = Encoding.ASCII.GetBytes(string.Format("{0}\r\n", this.ExecuteList[i]));


						if(this.ExecuteList[i].ToUpper().StartsWith("PASS")) {
							traceout = "< PASS [omitted for security]";
						}
						else {
							traceout = string.Format("< {0}", this.ExecuteList[i].Trim('\n').Trim('\r'));
						}

						WriteLineToLogStream(traceout);

#if DEBUG
						Debug.WriteLine(traceout);
#endif

						cmdstream.Write(cmd, 0, cmd.Length);

						// check the pipeline limits
						if(this.MaxPipelineExecute > 0 && ((i + 1) % this.MaxPipelineExecute) == 0) {
							WriteLineToLogStream("*** PIPELINE LIMIT REACHED AT " + this.MaxPipelineExecute);

							// write the commands in blocks to the socket
							cmdstream.Seek(0, SeekOrigin.Begin);
							while((read = cmdstream.Read(buf, 0, buf.Length)) > 0)
								this.Write(buf, 0, read);
							cmdstream.Dispose();
							cmdstream = new MemoryStream();

							for(; reslocation <= i; reslocation++) {
								this.ReadResponse();
								results[reslocation] = new FtpCommandResult(this);
							}

							WriteLineToLogStream("*** RESUMING PIPELINE EXECUTION AT " + i + "/" + this.ExecuteList.Count);
						}
					}
				}

				// write the commands in blocks to the control socket
				cmdstream.Seek(0, SeekOrigin.Begin);
				while((read = cmdstream.Read(buf, 0, buf.Length)) > 0)
					this.Write(buf, 0, read);
				cmdstream.Dispose();

				// go ahead and read the rest of the responses if there are any
				for(; reslocation < this.ExecuteList.Count; reslocation++) {
					this.ReadResponse();
					results[reslocation] = new FtpCommandResult(this);
				}

				WriteLineToLogStream("*** END PIPELINE");
			}
			finally {
				this.UnlockCommandChannel();
				this.ExecuteList.Clear();
				this.PipelineInProgress = false;
			}

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
		/// Pipeline the given commands on the server
		/// </summary>
		/// <param name="commands">If null value is passed, no attempt to execute is made but an attempt
		/// to performan a response read will be made regardless.</param>
		/// <returns>An array of FtpCommandResults</returns>
		public FtpCommandResult[] Execute(string[] commands) {
			this.BeginExecute();

			foreach(string cmd in commands) {
				this.Execute(cmd);
			}

			return this.EndExecute();
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
		/// Reads and parses the response a command that was executed. Do not call this
		/// unless you just executed a command, will cause code to freeze waiting for the
		/// server to send data that is never comming.
		/// </summary>
		/// <returns></returns>
		public bool ReadResponse() {
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
					// EPSV and EPRT are already assumed to be supported.
					//else if(feat.ToUpper().Contains("EPSV") || feat.ToUpper().Contains("EPRT"))
					//	this.Capabilities |= FtpCapability.EPSV | FtpCapability.EPRT;
				}
			}
			else {
				this.Capabilities = FtpCapability.NONE;
			}
		}

		/// <summary>
		/// Set the data type for the data channel
		/// </summary>
		/// <param name="datatype"></param>
		protected void SetDataType(FtpDataType datatype) {
			switch(datatype) {
				case FtpDataType.Binary:
					this.Execute("TYPE I");
					break;
				case FtpDataType.ASCII:
					this.Execute("TYPE A");
					break;
			}

			if(!this.ResponseStatus) {
				throw new FtpException(this.ResponseMessage);
			}
		}

		/// <summary>
		/// Set the transfer mode for the data channel. If block
        /// mode is requested and it fails, stream mode is
        /// automatically used.
		/// </summary>
		/// <param name="mode"></param>
		protected void SetDataMode(FtpDataMode mode) {
			switch(mode) {
				case FtpDataMode.Block:
                    if (!this.Execute("MODE B")) {
                        this.SetDataMode(FtpDataMode.Stream);
                    }
					break;
				case FtpDataMode.Stream:
                    if (!this.Execute("MODE S")) {
                        throw new FtpException(this.ResponseMessage);
                    }
					break;
			}
		}

		/// <summary>
		/// Opens a passive/binary data channel
		/// </summary>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel() {
			return this.OpenDataChannel(this.DataChannelType, FtpDataType.Binary, this.DataChannelMode);
		}

		/// <summary>
		/// Opens a passive channel of the specified FtpDataType
		/// </summary>
		/// <param name="datatype"></param>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel(FtpDataType datatype) {
			return this.OpenDataChannel(this.DataChannelType, datatype, this.DataChannelMode);
		}

		/// <summary>
		/// Opens the specified data channel type with a binary transfer mode
		/// </summary>
		/// <param name="chantype"></param>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel(FtpDataChannelType chantype) {
			return this.OpenDataChannel(chantype, FtpDataType.Binary, this.DataChannelMode);
		}

		/// <summary>
		/// Opens a data channel setup by the parameters specified
		/// </summary>
		/// <param name="chantype"></param>
		/// <param name="datatype"></param>
		/// <param name="datamode"></param>
		/// <returns></returns>
		protected FtpDataChannel OpenDataChannel(FtpDataChannelType chantype, FtpDataType datatype, FtpDataMode datamode) {
			FtpDataChannel ch = null;

			this.SetDataType(datatype);
			this.SetDataMode(datamode);

			switch(chantype) {
				case FtpDataChannelType.ExtendedPassive:
					ch = this.OpenExtendedPassiveChannel();
					break;
				case FtpDataChannelType.Passive:
					ch = this.OpenPassiveChannel();
					break;
				case FtpDataChannelType.ExtendedActive:
					ch = this.OpenExtendedActiveDataChannel();
					break;
				case FtpDataChannelType.Active:
					ch = this.OpenActiveChannel();
					break;
			}

			if(ch == null) {
				throw new FtpException("Unsupported data mode: " + chantype.ToString());
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
		private FtpDataChannel OpenPassiveChannel() {
			FtpDataChannel chan = new FtpDataChannel(this);
			Match m;

			if(!this.Execute("PASV")) {
				throw new FtpException(this.ResponseMessage);
			}
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

		private FtpDataChannel OpenExtendedPassiveChannel() {
			FtpDataChannel chan = new FtpDataChannel(this);
			Match m;

			if(!this.Execute("EPSV")) {
				// the server doesn't support EPSV
				chan.Dispose();

				if(this.ResponseType == FtpResponseType.PermanentNegativeCompletion) {
					this.Capabilities &= ~(FtpCapability.EPSV | FtpCapability.EPRT);
					return this.OpenPassiveChannel();
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
		private FtpDataChannel OpenActiveChannel() {
			FtpDataChannel dc = new FtpDataChannel(this);
			int port;

			dc.InitalizeActiveChannel();
			port = dc.LocalPort;

			this.Execute("PORT {0},{1},{2}",
				dc.LocalIPAddress.ToString().Replace(".", ","),
				port / 256, port % 256);

			if(!this.ResponseStatus) {
				dc.Dispose();
				throw new FtpException(this.ResponseMessage);
			}

			return dc;
		}

		private FtpDataChannel OpenExtendedActiveDataChannel() {
			FtpDataChannel dc = new FtpDataChannel(this);

			dc.InitalizeActiveChannel();
			this.Execute("EPRT |1|{0}|{1}|", dc.LocalIPAddress.ToString(), dc.LocalPort);

			// |1| is IPv4, need to support IPv6 at some point.
			if(!this.ResponseStatus) {
				dc.Dispose();

				if(this.ResponseType == FtpResponseType.PermanentNegativeCompletion) { // server doesn't support EPRT
					this.Capabilities &= ~(FtpCapability.EPSV | FtpCapability.EPRT);
					return this.OpenActiveChannel();
				}

				throw new FtpException(this.ResponseMessage);
			}

			return dc;
		}

        /// <summary>
        /// Opens a binary data stream
        /// </summary>
        /// <returns></returns>
        public FtpDataStream OpenDataStream() {
            return this.OpenDataStream(FtpDataType.Binary);
        }

        /// <summary>
        /// Opens a binary data stream
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public FtpDataStream OpenDataStream(FtpDataMode mode) {
            return this.OpenDataStream(FtpDataType.Binary, mode);
        }

        /// <summary>
        /// Opens a data stream in the format specified by type
        /// using the DefaultChannelMode property mode.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public FtpDataStream OpenDataStream(FtpDataType type) {
            return this.OpenDataStream(type, this.DataChannelMode);
        }

        /// <summary>
        /// Opens a data stream using the format and mode specified
        /// </summary>
        /// <param name="type"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
		public FtpDataStream OpenDataStream(FtpDataType type, FtpDataMode mode) {
			this.SetDataType(type);
			this.SetDataMode(mode);

			switch(this.DataChannelType) {
				case FtpDataChannelType.Passive:
				case FtpDataChannelType.ExtendedPassive:
					return new FtpPassiveStream(this, mode);
				case FtpDataChannelType.Active:
				case FtpDataChannelType.ExtendedActive:
					return new FtpActiveStream(this, mode);
			}

			throw new FtpException("Unknown data stream type " + this.DataChannelType);
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

		static FtpTraceListener TraceListener = new FtpTraceListener();
		/// <summary>
		/// Gets or sets a stream to log FTP transactions to. Can be
		/// used for logging to a file, the console window, or what have you.
		/// </summary>
		public static Stream FtpLogStream {
			get { return TraceListener.OutputStream; }
			set { TraceListener.OutputStream = value; }
		}

		/// <summary>
		/// Gets or sets a value that indicates if the
		/// output stream should be flushed everytime
		/// a log enter is written to it.
		/// </summary>
		public static bool FtpLogFlushOnWrite {
			get { return TraceListener.FlushOnWrite; }
			set { TraceListener.FlushOnWrite = value; }
		}

		/// <summary>
		/// Writes a message to the FTP log stream
		/// </summary>
		/// <param name="message"></param>
		public static void WriteToLogStream(string message) {
			TraceListener.Write(message);
		}

		/// <summary>
		/// Writes a line to the FTP log stream
		/// </summary>
		/// <param name="message"></param>
		public static void WriteLineToLogStream(string message) {
			TraceListener.WriteLine(message);
		}
	}
}
