using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using FluentFTP.Client.BaseClient;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using FluentFTP.Streams;

namespace FluentFTP {


	/// <summary>
	/// Stream class used for talking. Used by FtpClient, extended by FtpDataStream
	/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	public class FtpSocketStream : Stream, IDisposable, IAsyncDisposable {
#else
	public class FtpSocketStream : Stream, IDisposable {
#endif

		/// <summary>
		/// The client this stream is associated with
		/// </summary>
		public readonly BaseFtpClient Client;

		/// <summary>
		/// This stream connection to be associated with this client
		/// </summary>
		public FtpSocketStream(BaseFtpClient conn) {
			Client = conn;
		}

		/// <summary>
		/// This FtpSocketStream could be a NetStream
		/// </summary>
		private NetworkStream m_netStream = null;

		/// <summary>
		/// This FtpSocketStream could be a SslStream over a NetStream
		/// </summary>
		private FtpSslStream m_sslStream = null;

		/// <summary>
		/// This FtpSocketStream could be a customStream (such as a GnuTlsStream)
		/// </summary>
		private IFtpStream m_customStream = null;

		/// <summary>
		/// Gets the underlying stream, could be a NetworkStream, SslStream or CustomStream
		/// </summary>
		protected Stream BaseStream {
			get {
				if (m_customStream != null) {
					return m_customStream.GetBaseStream();
				}
				else if (m_sslStream != null) {
					return m_sslStream;
				}
				else if (m_netStream != null) {
					return m_netStream;
				}
				return null;
			}
		}

		/// <summary>
		/// Is this stream the control connection?
		/// </summary>
		public bool IsControlConnection { get; set; } = true;

		/// <summary>
		/// Gets a value indicating if this socket stream is disposed
		/// </summary>
		public bool IsDisposed { get; set; } = false;

		/// <summary>
		/// Gets a value indicating if encryption is being used
		/// </summary>
		public bool IsEncrypted {
			get {
				if (Client.Config.CustomStream != null) {
					return m_customStream != null;
				}
				else {
					return m_sslStream != null;
				}
			}
		}

		/// <summary>
		/// Actual connection state from the FTP client to the FTP server, as determined by the NOOP Deamon.
		/// </summary>
		public FtpConnectionState ConnectionState { get; set; } = FtpConnectionState.Disconnected;

		/// <summary>
		/// Gets a value indicating if this socket stream is connected
		/// </summary>
		public bool IsConnected {
			get {
				if (m_socket == null || !m_socket.Connected || !CanRead || !CanWrite) {
					if (ConnectionState != FtpConnectionState.Disconnected) {
						ConnectionState = FtpConnectionState.PendingDisconnect;
					}
				}

				if (ConnectionState == FtpConnectionState.Unknown) {
					Thread.Sleep(500);
					if (ConnectionState == FtpConnectionState.Unknown) {
						((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Connection state unknown. Waiting for timeout");
						DateTime startTime = DateTime.UtcNow;
						while (ConnectionState == FtpConnectionState.Unknown &&
							DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < 20000) {
							Thread.Sleep(1000);
						}
					}
				}

				if (ConnectionState == FtpConnectionState.PendingDisconnect) {
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Connection state pending down. Closing");
					if (Client is AsyncFtpClient) {
						Task.Run(async () => await CloseAsync()).Wait();
					}
					else {
						Close();
					}
				}

				return ConnectionState == FtpConnectionState.Connected;
			}
		}

		/// <summary>
		/// Used for tracking read/write activity on the socket
		/// </summary>
		private DateTime m_lastActivity = DateTime.UtcNow;

		private Socket m_socket = null;

		/// <summary>
		/// The socket used for talking on this connection
		/// </summary>
		protected Socket Socket {
			get => m_socket;
			private set => m_socket = value;
		}

		/// <summary>
		/// Gets the number of available bytes on the socket, 0 if the
		/// socket has not been initialized.
		/// </summary>
		internal int SocketDataAvailable {
			get {
				if (m_socket != null) {
					return m_socket.Available;
				}

				return 0;
			}
		}

		/// <summary>
		/// Keep a count of command/response transactions on the
		/// control connection
		/// </summary>
		public int SslSessionLength = 0;

		/// <summary>
		/// The negotiated SSL/TLS protocol version. Will have a valid value after connection is complete.
		/// </summary>
		public SslProtocols SslProtocol {
			get {
				if (Client.Config.CustomStream != null) {
					return IsEncrypted ? m_customStream.GetSslProtocol() : SslProtocols.None;
				}
				else {
					return IsEncrypted ? m_sslStream.SslProtocol : SslProtocols.None;
				}
			}
		}

		/// <summary>
		/// The negotiated SSL/TLS protocol ciphers. Will have a valid value after connection is complete.
		/// </summary>
		public string SslCipherSuite {
			get {
				if (Client.Config.CustomStream != null) {
					return IsEncrypted ? m_customStream.GetCipherSuite() : string.Empty;
				}
				else {
					return IsEncrypted ? m_sslStream.ToString() : string.Empty;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating if this stream can be read
		/// </summary>
		public override bool CanRead {
			get {
				if (m_customStream != null) {
					return m_customStream.CanRead();
				}
				else if (m_sslStream != null) {
					return m_sslStream.CanRead;
				}
				else if (m_netStream != null) {
					return m_netStream.CanRead;
				}

				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating if this stream can be written to
		/// </summary>
		public override bool CanWrite {
			get {
				if (m_customStream != null) {
					return m_customStream.CanWrite();
				}
				else if (m_sslStream != null) {
					return m_sslStream.CanWrite;
				}
				else if (m_netStream != null) {
					return m_netStream.CanWrite;
				}

				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating if this stream if seekable
		/// </summary>
		public override bool CanSeek => false;

		/// <summary>
		/// Gets the length of the stream
		/// </summary>
		public override long Length => 0;

		/// <summary>
		/// Gets the current position of the stream. Trying to
		/// set this property throws an InvalidOperationException()
		/// </summary>
		public override long Position {
			get {
				if (BaseStream != null) {
					return BaseStream.Position;
				}

				return 0;
			}
			set => throw new InvalidOperationException();
		}

		private int m_readTimeout = Timeout.Infinite;

		/// <summary>
		/// Gets or sets the amount of time to wait for a read operation to complete. Default
		/// value is Timeout.Infinite.
		/// </summary>
		public override int ReadTimeout {
			get => m_readTimeout;
			set {
				m_readTimeout = value;
				if (m_netStream != null) {
					m_netStream.ReadTimeout = m_readTimeout;
				}
			}
		}

		private int m_connectTimeout = int.MaxValue;

		/// <summary>
		/// Gets or sets the length of time milliseconds to wait
		/// for a connection succeed before giving up. The default
		/// is 0 = disable, use system default timeout.
		/// </summary>
		public int ConnectTimeout {
			get => m_connectTimeout;
			set {
				m_connectTimeout = value > 0 ? value : int.MaxValue;
			}
		}

		/// <summary>
		/// Gets the local end point of the socket
		/// </summary>
		public IPEndPoint LocalEndPoint {
			get {
				if (m_socket == null) {
					return null;
				}

				return (IPEndPoint)m_socket.LocalEndPoint;
			}
		}

		/// <summary>
		/// Gets the remote end point of the socket
		/// </summary>
		public IPEndPoint RemoteEndPoint {
			get {
				if (m_socket == null) {
					return null;
				}

				return (IPEndPoint)m_socket.RemoteEndPoint;
			}
		}

		private event FtpSocketStreamSslValidation m_sslvalidate = null;

		/// <summary>
		/// Event is fired when a SSL certificate needs to be validated
		/// </summary>
		public event FtpSocketStreamSslValidation ValidateCertificate {
			add => m_sslvalidate += value;
			remove => m_sslvalidate -= value;
		}

		/// <summary>
		/// Fires the SSL certificate validation event
		/// </summary>
		/// <param name="certificate">Certificate being validated</param>
		/// <param name="chain">Certificate chain</param>
		/// <param name="errors">Policy errors if any</param>
		/// <returns>True if it was accepted, false otherwise</returns>
		protected bool OnValidateCertificate(X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
			var evt = m_sslvalidate;

			if (evt != null) {
				var e = new FtpSslValidationEventArgs() {
					Certificate = certificate,
					Chain = chain,
					PolicyErrors = errors,
					Accept = errors == SslPolicyErrors.None
				};

				evt(this, e);
				return e.Accept;
			}

			// if the event was not handled then only accept
			// the certificate if there were no validation errors
			return errors == SslPolicyErrors.None;
		}

		/// <summary>
		/// Fires the SSL certificate validation event for custom encrypted streams
		/// </summary>
		/// <param name="certificate">Certificate being validated</param>
		/// <param name="chain">Certificate chain</param>
		/// <param name="errorMessage">Policy errors if any</param>
		/// <returns>True if it was accepted, false otherwise</returns>
		protected bool OnValidateCertificate(X509Certificate certificate, X509Chain chain, string errorMessage) {
			var evt = m_sslvalidate;

			if (evt != null) {
				var e = new FtpSslValidationEventArgs() {
					Certificate = certificate,
					Chain = chain,
					PolicyErrors = String.IsNullOrEmpty(errorMessage) ? SslPolicyErrors.None : SslPolicyErrors.RemoteCertificateNameMismatch,
					PolicyErrorMessage = errorMessage,
					Accept = errorMessage == string.Empty,
				};

				evt(this, e);
				return e.Accept;
			}

			// if the event was not handled then only accept
			// the certificate if there were no validation errors
			return errorMessage == string.Empty;
		}

		/// <summary>
		/// Throws an InvalidOperationException
		/// </summary>
		/// <param name="offset">Ignored</param>
		/// <param name="origin">Ignored</param>
		/// <returns></returns>
		public override long Seek(long offset, SeekOrigin origin) {
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Throws an InvalidOperationException
		/// </summary>
		/// <param name="value">Ignored</param>
		public override void SetLength(long value) {
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Flushes the stream
		/// </summary>
		public override void Flush() {
			BaseStream.Flush();
		}

		/// <summary>
		/// Flushes the stream asynchronously
		/// </summary>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public override async Task FlushAsync(CancellationToken token) {
			await BaseStream.FlushAsync(token);
		}

		/// <summary>
		/// Bypass the stream and read directly off the socket.
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <returns>The number of bytes read</returns>
		internal int RawSocketRead(byte[] buffer) {
			var read = 0;

			if (m_socket != null && m_socket.Connected) {
				read = m_socket.Receive(buffer, buffer.Length, 0);
			}

			return read;
		}

#if NETSTANDARD || NET5_0_OR_GREATER
		/// <summary>
		/// Bypass the stream and read directly off the socket.
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The number of bytes read</returns>
		internal async Task<int> RawSocketReadAsync(byte[] buffer, CancellationToken token) {
			var read = 0;

			if (m_socket != null && m_socket.Connected && !token.IsCancellationRequested) {
#if NET6_0_OR_GREATER
				read = await m_socket.ReceiveAsync(buffer, 0, token);
#else
				read = await m_socket.ReceiveAsync(new ArraySegment<byte>(buffer), 0);
#endif
			}

			return read;
		}
#else
		/// <summary>
		/// Bypass the stream and read directly off the socket.
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The number of bytes read</returns>
		internal async Task<int> RawSocketReadAsync(byte[] buffer, CancellationToken token) {
			var read = 0;

			if (m_socket != null && m_socket.Connected) {
				var asyncResult = m_socket.BeginReceive(buffer, 0, buffer.Length, 0, null, null);
				read = await EnableCancellation(
					Task.Factory.FromAsync(asyncResult, m_socket.EndReceive),
					token,
					() => DisposeSocket()
				);
			}

			return read;
		}
#endif

		/// <summary>
		/// Reads data from the stream
		/// </summary>
		/// <param name="buffer">Buffer to read into</param>
		/// <param name="offset">Where in the buffer to start</param>
		/// <param name="count">Number of bytes to be read</param>
		/// <returns>The amount of bytes read from the stream</returns>
		public override int Read(byte[] buffer, int offset, int count) {
			if (BaseStream == null) {
				return 0;
			}

#if NETFRAMEWORK
			IAsyncResult ar = null;
#endif

			m_lastActivity = DateTime.UtcNow;
#if NETSTANDARD || NET5_0_OR_GREATER
			return BaseStream.Read(buffer, offset, count);
#else
			ar = BaseStream.BeginRead(buffer, offset, count, null, null);
			bool success = ar.AsyncWaitHandle.WaitOne(m_readTimeout, true);
			if (!success) {
				Close();
				throw new TimeoutException("Timed out trying to read data from the socket stream!");
			}
			else if (Type.GetType("Mono.Runtime") == null) {
				ar.AsyncWaitHandle.Close(); // See issue #648 this needs to be commented out for MONO
			}
			return BaseStream.EndRead(ar);
#endif
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Reads data from the stream
		/// </summary>
		/// <param name="buffer">Buffer to read into</param>
		/// <returns>The amount of bytes read from the stream</returns>
		public override int Read(Span<byte> buffer) {
			if (BaseStream == null) {
				return 0;
			}

			m_lastActivity = DateTime.UtcNow;

			return BaseStream.Read(buffer);
		}
#endif

		/// <summary>
		/// Reads data from the stream
		/// </summary>
		/// <param name="buffer">Buffer to read into</param>
		/// <param name="offset">Where in the buffer to start</param>
		/// <param name="count">Number of bytes to be read</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		/// <returns>The amount of bytes read from the stream</returns>
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) {
			if (BaseStream == null) {
				return 0;
			}

			m_lastActivity = DateTime.UtcNow;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
				cts.CancelAfter(ReadTimeout);
				try {
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
					var res = await BaseStream.ReadAsync(buffer.AsMemory(offset, count), cts.Token);
#else
					var res = await BaseStream.ReadAsync(buffer, offset, count, cts.Token);
#endif
					return res;
				}
				catch {
					if (cts.IsCancellationRequested) {
						await CloseAsync(token);
					}
    
					// CTS for Cancellation triggered and caused the exception
					if (token.IsCancellationRequested) {
						throw new OperationCanceledException("Cancelled read from socket stream");
					}

					// CTS for Timeout triggered and caused the exception
					if (cts.IsCancellationRequested) {
						throw new TimeoutException("Timed out trying to read data from the socket stream!");
					}

					// Nothing of the above. So we rethrow the exception.
					throw;
				}
			}
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Reads data from the stream
		/// </summary>
		/// <param name="buffer">Buffer to read into</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		/// <returns>The amount of bytes read from the stream</returns>
		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token) {
			if (BaseStream == null) {
				return 0;
			}

			m_lastActivity = DateTime.UtcNow;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
				cts.Token.Register(async () => await CloseAsync(token));
				try {
					var res = await BaseStream.ReadAsync(buffer, cts.Token);
					return res;
				}
				catch {
					if (cts.IsCancellationRequested) {
						await CloseAsync(token);
					}

					// CTS for Cancellation triggered and caused the exception
					if (token.IsCancellationRequested) {
						throw new OperationCanceledException("Cancelled read from socket stream");
					}

					// CTS for Timeout triggered and caused the exception
					if (cts.IsCancellationRequested) {
						throw new TimeoutException("Timed out trying to read data from the socket stream!");
					}

					// Nothing of the above. So we rethrow the exception.
					throw;
				}
			}
		}
#endif

		/// <summary>
		/// Reads a line from the socket
		/// </summary>
		/// <param name="encoding">The type of encoding used to convert from byte[] to string</param>
		/// <returns>A line from the stream, null if there is nothing to read</returns>
		public string ReadLine(System.Text.Encoding encoding) {
			var data = new List<byte>();
			var buf = new byte[1];
			string line = null;

			while (Read(buf, 0, buf.Length) > 0) {
				data.Add(buf[0]);
				if ((char)buf[0] == '\n') {
					line = encoding.GetString(data.ToArray()).Trim('\r', '\n');
					break;
				}
			}

			// This is for the AutoReconnect feature
			if (IsControlConnection && IsEncrypted) {
				SslSessionLength++;
			}

			return line;
		}

		/// <summary>
		/// Reads all lines from the socket
		/// </summary>
		/// <param name="encoding">The type of encoding used to convert from byte[] to string</param>
		/// <param name="bufferSize">The size of the buffer</param>
		/// <returns>A list of lines from the stream</returns>
		public IEnumerable<string> ReadAllLines(System.Text.Encoding encoding, int bufferSize) {
			int charRead;
			var data = new List<byte>();
			var buf = new byte[bufferSize];

			while ((charRead = Read(buf, 0, buf.Length)) > 0) {
				var firstByteToReadIdx = 0;

				var separatorIdx = Array.IndexOf(buf, (byte)'\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array readed

				while (separatorIdx >= 0) // at least one '\n' returned
				{
					while (firstByteToReadIdx <= separatorIdx) {
						data.Add(buf[firstByteToReadIdx++]);
					}

					var line = encoding.GetString(data.ToArray()).Trim('\r', '\n'); // convert data to string
					yield return line;
					data.Clear();

					separatorIdx = Array.IndexOf(buf, (byte)'\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array readed

					// This is for the AutoReconnect feature
					if (IsControlConnection && IsEncrypted) {
						SslSessionLength++;
					}
				}

				while (firstByteToReadIdx < charRead) // add all remaining characters to data
				{
					data.Add(buf[firstByteToReadIdx++]);
				}
			}
		}

		/// <summary>
		/// Reads a line from the socket asynchronously
		/// </summary>
		/// <param name="encoding">The type of encoding used to convert from byte[] to string</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		/// <returns>A line from the stream, null if there is nothing to read</returns>
		public async Task<string> ReadLineAsync(System.Text.Encoding encoding, CancellationToken token) {
			var data = new List<byte>();
			var buf = new byte[1];
			string line = null;

			while (await ReadAsync(buf, 0, buf.Length, token) > 0) {
				data.Add(buf[0]);
				if ((char)buf[0] == '\n') {
					line = encoding.GetString(data.ToArray()).Trim('\r', '\n');
					break;
				}
			}

			// This is for the AutoReconnect feature
			if (IsControlConnection && IsEncrypted) {
				SslSessionLength++;
			}

			return line;
		}

		/// <summary>
		/// Reads all lines from the socket
		/// </summary>
		/// <param name="encoding">The type of encoding used to convert from byte[] to string</param>
		/// <param name="bufferSize">The size of the buffer</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A list of lines from the stream</returns>
		public async Task<IEnumerable<string>> ReadAllLinesAsync(System.Text.Encoding encoding, int bufferSize, CancellationToken token) {
			int charRead;
			var data = new List<byte>();
			var lines = new List<string>();
			var buf = new byte[bufferSize];

			while ((charRead = await ReadAsync(buf, 0, buf.Length, token)) > 0) {
				var firstByteToReadIdx = 0;

				var separatorIdx = Array.IndexOf(buf, (byte)'\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array read

				while (separatorIdx >= 0) // at least one '\n' returned
				{
					while (firstByteToReadIdx <= separatorIdx) {
						data.Add(buf[firstByteToReadIdx++]);
					}

					var line = encoding.GetString(data.ToArray()).Trim('\r', '\n'); // convert data to string
					lines.Add(line);
					data.Clear();

					separatorIdx = Array.IndexOf(buf, (byte)'\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array read

					// This is for the AutoReconnect feature
					if (IsControlConnection && IsEncrypted) {
						SslSessionLength++;
					}
				}

				while (firstByteToReadIdx < charRead) // add all remaining characters to data
				{
					data.Add(buf[firstByteToReadIdx++]);
				}
			}

			return lines;
		}

		/// <summary>
		/// Writes data to the stream
		/// </summary>
		/// <param name="buffer">Buffer to write to stream</param>
		/// <param name="offset">Where in the buffer to start</param>
		/// <param name="count">Number of bytes to be written</param>
		public override void Write(byte[] buffer, int offset, int count) {
			if (BaseStream == null) {
				return;
			}

			BaseStream.Write(buffer, offset, count);
			m_lastActivity = DateTime.UtcNow;
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Writes data to the stream
		/// </summary>
		/// <param name="buffer">Buffer to write to stream</param>
		public override void Write(ReadOnlySpan<byte> buffer) {
			if (BaseStream == null) {
				return;
			}

			BaseStream.Write(buffer);
			m_lastActivity = DateTime.UtcNow;
		}
#endif

		/// <summary>
		/// Writes data to the stream asynchronously
		/// </summary>
		/// <param name="buffer">Buffer to write to stream</param>
		/// <param name="offset">Where in the buffer to start</param>
		/// <param name="count">Number of bytes to be written</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) {
			if (BaseStream == null) {
				return;
			}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
			await BaseStream.WriteAsync(buffer.AsMemory(offset, count), token);
#else
			await BaseStream.WriteAsync(buffer, offset, count, token);
#endif
			m_lastActivity = DateTime.UtcNow;
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Writes data to the stream asynchronously
		/// </summary>
		/// <param name="buffer">Buffer to write to stream</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token) {
			if (BaseStream == null) {
				return;
			}

			await BaseStream.WriteAsync(buffer, token);
			m_lastActivity = DateTime.UtcNow;
		}
#endif

		/// <summary>
		/// Writes a line to the stream using the specified encoding
		/// </summary>
		/// <param name="encoding">Encoding used for writing the line</param>
		/// <param name="buf">The data to write</param>
		public void WriteLine(System.Text.Encoding encoding, string buf) {
			byte[] data;
			data = encoding.GetBytes(buf + "\r\n");
			Write(data, 0, data.Length);
		}

		/// <summary>
		/// Writes a line to the stream using the specified encoding asynchronously
		/// </summary>
		/// <param name="encoding">Encoding used for writing the line</param>
		/// <param name="buf">The data to write</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public async Task WriteLineAsync(System.Text.Encoding encoding, string buf, CancellationToken token) {
			var data = encoding.GetBytes(buf + "\r\n");
			await WriteAsync(data, 0, data.Length, token);
		}

		/// <summary>
		/// Sets socket options on the underlying socket
		/// </summary>
		/// <param name="level">SocketOptionLevel</param>
		/// <param name="name">SocketOptionName</param>
		/// <param name="value">SocketOptionValue</param>
		public void SetSocketOption(SocketOptionLevel level, SocketOptionName name, bool value) {
			if (m_socket == null) {
				throw new InvalidOperationException("The underlying socket is null. Have you established a connection?");
			}

			m_socket.SetSocketOption(level, name, value);
		}

		/// <summary>
		/// Check if the specified IP Address is allowed
		/// </summary>
		/// <param name="ipad">The ip address to connect to</param>
		/// <param name="ipVersions">The enum value of allowed IP Versions</param>
		/// <param name="ipVersionString">Textual representation of the address family</param>
		private static bool IsIpVersionAllowed(IPAddress ipad, FtpIpVersion ipVersions, out string ipVersionString) {
			ipVersionString = string.Empty;

			if (ipVersions == FtpIpVersion.ANY) {
				return true;
			}

			bool allowIPv4 = ipVersions.HasFlag(FtpIpVersion.IPv4);
			bool allowIPv6 = ipVersions.HasFlag(FtpIpVersion.IPv6);

			bool addrIsIPv4 = ipad.AddressFamily == AddressFamily.InterNetwork;
			bool addrIsIPv6 = ipad.AddressFamily == AddressFamily.InterNetworkV6;

			if (addrIsIPv4) {
				ipVersionString = "IPv4";
			}
			else if (addrIsIPv6) {
				ipVersionString = "IPv6";
			}
			else {
				ipVersionString = ipad.AddressFamily.ToString();
			}

			return (addrIsIPv4 && allowIPv4) || (addrIsIPv6 && allowIPv6);
		}

		/// <summary>
		/// Get the IP Address(es) associated with this host
		/// </summary>
		/// <param name="host">The host to query</param>
		private IPAddress[] GetCachedHostAddresses(string host) {

			if (!Client.Status.CachedHostIpads.TryGetValue(host, out IPAddress[] ipads)) {
#if NETSTANDARD || NET5_0_OR_GREATER
				ipads = Dns.GetHostAddressesAsync(host).Result;
#else
				ipads = Dns.GetHostAddresses(host);
#endif
				Client.Status.CachedHostIpads[host] = ipads;
			}

			return ipads;
		}

		/// <summary>
		/// Set the connected IP Address associated with this host
		/// </summary>
		/// <param name="host">The host to query</param>
		/// <param name="ipad">The IP address to store in the cache</param>
		private void SetCachedHostAddresses(string host, IPAddress ipad) {
			Client.Status.CachedHostIpads[host] = new IPAddress[1] { ipad };
		}

		/// <summary>
		/// Connect to the specified host
		/// </summary>
		/// <param name="host">The host to connect to</param>
		/// <param name="port">The port to connect to</param>
		/// <param name="ipVersions">Internet Protocol versions to support during the connection phase</param>
		public void Connect(string host, int port, FtpIpVersion ipVersions) {

			IPAddress[] ipads = GetCachedHostAddresses(host);
			IPAddress ipad = null;

			if (ipVersions == 0) {
				throw new ArgumentException("The ipVersions parameter must contain at least 1 flag.");
			}

			for (var i = 0; i < ipads.Length; i++) {
				int iPlusOne = i + 1;

				ipad = ipads[i];

				string logIp = Client.Config.LogHost ? ipad.ToString() : "***";
				string connText = this.IsControlConnection ? "control" : "data";

				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Connecting(sync) " + Client.ClientType + ".FtpSocketStream(" + connText + ") IP #" + iPlusOne + " = " + logIp + ":" + port);

				if (!IsIpVersionAllowed(ipad, ipVersions, out string logFamily)) {
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...skipped IP #" + iPlusOne + " - " + logFamily + ", IP address: " + logIp);
					continue;
				}

				m_socket = new Socket(ipad.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

				BindSocketToLocalIp();

				bool lastIP = iPlusOne == ipads.Length;

				try {
					if (ConnectHelper(ipad, port)) {
						break;
					}
					else {
						((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...failed to connect to IP #" + iPlusOne);
					}
				}
				catch (TimeoutException) {
					if (lastIP) {
						throw new TimeoutException("Timed out trying to connect!");
					}
					else {
						((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...timeout connecting to IP #" + iPlusOne);
					}
				}
				catch (Exception ex) when (!lastIP) {
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...error connecting to IP #" + iPlusOne, ex);
				}
			}

			if (m_socket == null || !m_socket.Connected) {
				Close();
				throw new IOException("Failed to connect to host.");
			}


			SetCachedHostAddresses(host, ipad);

			m_netStream = new NetworkStream(m_socket);
			m_netStream.ReadTimeout = m_readTimeout;
			m_lastActivity = DateTime.UtcNow;

			ConnectionState = FtpConnectionState.Connected;

			if (!IsControlConnection) {
				// the NOOP daemon needs to know this
				Client.Status.NoopDaemonCmdMode = false;
			}
		}

		/// <summary>
		/// Connect to the specified host
		/// Detects timeout and throws that explicitly
		/// </summary>
		/// <param name="ipad">The ip address to connect to</param>
		/// <param name="port">The port to connect to</param>
		private bool ConnectHelper(IPAddress ipad, int port) {

			int ctmo = this.ConnectTimeout;

#if NETSTANDARD || NET5_0_OR_GREATER
			var args = new SocketAsyncEventArgs {
				RemoteEndPoint = new IPEndPoint(ipad, port)
			};
			var connectEvent = new ManualResetEvent(false);
			args.Completed += (s, e) => { connectEvent.Set(); };

			if (m_socket.ConnectAsync(args)) {
				if (!connectEvent.WaitOne(ctmo)) {
					Close();
					throw new TimeoutException("Timed out trying to connect!");
				}
			}

			if (args.SocketError == SocketError.TimedOut) {
				throw new TimeoutException("Timed out trying to connect!");
			}

			return args.SocketError == SocketError.Success;
#else
			IAsyncResult iar = m_socket.BeginConnect(ipad, port, null, null);
			_ = iar.AsyncWaitHandle.WaitOne(ctmo, true);
			if (!m_socket.Connected) {
				Close();
				throw new TimeoutException("Timed out trying to connect!");
			}
			else {
				iar.AsyncWaitHandle.Close();
				m_socket.EndConnect(iar);
			}

			return m_socket.Connected;
#endif
		}

		/// <summary>
		/// Get the IP Address(es) associated with this host
		/// </summary>
		/// <param name="host">The host to query</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		private async Task<IPAddress[]> GetCachedHostAddressesAsync(string host, CancellationToken token) {

			if (!Client.Status.CachedHostIpads.TryGetValue(host, out IPAddress[] ipads)) {
#if NET6_0_OR_GREATER
				ipads = await Dns.GetHostAddressesAsync(host, token);
#else
				ipads = await Dns.GetHostAddressesAsync(host);
#endif
				Client.Status.CachedHostIpads[host] = ipads;
			}

			return ipads;
		}

		/// <summary>
		/// Connect to the specified host
		/// </summary>
		/// <param name="host">The host to connect to</param>
		/// <param name="port">The port to connect to</param>
		/// <param name="ipVersions">Internet Protocol versions to support during the connection phase</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task ConnectAsync(string host, int port, FtpIpVersion ipVersions, CancellationToken token) {

			IPAddress[] ipads = await GetCachedHostAddressesAsync(host, token);
			IPAddress ipad = null;

			if (ipVersions == 0) {
				throw new ArgumentException("The ipVersions parameter must contain at least 1 flag.");
			}

			for (var i = 0; i < ipads.Length; i++) {
				int iPlusOne = i + 1;

				ipad = ipads[i];

				string logIp = Client.Config.LogHost ? ipad.ToString() : "***";
				string connText = this.IsControlConnection ? "control" : "data";

				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Connecting(async) " + Client.ClientType + ".FtpSocketStream(" + connText + ") IP #" + iPlusOne + " = " + logIp + ":" + port);

				if (!IsIpVersionAllowed(ipad, ipVersions, out string logFamily)) {
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...skipped IP #" + iPlusOne + " - " + logFamily + ", IP address: " + logIp);
					continue;
				}

				m_socket = new Socket(ipad.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

				BindSocketToLocalIp();

				bool lastIP = iPlusOne == ipads.Length;

				try {
					if (await ConnectHelperAsync(ipad, port, token)) {
						break;
					}
					else {
						((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...failed to connect to IP #" + iPlusOne);
					}
				}
				catch (TimeoutException) {
					if (lastIP) {
						throw new TimeoutException("Timed out trying to connect!");
					}
					else {
						((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...timeout connecting to IP #" + iPlusOne);
					}
				}
				catch (Exception ex) when (!lastIP) {
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "...error connecting to IP #" + iPlusOne, ex);
				}
			}

			if (m_socket == null || !m_socket.Connected) {
				Close();
				throw new IOException("Failed to connect to host.");
			}

			SetCachedHostAddresses(host, ipad);

			m_netStream = new NetworkStream(m_socket);
			m_netStream.ReadTimeout = m_readTimeout;
			m_lastActivity = DateTime.UtcNow;

			ConnectionState = FtpConnectionState.Connected;

			if (!IsControlConnection) {
				// the NOOP daemon needs to know this
				Client.Status.NoopDaemonCmdMode = false;
			}
		}

		/// <summary>
		/// Helper for Async cancel in ConnectAsync 
		/// </summary>
		internal async Task EnableCancellation(Task task, CancellationToken token, Action action) {
			var registration = token.Register(action);
			_ = task.ContinueWith(x => registration.Dispose(), CancellationToken.None);
			await task;
		}

		/// <summary>
		/// Helper for Async cancel in ConnectAsync 
		/// </summary>
		internal async Task<T> EnableCancellation<T>(Task<T> task, CancellationToken token, Action action) {
			var registration = token.Register(action);
			_ = task.ContinueWith(x => registration.Dispose(), CancellationToken.None);
			return await task;
		}

		/// <summary>
		/// Connect to the specified host
		/// Detects timeout and throws that explicitly
		/// </summary>
		/// <param name="ipad">The ip address to connect to</param>
		/// <param name="port">The port to connect to</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		private async Task<bool> ConnectHelperAsync(IPAddress ipad, int port, CancellationToken token) {
			try {
				using (var timeoutSrc = CancellationTokenSource.CreateLinkedTokenSource(token)) {
					timeoutSrc.CancelAfter(ConnectTimeout);
#if NET462
					var connectResult = m_socket.BeginConnect(ipad, port, null, null);
					await EnableCancellation(Task.Factory.FromAsync(connectResult, m_socket.EndConnect), timeoutSrc.Token, () => DisposeSocket());
#else
					await EnableCancellation(m_socket.ConnectAsync(ipad, port), timeoutSrc.Token, () => DisposeSocket());
#endif
				}
			}
#if !NETSTANDARD
			catch (ObjectDisposedException) {
				throw new TimeoutException("Timed out trying to connect!");
			}
#endif
			catch (SocketException ex) when (ex.SocketErrorCode is SocketError.OperationAborted or SocketError.TimedOut) {
				throw new TimeoutException("Timed out trying to connect!");
			}

			return m_socket.Connected;
		}

		/// <summary>
		/// Activates SSL on this stream using the specified protocols. Fires the ValidateCertificate event.
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will
		/// not be accepted.
		/// </summary>
		/// <param name="targetHost">The host to authenticate the certificate against</param>
		/// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
		/// <param name="sslProtocols">A bitwise parameter for supported encryption protocols.</param>
		/// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
		public void ActivateEncryption(string targetHost, X509CertificateCollection clientCerts, SslProtocols sslProtocols) {
			if (!IsConnected) {
				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
			}

			if (m_netStream == null) {
				throw new InvalidOperationException("The base network stream is null.");
			}

			var authStart = DateTime.Now;
			string authType, cipherSuite;

			if (Client.Config.CustomStream != null) {
				if (m_customStream != null) {
					throw new InvalidOperationException("SSL Encryption has already been enabled on this stream.");
				}

				authType = Client.Config.CustomStream.ToString();

				try {
					CreateCustomStream(targetHost);
				}
				catch (Exception ex) {
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Error, "FTPS Authentication failed, lib = " + authType, ex, true);
					if (ex.InnerException != null) {
						((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Error, ex.InnerException.Message);
					}
					Close();
					throw;
				}

				cipherSuite = m_customStream.GetCipherSuite();
			}
			else {
				if (m_sslStream != null) {
					throw new InvalidOperationException("SSL Encryption has already been enabled on this stream.");
				}

				authType = ".NET SslStream";

				try {
					CreateSslStream();

					try {
						m_sslStream.AuthenticateAsClient(targetHost, clientCerts, sslProtocols, Client.Config.ValidateCertificateRevocation);
					}
#if NETSTANDARD || NET5_0_OR_GREATER
					catch (AggregateException ex) {
						if (ex.InnerException is AuthenticationException) {
							throw ex.InnerException;
						}
						if (ex.InnerException is IOException) {
							throw new AuthenticationException(ex.InnerException.Message);
						}
						throw;
					}
#endif
					catch (IOException ex) {
						if (ex.InnerException is Win32Exception { NativeErrorCode: 10053 }) {
							throw new FtpMissingSocketException(ex);
						}
#if NETFRAMEWORK
						throw new AuthenticationException(ex.Message);
#else
						throw;
#endif
					}

				}

				catch (AuthenticationException ex) {
					// authentication failed and in addition it left our
					// ssl stream in an unusable state so cleanup needs
					// to be done and the exception can be re-thrown for
					// handling down the chain.
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Error, "FTPS Authentication failed, lib = " + authType, ex, true);
					Close();
					throw;
				}

				cipherSuite = m_sslStream.ToString();
			}

			if (Client.Config.LogDurations) {
				var authDuration = DateTime.Now.Subtract(authStart);
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Info, "FTPS authentication successful, lib = " + authType + ", cipher suite = " + cipherSuite + " [" + authDuration.ToShortString() + "]");
			}
			else {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Info, "FTPS authentication successful, lib = " + authType + ", cipher suite = " + cipherSuite);
			}
		}

		/// <summary>
		/// Activates SSL on this stream using the specified protocols. Fires the ValidateCertificate event.
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will
		/// not be accepted.
		/// </summary>
		/// <param name="targetHost">The host to authenticate the certificate against</param>
		/// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
		/// <param name="sslProtocols">A bitwise parameter for supported encryption protocols.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
		public async Task ActivateEncryptionAsync(string targetHost, X509CertificateCollection clientCerts, SslProtocols sslProtocols, CancellationToken token = default) {
			if (!IsConnected) {
				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
			}

			if (m_netStream == null) {
				throw new InvalidOperationException("The base network stream is null.");
			}

			var authStart = DateTime.Now;
			string authType, cipherSuite;

			if (Client.Config.CustomStream != null) {
				if (m_customStream != null) {
					throw new InvalidOperationException("SSL Encryption has already been enabled on this stream.");
				}

				authType = Client.Config.CustomStream.ToString();

				try {
					CreateCustomStream(targetHost);
				}
				catch (Exception ex) {
					await CloseAsync(token);
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Error, "FTPS Authentication failed, lib = " + authType, ex, true);
					throw;
				}

				cipherSuite = m_customStream.GetCipherSuite();
			}
			else {
				if (m_sslStream != null) {
					throw new InvalidOperationException("SSL Encryption has already been enabled on this stream.");
				}

				authType = ".NET SslStream";

				try {
					CreateSslStream();

					try {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
						var options = new SslClientAuthenticationOptions() {
							TargetHost = targetHost,
							ClientCertificates = clientCerts,
							EnabledSslProtocols = sslProtocols,
							CertificateRevocationCheckMode = Client.Config.ValidateCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck
						};
						await m_sslStream.AuthenticateAsClientAsync(options, token);
#else
						await m_sslStream.AuthenticateAsClientAsync(targetHost, clientCerts, sslProtocols, Client.Config.ValidateCertificateRevocation);
#endif
					}
#if NETSTANDARD || NET5_0_OR_GREATER
					catch (AggregateException ex) {
						if (ex.InnerException is AuthenticationException) {
							throw ex.InnerException;
						}
						if (ex.InnerException is IOException) {
							throw new AuthenticationException(ex.InnerException.Message);
						}
						throw;
					}
#endif
					catch (IOException ex) {
						if (ex.InnerException is Win32Exception { NativeErrorCode: 10053 }) {
							throw new FtpMissingSocketException(ex);
						}
#if NETFRAMEWORK
						throw new AuthenticationException(ex.Message);
#else
						throw;
#endif
					}

				}

				catch (AuthenticationException ex) {
					await CloseAsync(token);
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Error, "FTPS Authentication failed, lib = " + authType, ex, true);
					throw;
				}

				cipherSuite = m_sslStream.ToString();
			}

			if (Client.Config.LogDurations) {
				var authDuration = DateTime.Now.Subtract(authStart);
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Info, "FTPS authentication successful, lib = " + authType + ", cipher suite = " + cipherSuite + " [" + authDuration.ToShortString() + "]");
			}
			else {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Info, "FTPS authentication successful, lib = " + authType + ", cipher suite = " + cipherSuite);
			}
		}

		private void CreateSslStream() {

			m_sslStream = new FtpSslStream(m_netStream, true, new RemoteCertificateValidationCallback(
				delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return OnValidateCertificate(certificate, chain, sslPolicyErrors); }
			));

		}

		private void CreateCustomStream(string targetHost) {

			m_customStream = Activator.CreateInstance(Client.Config.CustomStream) as IFtpStream;
			m_customStream.Init(Client,
				targetHost,
				m_socket,
				new CustomRemoteCertificateValidationCallback(delegate (object sender, X509Certificate certificate, X509Chain chain, string errorMessage) { return OnValidateCertificate(certificate, chain, errorMessage); }),
				IsControlConnection,
				((IInternalFtpClient)Client).GetBaseStream().m_customStream,
				Client.Config.CustomStreamConfig);

		}

		//#if NETFRAMEWORK
		//		/// <summary>
		//		/// Deactivates SSL on this stream using the specified protocols and reverts back to plain-text FTP.
		//		/// </summary>
		//		public void DeactivateEncryption() {
		//			if (!IsConnected) {
		//				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
		//			}

		//			if (m_sslStream == null) {
		//				throw new InvalidOperationException("SSL Encryption has not been enabled on this stream.");
		//			}

		//			m_sslStream.Close();
		//			m_sslStream = null;
		//		}
		//#endif

		/// <summary>
		/// Instructs this stream to listen for connections on the specified address and port
		/// </summary>
		/// <param name="address">The address to listen on</param>
		/// <param name="port">The port to listen on</param>
		public void Listen(IPAddress address, int port) {
			if (!IsConnected) {
				if (m_socket == null) {
					m_socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				}

				m_socket.Bind(new IPEndPoint(address, port));
				m_socket.Listen(1);
			}
		}

		/// <summary>
		/// Accepts a connection from a listening socket
		/// </summary>
		public void Accept() {
			if (m_socket != null) {
				var socketSave = m_socket;
				m_socket = m_socket.Accept();
				socketSave.Close();
			}
		}

#if NETFRAMEWORK
		/// <summary>
		/// Asynchronously accepts a connection from a listening socket
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public IAsyncResult BeginAccept(AsyncCallback callback, object state) {
			if (m_socket != null) {
				return m_socket.BeginAccept(callback, state);
			}

			return null;
		}

		/// <summary>
		/// Completes a BeginAccept() operation
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginAccept</param>
		public void EndAccept(IAsyncResult ar) {
			if (m_socket != null) {
				var socketSave = m_socket;
				m_socket = m_socket.EndAccept(ar);
				socketSave.Close();
				m_netStream = new NetworkStream(m_socket);
				m_netStream.ReadTimeout = m_readTimeout;
			}
		}

		/// <summary>
		/// Accepts a connection from a listening socket
		/// </summary>
		public async Task AcceptAsync() {
			if (m_socket != null) {
				var iar = m_socket.BeginAccept(null, null);
				await Task.Factory.FromAsync(iar, m_socket.EndAccept);
			}
		}
#endif

#if !NETFRAMEWORK
		/// <summary>
		/// Accepts a connection from a listening socket
		/// </summary>
		public async Task AcceptAsync(CancellationToken token = default) {
			if (m_socket != null) {
				var socketSave = m_socket;
#if NET6_0_OR_GREATER
				m_socket = await m_socket.AcceptAsync(token);
#else
				m_socket = await m_socket.AcceptAsync();
#endif
				socketSave.Close();
#if NETSTANDARD || NET5_0_OR_GREATER
				m_netStream = new NetworkStream(m_socket);
				m_netStream.ReadTimeout = m_readTimeout;
#endif
			}
		}

#endif
		private void BindSocketToLocalIp() {
			if (Client.Config.SocketLocalIp != null) {

				var localPort = LocalPorts.GetRandomAvailable(Client.Config.SocketLocalIp);
				var localEndpoint = new IPEndPoint(Client.Config.SocketLocalIp, localPort);

#if DEBUG
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, $"Will now bind to {localEndpoint}");
#endif

				this.m_socket.Bind(localEndpoint);
			}
		}

#if !NETFRAMEWORK

		internal SocketAsyncEventArgs BeginAccept() {
			var args = new SocketAsyncEventArgs();
			var connectEvent = new ManualResetEvent(false);
			args.UserToken = connectEvent;
			args.Completed += (s, e) => { connectEvent.Set(); };
			if (!m_socket.AcceptAsync(args)) {
				CheckResult(args);
				return null;
			}

			return args;
		}

		internal void EndAccept(SocketAsyncEventArgs args, int timeout) {
			if (args == null) {
				return;
			}

			var connectEvent = (ManualResetEvent)args.UserToken;
			if (!connectEvent.WaitOne(timeout)) {
				Close();
				throw new TimeoutException("Timed out waiting for the server to connect to the active data socket.");
			}

			CheckResult(args);
		}

		private void CheckResult(SocketAsyncEventArgs args) {
			if (args.SocketError != SocketError.Success) {
				throw new SocketException((int)args.SocketError);
			}

			var socketSave = m_socket;
			m_socket = args.AcceptSocket;
			socketSave.Close();
			m_netStream = new NetworkStream(args.AcceptSocket);
			m_netStream.ReadTimeout = m_readTimeout;
		}

#endif

		//
		// CLOSE and DISPOSE logic
		//
		// To be called from Sync:
		// Close()
		// Dispose()
		//
		// To be called from Async:
		// CloseAsync()
		// DisposeAsync()
		//

		//
		// S Y N C
		//

		/// <summary>
		/// Disconnects from server
		/// </summary>
		public override void Close() {
			if (Client is AsyncFtpClient) {
				// If this is called from an AsyncFtpClient, it is an error and needs a code change in the
				// caller, to "await CloseAsync()".
				// One of the biggest cuplrits would be "stream.IsConnected" above.
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "AsyncFtpClient incorrectly called Close(sync)");
			}
			Dispose();
		}

		/// <summary>
		/// Disconnects from server
		/// </summary>
		protected new void Dispose() {
			if (IsControlConnection) {
				if (Client.Status.NoopDaemonEnable) {
					Client.Status.NoopDaemonEnable = false;
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "NoopDaemon disabled");
				}
			}
			else {
				Client.Status.NoopDaemonCmdMode = true;
			}

			ConnectionState = FtpConnectionState.Disconnected;

			string connText = IsControlConnection ? "control" : "data";
			string reduText = this.IsDisposed ? " (redundant)" : string.Empty;

			if (Client != null) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Disposing(sync) " + Client.ClientType + ".FtpSocketStream(" + connText + ")" + reduText);
			}

			// TODO: To support the CCC (Deactivate Encryption) command, some more additional logic
			// is required and note that CustomStream GnuTLS currently does not support this at all.

			if (m_sslStream != null) {          // Connection was a standard .NET SslStream (actually FtpSslStream)
				DisposeSslStream();
				DisposeNetStream();
			}
			else if (m_customStream != null) {  // Connection was a custom Stream (typically: FluentFTP.GnuTLS)
				DisposeCustomStream();
			}
			else {                              // Connection was a standard .NET NetworkStream (unencrypted)
				DisposeNetStream();
			}

			DisposeSocket();

			base.Dispose(true);

			m_sslStream = null;
			m_customStream = null;
			m_netStream = null;
			m_socket = null;

			IsDisposed = true;
		}

		internal void DisposeSslStream() {
			// Note: FtpSslStream SSL shutdown gets called here ( Dispose() calls Close() )
			//		 This sends the Ssl Alert "Notify Close" and causes an orderly Ssl shutdown.
			//		 This send will fail in those cases where a connection is lost and not shutdown.
			try {
				m_sslStream.Dispose();
			}
			catch /*(Exception ex)*/ {
				// ((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught SYNC SSLSTREAM(Dispose) exception: " + ex.Message);
			};
		}

		internal void DisposeCustomStream() {
			try {
				m_customStream.Dispose();
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught SYNC CUSTOMSTREAM(Dispose) exception: " + ex.Message);
			};
		}

		internal void DisposeNetStream() {
			try {
				m_netStream?.Dispose();
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught SYNC NETSTREAM(Dispose) exception: " + ex.Message);
			};

		}

		internal void DisposeSocket() {
			try {
				m_socket?.Dispose();
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught SYNC SOCKET(Dispose) exception: " + ex.Message);
			};
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

		//
		// A S Y N C
		//

		/// <summary>
		/// CloseAsync, disconnects from server
		/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		public virtual async ValueTask CloseAsync(CancellationToken token = default(CancellationToken)) {
			await DisposeAsync();
		}
#else
		public virtual async Task CloseAsync(CancellationToken token = default(CancellationToken)) {
			await DisposeAsync();
		}
#endif

		/// <summary>
		/// DisposeAsync, disconnects from server
		/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		public override async ValueTask DisposeAsync() {
			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
		}
#else
		public async Task DisposeAsync() {
			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
		}
#endif

		/// <summary>
		/// Disconnects from server, actually
		/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		protected async ValueTask DisposeAsyncCore() {
#else
		protected async Task DisposeAsyncCore() {
#endif
			if (IsControlConnection) {
				if (Client.Status.NoopDaemonEnable) {
					Client.Status.NoopDaemonEnable = false;
					((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "NoopDaemon disabled");
				}
			}
			else {
				Client.Status.NoopDaemonCmdMode = true;
			}

			ConnectionState = FtpConnectionState.Disconnected;

			string connText = this.IsControlConnection ? "control" : "data";
			string reduText = this.IsDisposed ? " (redundant)" : string.Empty;

			if (Client != null) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Disposing(async) " + Client.ClientType + ".FtpSocketStream(" + connText + ")" + reduText);
			}

			// TODO: To support the CCC (Deactivate Encryption) command, some more additional logic
			// is required and note that CustomStream GnuTLS currently does not support this at all.

			if (m_sslStream != null) {             // Connection was a standard .NET SslStream (actually FtpSslStream)
				await DisposeSslStreamAsync();
				await DisposeNetStreamAsync();
			}
			else if (m_customStream != null) {      // Connection was a custom Stream (typically: FluentFTP.GnuTLS)
				await DisposeCustomStreamAsync();
			}
			else {
				await DisposeNetStreamAsync();  // Connection was a standard .NET NetworkStream (unencrypted)
			}

			await DisposeSocketAsync();

			base.Dispose(true);

			m_sslStream = null;
			m_customStream = null;
			m_netStream = null;
			m_socket = null;

			IsDisposed = true;
		}

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		internal async ValueTask DisposeSslStreamAsync() {
			// Note: FtpSslStream SSL shutdown gets called here ( in the Close() )
			//		 This sends the Ssl Alert "Notify Close" and causes an orderly Ssl shutdown
			try {
				m_sslStream.Close();   // Async Close override in FtpSslStream not supported yet
			}
			catch /*(Exception ex)*/ {
				// ((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC SSLSTREAM(Close) dispose exception: " + ex.Message);
			};
			try {
				await m_sslStream.DisposeAsync();
			}
			catch /*(Exception ex)*/ {
				// ((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC SSLSTREAM(Dispose) dispose exception: " + ex.Message);
			};
#else
		internal async Task DisposeSslStreamAsync() {
			// Note: FtpSslStream SSL shutdown gets called here ( Dispose() calls Close() )
			//		 This sends the Ssl Alert "Notify Close" and causes an orderly Ssl shutdown
			try {
				m_sslStream.Dispose(); // Async dispose not supported in this .NET?
			}
			catch /*(Exception ex)*/ {
				// ((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC SSLSTREAM(Dispose) exception: " + ex.Message);
			};
#endif
		}

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		internal async ValueTask DisposeCustomStreamAsync() {
			try {
				m_customStream.Dispose();
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC CUSTOMSTREAM(Dispose) exception: " + ex.Message);
			};
#else
		internal async Task DisposeCustomStreamAsync() {
			try {
				m_customStream.Dispose();
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC CUSTOMSTREAM(Dispose) exception: " + ex.Message);
			};
#endif
		}

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		internal async ValueTask DisposeNetStreamAsync() {
			if (m_netStream == null) {
				return;
			}
			try {
				await m_netStream.DisposeAsync();
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC NETSTREAM(Dispose) exception: " + ex.Message);
			};
#else
		internal async Task DisposeNetStreamAsync() {
			if (m_netStream == null) {
				return;
			}
			try {
				m_netStream.Dispose(); // Async dispose not supported in this .NET?
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC NETSTREAM(Dispose) exception: " + ex.Message);
			};
#endif
		}

		/// <summary>
		/// Safely close the socket if its open
		/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		internal async ValueTask DisposeSocketAsync() {
#else
		internal async Task DisposeSocketAsync() {
#endif
			if (m_socket == null) {
				return;
			}
			try {
				m_socket.Dispose();    // Async dispose not supported in this .NET?
			}
			catch (Exception ex) {
				((IInternalFtpClient)Client).LogStatus(FtpTraceLevel.Verbose, "Caught ASYNC SOCKET(Dispose) exception: " + ex.Message);
			};
		}

	}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

}
