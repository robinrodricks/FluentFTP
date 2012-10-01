using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Net.FtpClient.Proxy;

namespace System.Net.FtpClient {
	/// <summary>
	/// Delegate for event
	/// </summary>
	/// <param name="c"></param>
	public delegate void FtpChannelConnected(FtpChannel c);
	/// <summary>
	/// Delegate for event
	/// </summary>
	/// <param name="c"></param>
	public delegate void FtpChannelDisconnected(FtpChannel c);
	/// <summary>
	/// Delegate for event
	/// </summary>
	/// <param name="c"></param>
	public delegate void FtpChannelDisposed(FtpChannel c);
	/// <summary>
	/// Delegate for event
	/// </summary>
	/// <param name="c"></param>
	/// <param name="e"></param>
	public delegate void FtpInvalidCertificate(FtpChannel c, InvalidCertificateInfo e);
    /// <summary>
    /// Delegate for asynchronous connections
    /// </summary>
    /// <returns></returns>
    public delegate void FtpAsyncInvoker();
	
	/// <summary>
	/// Base class for Ftp*Channel implementations
	/// </summary>
	public abstract class FtpChannel : IDisposable {
		event FtpChannelConnected _onConnected = null;
		/// <summary>
		/// Event is fired after a connection has been made
		/// </summary>
		public event FtpChannelConnected ConnectionReady {
			add { _onConnected += value; }
			remove { _onConnected -= value; }
		}

		event FtpChannelDisconnected _onDisconnected = null;
		/// <summary>
		/// Event is fired when Disconnect is called
		/// </summary>
		public event FtpChannelDisconnected ConnectionClosed {
			add { _onDisconnected += value; }
			remove { _onDisconnected -= value; }
		}

		event FtpChannelDisposed _onDisposed = null;
		/// <summary>
		/// Event is fired when this object is disposed.
		/// </summary>
		public event FtpChannelDisposed Disposed {
			add { _onDisposed += value; }
			remove { _onDisposed -= value; }
		}

		event FtpInvalidCertificate _onBadCert = null;
		/// <summary>
		/// Event is fired when an invalid SSL certificate is encountered.
		/// </summary>
		public event FtpInvalidCertificate InvalidCertificate {
			add { _onBadCert += value; }
			remove { _onBadCert -= value; }
		}

		/// <summary>
		/// Fire ConnectionReady event
		/// </summary>
		protected void OnConnectionReady() {
			if (_onConnected != null) {
				this._onConnected(this);
			}
		}

		/// <summary>
		/// Fire ConnectionClosed event
		/// </summary>
		protected void OnConnectionClosed() {
			if (_onDisconnected != null) {
				this._onDisconnected(this);
			}
		}

		/// <summary>
		/// Fire Disposed event
		/// </summary>
		protected void OnDisposed() {
			if (_onDisposed != null) {
				this._onDisposed(this);
			}
		}

		/// <summary>
		/// Fire the invalid SSL certificate event
		/// </summary>
		public void OnInvalidSslCerticate(FtpChannel c, InvalidCertificateInfo e) {
			if (this._onBadCert != null) {
				this._onBadCert(c, e);
			}
		}

		private bool _isServerSocket = false;
		/// <summary>
		/// Indicates if this is an incoming (active) or outgoing channel (passive)
		/// </summary>
		protected bool IsServerSocket {
			get { return _isServerSocket; }
			set { _isServerSocket = value; }
		}

		/// <summary>
		/// Gets a value indicating if encryption is in use
		/// </summary>
		public bool SslEnabled {
			get {
				if (this.Connected) {
					return this.SecurteStream.IsEncrypted;
				}

				return false;
			}
		}

		SslPolicyErrors _policyErrors = SslPolicyErrors.None;
		/// <summary>
		/// Gets the SSL errors if there were any
		/// </summary>
		public SslPolicyErrors SslPolicyErrors {
			get { return _policyErrors; }
			private set { _policyErrors = value; }
		}

		X509Certificate _sslCertificate = null;
		/// <summary>
		/// Gets the certificate associated with the current connection
		/// </summary>
		public X509Certificate SslCertificate {
			get { return _sslCertificate; }
			private set { _sslCertificate = value; }
		}

