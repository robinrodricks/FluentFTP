using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace System.Net.FtpClient {
	public delegate void FtpChannelConnected();

	public abstract class FtpChannel : IDisposable {
		event FtpChannelConnected _onConnected = null;
		/// <summary>
		/// Event is fired after a connection has been made
		/// </summary>
		protected event FtpChannelConnected ConnectionReady {
			add { _onConnected += value; }
			remove { _onConnected -= value; }
		}

		protected void OnConnectionReady() {
			if (_onConnected != null) {
				this._onConnected();
			}
		}

		private bool _isServerSocket = false;
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

		bool _ignoreInvalidSslCerts = false;
		/// <summary>
		/// Indicates if an exception should be thrown
		/// when an invalid certifcate is encountered
		/// </summary>
		public bool IgnoreInvalidSslCertificates {
			get { return _ignoreInvalidSslCerts; }
			set { _ignoreInvalidSslCerts = value; }
		}

		Socket _sock = null;
		/// <summary>
		/// Connection
		/// </summary>
		protected Socket Socket {
			get {
				if (_sock == null) {
					_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}

				return _sock;
			}

			set {
				_sock = value;
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

		/// <summary>
		/// Gets/Sets the read timeout
		/// </summary>
		public int ReadTimeout {
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
		}

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
		/// Checks if a certificate is valid
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="sslPolicyErrors"></param>
		/// <returns></returns>
		bool CheckCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			if (sslPolicyErrors == SslPolicyErrors.None)
				return true;
			return this.IgnoreInvalidSslCertificates;
		}

		/// <summary>
		/// Authenticates the SSL certificate. This should be called when the stream is switched over
		/// to encryption.
		/// </summary>
		public void AuthenticateConnection() {
			if (this.Connected && !this.SecurteStream.IsAuthenticated) {

				this.SecurteStream.AuthenticateAsClient(((IPEndPoint)this.RemoteEndPoint).Address.ToString());
#if DEBUG
				Debug.WriteLine("Secure stream authenticated...");
#endif
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

		StreamReader _reader = null;
		/// <summary>
		/// Used for easy reading from the socket
		/// </summary>
		protected StreamReader StreamReader {
			get {
				if (_reader == null && this.Connected) {
					_reader = new StreamReader(this.BaseStream, Encoding.Default);
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

		/// <summary>
		/// Reads a line from the FTP channel socket. Use with discretion,
		/// can cause the code to freeze if you're trying to read data when no data
		/// is being sent.
		/// </summary>
		/// <returns></returns>
		public virtual string ReadLine() {
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
		public virtual int Read(byte[] buf, int offset, int size) {
			if (this.BaseStream != null) {
				return this.BaseStream.Read(buf, 0, size);
			}

			throw new FtpException("The network stream is null. Are we connected?");
		}

		/// <summary>
		/// Writes a line to the channel with the correct line endings.
		/// </summary>
		/// <param name="line">Format</param>
		/// <param name="args">Parameters</param>
		public virtual void WriteLine(string line, params object[] args) {
			this.WriteLine(line, args);
		}

		/// <summary>
		/// Writes a line to the channel with the correct line endings.
		/// </summary>
		/// <param name="line">The line to write</param>
		public virtual void WriteLine(string line) {
			this.Write(string.Format("{0}\r\n", line));
		}

		/// <summary>
		/// Writes the specified data to the network stream in the proper encoding
		/// </summary>
		public virtual void Write(string format, params object[] args) {
			this.Write(string.Format(format, args));
		}

		/// <summary>
		/// Writes the specified data to the network stream in the proper encoding
		/// </summary>
		/// <param name="data"></param>
		public virtual void Write(string data) {
#if DEBUG
			Debug.WriteLine(string.Format("< {0}", data.Trim('\n').Trim('\r')));
#endif
			this.Write(Encoding.ASCII.GetBytes(data));
		}

		/// <summary>
		/// Writes the specified byte array to the network stream
		/// </summary>
		/// <param name="buf"></param>
		public virtual void Write(byte[] buf) {
			this.Write(buf, 0, buf.Length);
		}

		/// <summary>
		/// Writes the specified byte array to the network stream
		/// </summary>
		public virtual void Write(byte[] buf, int offset, int count) {
			if (this.BaseStream != null) {
				this.BaseStream.Write(buf, offset, count);
			}
			else {
				throw new FtpException("The network stream is null. Are we connected?");
			}
		}

		public virtual void Connect() {
			this.Connect(this.Server, this.Port);
		}

		/// <summary>
		/// Open a connection
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public virtual void Connect(string host, int port) {
			if (!this.Connected) {
				this.Server = host;
				this.Port = port;
				this.Socket.Connect(host, port);
				this.OnConnectionReady();
			}
		}

		/// <summary>
		/// Open a connection
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		public virtual void Connect(IPAddress ip, int port) {
			if (!this.Connected) {
				this.Server = ip.ToString();
				this.Port = port;
				this.Socket.Connect(ip, port);
				this.OnConnectionReady();
			}
		}

		/// <summary>
		/// Open a connection
		/// </summary>
		/// <param name="ep"></param>
		public virtual void Connect(EndPoint ep) {
			if (!this.Connected) {

				if (ep is IPEndPoint) {
					IPEndPoint ipep = (IPEndPoint)ep;

					this.Server = ipep.Address.ToString();
					this.Port = ipep.Port;
				}

				this.Socket.Connect(ep);
				this.OnConnectionReady();
			}
		}

		/// <summary>
		/// Disconnect the socket and free up any resources being used here
		/// </summary>
		public virtual void Disconnect() {
#if DEBUG
			Debug.WriteLine("Called: FtpChannel.Disconnect();");
#endif
			if (this.Connected) {
				if (this._sock != null) {
					this._sock.Shutdown(SocketShutdown.Both);
					this._sock.Close();
					this._sock = null;
				}

				if (this._stream != null) {
					this._stream.Close();
					this._stream.Dispose();
					this._stream = null;
				}

				if (this._reader != null) {
					this._reader.Close();
					this._reader.Dispose();
					this._reader = null;
				}

				if (this._sslStream != null) {
					this._sslStream.Close();
					this._sslStream.Dispose();
					this._sslStream = null;
				}
			}
		}

		/// <summary>
		/// Flush the network stream this object is working with.
		/// </summary>
		public virtual void Flush() {
			if (this.Connected && this.NetworkStream != null) {
				this.NetworkStream.Flush();
			}
		}

		public void Dispose() {
			this.Disconnect();
		}
	}
}