        private IPAddress _ipAddress = null;
        /// <summary>
        /// The IPAddress used to connect to the channel.
        /// </summary>
        public IPAddress IpAddress {
            get {
                return _ipAddress;
            }
        }

        private bool _autoTryOtherAddresses = true;
        /// <summary>
        /// Indicates if we should try to connect to other available
        /// addresses, when it cannot connect to a specific one. Default value is true.
        /// </summary>
        public bool AutoTryOtherAddresses {
            get { return _autoTryOtherAddresses; }
            set { _autoTryOtherAddresses = value; }
        }

        private bool _usesIPv6 = true;
        /// <summary>
        /// Indicates if we can uses IPv6 addresses to connect to the socket when
        /// the server address is not an IP. Default value is true.
        /// </summary>
        public bool UsesIPv6 {
            get { return _usesIPv6; }
            set { _usesIPv6 = value; }
        }

        /// <summary>
        /// The proxy server type used for the connection.
        /// </summary>
        public ProxyType ProxyType { get; set; }

        /// <summary>
        /// The proxy server address.
        /// </summary>
        public string ProxyHost { get; set; }

        /// <summary>
        /// The proxy server port.
        /// </summary>
        public int ProxyPort { get; set; }

        /// <summary>
        /// The proxy server username used to connect.
        /// </summary>
        public string ProxyUsername { get; set; }

        /// <summary>
        /// The proxy server password used to connect.
        /// </summary>
        public string ProxyPassword { get; set; }

        ProxySocket _sock = null;
        /// <summary>
        /// Connection
        /// </summary>
        protected ProxySocket Socket
        {
            get
            {
                if (_sock == null)
                {
                    _sock = new ProxySocket(IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _sock.ProxyType = ProxyType.None;
                    if (this.ProxyType != ProxyType.None)
                    {
                        _sock.ProxyType = this.ProxyType;
                        _sock.ProxyEndPoint = new IPEndPoint(IPAddress.Parse(this.ProxyHost), this.ProxyPort);
                        _sock.ProxyUsername = this.ProxyUsername;
                        _sock.ProxyPassword = this.ProxyPassword;
                    }
                }

                return _sock;
            }

            set
            {
                _sock = value;
            }
        }

		/// <summary>
		/// Default buffer size of the underlying socket
		/// </summary>
		public int ReceiveBufferSize {
			get {
				if (this._sock != null) {
					return this._sock.ReceiveBufferSize;
				}

				return 0;
			}

			set {
				if (this._sock != null) {
					this._sock.ReceiveBufferSize = value;
				}
			}
		}

		/// <summary>
		/// Default buffer size of the underlying socket
		/// </summary>
		public int SendBufferSize {
			get {
				if (this._sock != null) {
					return this._sock.SendBufferSize;
				}

				return 0;
			}

			set {
				if (this._sock != null) {
					this._sock.SendBufferSize = value;
				}
			}
		}

		/// <summary>
		/// Local end point
		/// </summary>
		public EndPoint LocalEndPoint {
			get {
				if (_sock != null && _sock.Connected) {
					return _sock.LocalEndPoint;
				}

				return null;
			}
		}

		/// <summary>
		/// Remote end point
		/// </summary>
		public EndPoint RemoteEndPoint {
			get {
				if (_sock != null && _sock.Connected) {
					return _sock.RemoteEndPoint;
				}

				return null;
			}
		}

		/// <summary>
		/// Indicates if there is an active connection
		/// </summary>
		public bool Connected {
			get {
				if (_sock != null)
					return _sock.Connected;
				return false;
			}
		}

        // These don't work so we're not including them.
        // In order to implement timeouts we need to use
        // asynchronous reads and writes.

		/// <summary>
		/// Gets/Sets the read timeout
		/// </summary>
		/*public int ReadTimeout {
			get {
				if (this.NetworkStream != null)
					return this.NetworkStream.ReadTimeout;
				return 0;
			}
			set {
				if (this.NetworkStream != null)
					this.NetworkStream.ReadTimeout = value;
			}
		}

		/// <summary>
		/// Gets/Sets the write timeout
		/// </summary>
		public int WriteTimeout {
			get {
				if (this.NetworkStream != null)
					return this.NetworkStream.WriteTimeout;
				return 0;
			}
			set {
				if (this.NetworkStream != null)
					this.NetworkStream.WriteTimeout = value;
			}
		}*/

		NetworkStream _stream = null;
		/// <summary>
		/// The base stream used for reading and writing the socket
		/// </summary>
		protected NetworkStream NetworkStream {
			get {
				if (_stream == null && this.Connected) {
					_stream = new NetworkStream(this.Socket);
				}

				return _stream;
			}

			private set { _stream = value; }
		}

		/// <summary>
		/// Gets the host to use for ssl certificate authentication
		/// </summary>
		protected virtual string SslAuthTargetHost {
			get { return this.Server; }
		}

		/// <summary>
		/// Checks if a certificate is valid
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		bool CheckCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            if (sslPolicyErrors == SslPolicyErrors.None) {
                return true;
            }

            this.SslPolicyErrors = sslPolicyErrors;
            this.SslCertificate = certificate;

            InvalidCertificateInfo e = new InvalidCertificateInfo(this);
            this.OnInvalidSslCerticate(this, e);
            if (!e.Ignore) {
                return false;
            }

			return true;
		}

		/// <summary>
		/// Authenticates the SSL certificate. This should be called when the stream is switched over
		/// to encryption.
		/// </summary>
		protected void AuthenticateConnection() {
			if (this.Connected && !this.SecurteStream.IsAuthenticated) {
				//this.SecurteStream.AuthenticateAsClient(((IPEndPoint)this.RemoteEndPoint).Address.ToString());
                try {
                    this.SecurteStream.AuthenticateAsClient(this.SslAuthTargetHost);
                }
                catch (AuthenticationException) {
                    this.InternalDisconnect();
                    throw new FtpInvalidCertificateException("There were errors validating the SSL certificate: " + this.SslPolicyErrors.ToString());
                }
			}
		}

		SslStream _sslStream = null;
		/// <summary>
		/// Gets a secure stream to the socket.
		/// Intended to be used with the AUTH SSL command
		/// </summary>
		protected SslStream SecurteStream {
			get {
				if (_sslStream == null) {
					this._reader = null;
					this._sslStream = new SslStream(this.NetworkStream, true,
						new RemoteCertificateValidationCallback(CheckCertificate));
				}

				return _sslStream;
			}

			private set { _sslStream = value; }
		}


		/// <summary>
		/// The base stream for reading and writing the socket
		/// </summary>
		public virtual Stream BaseStream {
			get {
				if (this.SecurteStream.IsEncrypted) {
					if (this._reader != null && this._reader.BaseStream == this.NetworkStream) {
						this._reader = new StreamReader(this.SecurteStream);
					}

					return this.SecurteStream;
				}
				else {
					return this.NetworkStream;
				}
			}
		}

        bool m_utf8Enabled = false;
        /// <summary>
        /// Gets a value indicating if UTF8 has been enabled
        /// on this connection
        /// </summary>
        public bool IsUTF8Enabled {
            get { return m_utf8Enabled; }
            protected set { m_utf8Enabled = value; }
        }

		StreamReader _reader = null;
		/// <summary>
		/// Used for easy reading from the socket
		/// </summary>
		protected StreamReader StreamReader {
			get {
                // If UTF8 is enabled and the encoding of the stream reader doesn't 
                // match up set it to null so a new isntance is created with the
                // right encoding
                if (_reader != null && m_utf8Enabled && _reader.CurrentEncoding != Encoding.UTF8)
                    _reader = null;

				if (_reader == null && this.Connected) {
                    _reader = new StreamReader(this.BaseStream, m_utf8Enabled ? Encoding.UTF8 : Encoding.Default);
				}

				return _reader;
			}

			private set { _reader = value; }
		}

		string _server = null;
		/// <summary>
		/// The FTP server to connect to
		/// </summary>
		public virtual string Server {
			get { return _server; }
			set { _server = value; }
		}

		int _port = 21;
		/// <summary>
		/// The port the FTP server is listening on
		/// </summary>
		public virtual int Port {
			get { return _port; }
			set { _port = value; }
		}

        FtpAsyncInvoker _asyncConnect = null;
        /// <summary>
        /// Gets an FtpAsyncInvoker object for asynchronous
        /// connections
        /// </summary>
        protected FtpAsyncInvoker AsyncConnect {
            get {
                if (_asyncConnect == null) {
                    _asyncConnect = new FtpAsyncInvoker(Connect);
                }

                return _asyncConnect;
            }
            private set {
                _asyncConnect = value;
            }
        }

        /// <summary>
		/// Connect this channel
		/// </summary>
		public virtual void Connect() {
			if (!this.Connected) {
				this.Disconnect(); // cleans up socket resources before making another connection

                IPAddress serverAddress = null;
                IPAddress[] addressList = null;

                if (this.AutoTryOtherAddresses == false) {
                    if (!IPAddress.TryParse(this.Server, out serverAddress)) {
                        if (_usesIPv6 == false)
                            serverAddress = Array.Find(Dns.GetHostEntry(this.Server).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                        else
                            serverAddress = Dns.GetHostEntry(this.Server).AddressList[0];
                    }

                    _ipAddress = serverAddress;
                    this.Socket.Connect(this.Server, this.Port);
                }
                else {
                    if (!IPAddress.TryParse(this.Server, out serverAddress)) {
                        if (_usesIPv6 == false)
                            addressList = Array.FindAll(Dns.GetHostEntry(this.Server).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                        else
                            addressList = Dns.GetHostEntry(this.Server).AddressList;

                        int i = 0;
                        _ipAddress = addressList[i];
                        while (this.Socket.Connected == false && i < addressList.Length) {
                            try {
                                this.Socket.Connect(this.Server, this.Port);
                            }
                            catch (SocketException ex) {
                                if (ex.ErrorCode == 10061) {
                                    this.Disconnect(); // cleans up socket resources, before making another attempt. 
                                    ++i;
                                    if (i < addressList.Length)
                                        _ipAddress = addressList[i];
                                }
                            }
                        }

                        if (this.Socket.Connected == false) {
                            // If we cannot connect to the socket even after trying all the possible addresses,
                            // we rethrow a SocketExpection that indicates the server refuses connection.
                            throw new SocketException(10061);
                        }

                    }
                    else {
                        _ipAddress = serverAddress;
                        this.Socket.Connect(this.Server, this.Port);
                    }
                }

				this.OnConnectionReady();
			}
		}

        /// <summary>
        /// Connect asynchronously
        /// </summary>
        /// <returns>IAsyncResult</returns>
        public virtual IAsyncResult BeginConnect() {
            return this.BeginConnect(null, this);
        }

        /// <summary>
        /// Connect asynchronously
        /// </summary>
        /// <param name="state"></param>
        /// <returns>IAsyncResult</returns>
        public virtual IAsyncResult BeginConnect(object state) {
            return this.BeginConnect(null, state);
        }

        /// <summary>
        /// Connect asynchronously
        /// </summary>
        /// <returns>IAsyncResult</returns>
        public virtual IAsyncResult BeginConnect(AsyncCallback callback, object state) {
            return this.AsyncConnect.BeginInvoke(callback, state);
        }

        /// <summary>
        /// End connection
        /// </summary>
        /// <param name="result">IAsyncResult returned from BeginConnect()</param>
        public virtual void EndConnect(IAsyncResult result) {
            this.AsyncConnect.EndInvoke(result);
        }

        /// <summary>
        /// Disconnects and frees up the socket and streams
        /// </summary>
        private void InternalDisconnect() {
            if (this._sock != null) {
                if (this.Connected) {
                    this._sock.Shutdown(SocketShutdown.Both);
                    this._sock.Disconnect(false);
                    this._sock.Close(5);
                }

                this.OnConnectionClosed();
            }

            if (this._stream != null) {
                this._stream.Close();
                this._stream.Dispose();
            }

            if (this._reader != null) {
                this._reader.Close();
                this._reader.Dispose();
            }

            if (this._sslStream != null) {
                this._sslStream.Close();
                this._sslStream.Dispose();
            }

            this._sock = null;
            this._stream = null;
            this._reader = null;
            this._sslStream = null;
            this._sslCertificate = null;
            this._policyErrors = Security.SslPolicyErrors.None;
        }

        /// <summary>
		/// Disconnect the socket and free up any resources being used here
		/// </summary>
		public virtual void Disconnect() {
            this.InternalDisconnect();
		}


		/// <summary>
		/// Cleanup and release resources
		/// </summary>
		public void Dispose() {
			this.Disconnect();
			this.OnDisposed();
		}
	}
}
