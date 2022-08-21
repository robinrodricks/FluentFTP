using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

using FluentFTP.Helpers;
using FluentFTP.Exceptions;
#if CORE || NET45
using System.Threading.Tasks;
#endif

namespace FluentFTP {


	/// <summary>
	/// Stream class used for talking. Used by FtpClient, extended by FtpDataStream
	/// </summary>
	public class FtpSocketStream : Stream, IDisposable {
		public readonly FtpClient Client;

		public FtpSocketStream(FtpClient conn) {
			Client = conn;
		}

		/// <summary>
		/// Used for tacking read/write activity on the socket
		/// to determine if Poll() should be used to test for
		/// socket connectivity. The socket in this class will
		/// not know it has been disconnected if the remote host
		/// closes the connection first. Using Poll() avoids 
		/// the exception that would be thrown when trying to
		/// read or write to the disconnected socket.
		/// </summary>
		private DateTime m_lastActivity = DateTime.Now;

		private Socket m_socket = null;

		/// <summary>
		/// The socket used for talking
		/// </summary>
		protected Socket Socket {
			get => m_socket;
			private set => m_socket = value;
		}

		private int m_socketPollInterval = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds
		/// that must pass since the last socket activity
		/// before calling Poll() on the socket to test for
		/// connectivity. Setting this interval too low will
		/// have a negative impact on performance. Setting this
		/// interval to 0 disables Poll()'ing all together.
		/// The default value is 15 seconds.
		/// </summary>
		public int SocketPollInterval {
			get => m_socketPollInterval;
			set => m_socketPollInterval = value;
		}

		/// <summary>
		/// Gets the number of available bytes on the socket, 0 if the
		/// socket has not been initialized. This property is used internally
		/// by FtpClient in an effort to detect disconnections and gracefully
		/// reconnect the control connection.
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
		/// Gets a value indicating if this socket stream is connected
		/// </summary>
		public bool IsConnected {
			get {
				try {
					if (m_socket == null) {
						return false;
					}

					if (!m_socket.Connected) {
						Close();
						return false;
					}

					if (!CanRead || !CanWrite) {
						Close();
						return false;
					}

					if (m_socketPollInterval > 0 && DateTime.Now.Subtract(m_lastActivity).TotalMilliseconds > m_socketPollInterval) {
						Client.LogStatus(FtpTraceLevel.Verbose, "Testing connectivity using Socket.Poll()...");

						// FIX : #273 update m_lastActivity to the current time
						m_lastActivity = DateTime.Now;

						if (m_socket.Poll(500000, SelectMode.SelectRead) && m_socket.Available == 0) {
							Close();
							return false;
						}
					}
				}
				catch (SocketException sockex) {
					Close();
					Client.LogStatus(FtpTraceLevel.Warn, "FtpSocketStream.IsConnected: Caught and discarded SocketException while testing for connectivity: " + sockex.ToString());
					return false;
				}
				catch (IOException ioex) {
					Close();
					Client.LogStatus(FtpTraceLevel.Warn, "FtpSocketStream.IsConnected: Caught and discarded IOException while testing for connectivity: " + ioex.ToString());
					return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Gets a value indicating if encryption is being used
		/// </summary>
		public bool IsEncrypted {
			get {
#if NO_SSL
				return false;
#else
				return m_sslStream != null;
#endif
			}
		}
		
		/// <summary>
		/// The negotiated SSL/TLS protocol version. Will have a valid value after connection is complete.
		/// </summary>
		public SslProtocols SslProtocolActive {
			get {
#if NO_SSL
				return SslProtocols.None;
#else
				return IsEncrypted ? m_sslStream.SslProtocol : SslProtocols.None;
#endif
			}
		}

		private NetworkStream m_netStream = null;

		/// <summary>
		/// The non-encrypted stream
		/// </summary>
		private NetworkStream NetworkStream {
			get => m_netStream;
			set => m_netStream = value;
		}

#if !NO_SSL
		private BufferedStream m_bufStream = null;

		private SslStream m_sslStream = null;

		/// <summary>
		/// The encrypted stream
		/// </summary>
		private SslStream SslStream {
			get => m_sslStream;
			set => m_sslStream = value;
		}
#endif

		/// <summary>
		/// Gets the underlying stream, could be a NetworkStream or SslStream
		/// </summary>
		protected Stream BaseStream {
			get {
#if NO_SSL
				if (m_netStream != null) {
					return m_netStream;
				}

#else
				if (m_sslStream != null) {
					return m_sslStream;
				}
				else if (m_netStream != null) {
					return m_netStream;
				}
#endif

				return null;
			}
		}

		/// <summary>
		/// Gets a value indicating if this stream can be read
		/// </summary>
		public override bool CanRead {
			get {
				if (m_netStream != null) {
					return m_netStream.CanRead;
				}

				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating if this stream if seekable
		/// </summary>
		public override bool CanSeek => false;

		/// <summary>
		/// Gets a value indicating if this stream can be written to
		/// </summary>
		public override bool CanWrite {
			get {
				if (m_netStream != null) {
					return m_netStream.CanWrite;
				}

				return false;
			}
		}

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

		private event FtpSocketStreamSslValidation m_sslvalidate = null;

		/// <summary>
		/// Event is fired when a SSL certificate needs to be validated
		/// </summary>
		public event FtpSocketStreamSslValidation ValidateCertificate {
			add => m_sslvalidate += value;
			remove => m_sslvalidate -= value;
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

		private int m_connectTimeout = 30000;

		/// <summary>
		/// Gets or sets the length of time milliseconds to wait
		/// for a connection succeed before giving up. The default
		/// is 30000 (30 seconds).
		/// </summary>
		public int ConnectTimeout {
			get => m_connectTimeout;
			set => m_connectTimeout = value;
		}

		/// <summary>
		/// Gets the local end point of the socket
		/// </summary>
		public IPEndPoint LocalEndPoint {
			get {
				if (m_socket == null) {
					return null;
				}

				return (IPEndPoint) m_socket.LocalEndPoint;
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

				return (IPEndPoint) m_socket.RemoteEndPoint;
			}
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
			if (!IsConnected) {
				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
			}

			if (BaseStream == null) {
				throw new InvalidOperationException("The base stream of the FtpSocketStream object is null.");
			}

			BaseStream.Flush();
		}

#if ASYNC

		/// <summary>
		/// Flushes the stream asynchronously
		/// </summary>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public override async Task FlushAsync(CancellationToken token) {
			if (!IsConnected) {
				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
			}

			if (BaseStream == null) {
				throw new InvalidOperationException("The base stream of the FtpSocketStream object is null.");
			}

			await BaseStream.FlushAsync(token);
		}

#endif

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

#if ASYNC
		internal async Task EnableCancellation(Task task, CancellationToken token, Action action) {
			var registration = token.Register(action);
			_ = task.ContinueWith(x => registration.Dispose(), CancellationToken.None);
			await task;
		}

		internal async Task<T> EnableCancellation<T>(Task<T> task, CancellationToken token, Action action) {
			var registration = token.Register(action);
			_ = task.ContinueWith(x => registration.Dispose(), CancellationToken.None);
			return await task;
		}

#endif

#if NET45
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
					() => CloseSocket()
				);
			}

			return read;
		}

#endif

#if ASYNC && !NET45
		/// <summary>
		/// Bypass the stream and read directly off the socket.
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <returns>The number of bytes read</returns>
		internal async Task<int> RawSocketReadAsync(byte[] buffer, CancellationToken token) {
			var read = 0;

			if (m_socket != null && m_socket.Connected && !token.IsCancellationRequested) {
				read = await m_socket.ReceiveAsync(new ArraySegment<byte>(buffer), 0);
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
#if !CORE
			IAsyncResult ar = null;
#endif

			if (BaseStream == null) {
				return 0;
			}

			m_lastActivity = DateTime.Now;
#if CORE
			return BaseStream.Read(buffer, offset, count);
#else
			ar = BaseStream.BeginRead(buffer, offset, count, null, null);
			bool success = ar.AsyncWaitHandle.WaitOne(m_readTimeout, true);
			ar.AsyncWaitHandle.Close();
			if (!success) {
				Close();
				throw new TimeoutException("Timed out trying to read data from the socket stream!");
			}

			return BaseStream.EndRead(ar);
#endif
		}

#if ASYNC

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

			m_lastActivity = DateTime.Now;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token)) {
				cts.CancelAfter(ReadTimeout);
				cts.Token.Register(() => Close());
				try {
					var res = await BaseStream.ReadAsync(buffer, offset, count, cts.Token);
					return res;
				}
				catch {
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
				if ((char) buf[0] == '\n') {
					line = encoding.GetString(data.ToArray()).Trim('\r', '\n');
					break;
				}
			}

			return line;
		}

		/// <summary>
		/// Reads all line from the socket
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

				var separatorIdx = Array.IndexOf(buf, (byte) '\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array readed

				while (separatorIdx >= 0) // at least one '\n' returned
				{
					while (firstByteToReadIdx <= separatorIdx) {
						data.Add(buf[firstByteToReadIdx++]);
					}

					var line = encoding.GetString(data.ToArray()).Trim('\r', '\n'); // convert data to string
					yield return line;
					data.Clear();

					separatorIdx = Array.IndexOf(buf, (byte) '\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array readed
				}

				while (firstByteToReadIdx < charRead) // add all remaining characters to data
				{
					data.Add(buf[firstByteToReadIdx++]);
				}
			}
		}

#if ASYNC
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
				if ((char) buf[0] == '\n') {
					line = encoding.GetString(data.ToArray()).Trim('\r', '\n');
					break;
				}
			}

			return line;
		}

		/// <summary>
		/// Reads all line from the socket
		/// </summary>
		/// <param name="encoding">The type of encoding used to convert from byte[] to string</param>
		/// <param name="bufferSize">The size of the buffer</param>
		/// <returns>A list of lines from the stream</returns>
		public async Task<IEnumerable<string>> ReadAllLinesAsync(System.Text.Encoding encoding, int bufferSize, CancellationToken token) {
			int charRead;
			var data = new List<byte>();
			var lines = new List<string>();
			var buf = new byte[bufferSize];

			while ((charRead = await ReadAsync(buf, 0, buf.Length, token)) > 0) {
				var firstByteToReadIdx = 0;

				var separatorIdx = Array.IndexOf(buf, (byte) '\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array read

				while (separatorIdx >= 0) // at least one '\n' returned
				{
					while (firstByteToReadIdx <= separatorIdx) {
						data.Add(buf[firstByteToReadIdx++]);
					}

					var line = encoding.GetString(data.ToArray()).Trim('\r', '\n'); // convert data to string
					lines.Add(line);
					data.Clear();

					separatorIdx = Array.IndexOf(buf, (byte) '\n', firstByteToReadIdx, charRead - firstByteToReadIdx); //search in full byte array read
				}

				while (firstByteToReadIdx < charRead) // add all remaining characters to data
				{
					data.Add(buf[firstByteToReadIdx++]);
				}
			}

			return lines;
		}
#endif

		/// <summary>
		/// Writes data to the stream
		/// </summary>
		/// <param name="buffer">Buffer to write to stream</param>
		/// <param name="offset">Where in the buffer to start</param>
		/// <param name="count">Number of bytes to be read</param>
		public override void Write(byte[] buffer, int offset, int count) {
			if (BaseStream == null) {
				return;
			}

			BaseStream.Write(buffer, offset, count);
			m_lastActivity = DateTime.Now;
		}

#if ASYNC
		/// <summary>
		/// Writes data to the stream asynchronously
		/// </summary>
		/// <param name="buffer">Buffer to write to stream</param>
		/// <param name="offset">Where in the buffer to start</param>
		/// <param name="count">Number of bytes to be read</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) {
			if (BaseStream == null) {
				return;
			}

			await BaseStream.WriteAsync(buffer, offset, count, token);
			m_lastActivity = DateTime.Now;
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

#if ASYNC
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
#endif

#if CORE
		/// <summary>
		/// Disconnects from server
		/// </summary>
		public virtual void Close() {
			Dispose(true);
		}
#endif

		/// <summary>
		/// Disconnects from server
		/// </summary>
		protected override void Dispose(bool disposing) {
			// Fix: Hard catch and suppress all exceptions during disposing as there are constant issues with this method
			try {
				// ensure null exceptions don't occur here
				if (Client != null) {
					Client.LogStatus(FtpTraceLevel.Verbose, "Disposing FtpSocketStream...");
				}
			}
			catch (Exception) {
			}

#if !NO_SSL
			if (m_bufStream != null) {
				try {
					// ensure the last of the buffered bytes are flushed
					// before we close the socket and network stream
					m_bufStream.Flush();
				}
				catch (Exception ex) {
				}

				m_bufStream = null;
			}
#endif

			CloseSocket();

			if (m_netStream != null) {
				try {
					m_netStream.Dispose();
				}
				catch (Exception ex) {
				}

				m_netStream = null;
			}

#if !NO_SSL
			if (m_sslStream != null) {
				try {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
					if (Client != null && Client.DisconnectWithShutdown) {
						m_sslStream.ShutdownAsync().RunSynchronously();
					}
#endif
					m_sslStream.Dispose();
				}
				catch (Exception ex) {
				}

				m_sslStream = null;
			}

#endif
		}

		/// <summary>
		/// Safely close the socket if its open
		/// </summary>
		internal void CloseSocket() {
			if (m_socket != null) {
				try {
#if CORE
#if NET5_0_OR_GREATER
					if (Client != null && Client.DisconnectWithShutdown) {
						m_socket.Shutdown(SocketShutdown.Send);
					}
#endif
					m_socket.Dispose();
#else
					if (m_socket.Connected) {
						if (Client != null && Client.DisconnectWithShutdown) {
							m_socket.Shutdown(SocketShutdown.Send);
						}
						m_socket.Close();
					}
#endif

#if !NET20 && !NET35 && !CORE
					m_socket.Dispose();
#endif
				}
				catch (Exception ex) {
				}

				m_socket = null;
			}

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
		/// Connect to the specified host
		/// </summary>
		/// <param name="host">The host to connect to</param>
		/// <param name="port">The port to connect to</param>
		/// <param name="ipVersions">Internet Protocol versions to support during the connection phase</param>
		public void Connect(string host, int port, FtpIpVersion ipVersions) {
#if CORE
			IPAddress[] addresses = Dns.GetHostAddressesAsync(host).Result;
#else
			IAsyncResult ar = null;
			var addresses = Dns.GetHostAddresses(host);
#endif

			if (ipVersions == 0) {
				throw new ArgumentException("The ipVersions parameter must contain at least 1 flag.");
			}

			for (var i = 0; i < addresses.Length; i++) {

				// we don't need to do this check unless
				// a particular version of IP has been
				// omitted so we won't.
				if (ipVersions != FtpIpVersion.ANY) {
					switch (addresses[i].AddressFamily) {
						case AddressFamily.InterNetwork:
							if ((ipVersions & FtpIpVersion.IPv4) != FtpIpVersion.IPv4) {
#if DEBUG
								Client.LogStatus(FtpTraceLevel.Verbose, "Skipped IPV4 address : " + addresses[i].ToString());
#endif
								continue;
							}

							break;

						case AddressFamily.InterNetworkV6:
							if ((ipVersions & FtpIpVersion.IPv6) != FtpIpVersion.IPv6) {
#if DEBUG
								Client.LogStatus(FtpTraceLevel.Verbose, "Skipped IPV6 address : " + addresses[i].ToString());
#endif
								continue;
							}

							break;
					}
				}

				if (FtpTrace.LogIP) {
					Client.LogStatus(FtpTraceLevel.Info, "Connecting to " + addresses[i].ToString() + ":" + port);
				}
				else {
					Client.LogStatus(FtpTraceLevel.Info, "Connecting to ***:" + port);
				}

				m_socket = new Socket(addresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				BindSocketToLocalIp();

#if CORE


				var args = new SocketAsyncEventArgs {
					RemoteEndPoint = new IPEndPoint(addresses[i], port)
				};
				var connectEvent = new ManualResetEvent(false);
				args.Completed += (s, e) => { connectEvent.Set(); };

				if (m_socket.ConnectAsync(args)) {
					if (!connectEvent.WaitOne(m_connectTimeout)) {
						Close();
						throw new TimeoutException("Timed out trying to connect!");
					}
				}

				if (args.SocketError != SocketError.Success) {
					throw new SocketException((int)args.SocketError);
				}

				// only try the first address
				break;
#else
				ar = m_socket.BeginConnect(addresses[i], port, null, null);
				bool success = ar.AsyncWaitHandle.WaitOne(m_connectTimeout, true);
				ar.AsyncWaitHandle.Close();
				if (!success) {
					Close();

					// check to see if we're out of addresses, and throw a TimeoutException
					if (i + 1 == addresses.Length) {
						throw new TimeoutException("Timed out trying to connect!");
					}
				}
				else {
					m_socket.EndConnect(ar);

					// we got a connection, break out
					// of the loop.
					break;
				}

#endif
			}

			// make sure that we actually connected to
			// one of the addresses returned from GetHostAddresses()
			if (m_socket == null || !m_socket.Connected) {
				Close();
				throw new IOException("Failed to connect to host.");
			}

			m_netStream = new NetworkStream(m_socket);
			m_netStream.ReadTimeout = m_readTimeout;
			m_lastActivity = DateTime.Now;
		}

#if ASYNC
		/// <summary>
		/// Connect to the specified host
		/// </summary>
		/// <param name="host">The host to connect to</param>
		/// <param name="port">The port to connect to</param>
		/// <param name="ipVersions">Internet Protocol versions to support during the connection phase</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task ConnectAsync(string host, int port, FtpIpVersion ipVersions, CancellationToken token) {
			IPAddress[] addresses = await Dns.GetHostAddressesAsync(host);

			if (ipVersions == 0) {
				throw new ArgumentException("The ipVersions parameter must contain at least 1 flag.");
			}

			for (var i = 0; i < addresses.Length; i++) {
				// we don't need to do this check unless
				// a particular version of IP has been
				// omitted so we won't.
				if (ipVersions != FtpIpVersion.ANY) {
					switch (addresses[i].AddressFamily) {
						case AddressFamily.InterNetwork:
							if ((ipVersions & FtpIpVersion.IPv4) != FtpIpVersion.IPv4) {
#if DEBUG
								Client.LogStatus(FtpTraceLevel.Verbose, "Skipped IPV4 address : " + addresses[i].ToString());
#endif
								continue;
							}

							break;

						case AddressFamily.InterNetworkV6:
							if ((ipVersions & FtpIpVersion.IPv6) != FtpIpVersion.IPv6) {
#if DEBUG
								Client.LogStatus(FtpTraceLevel.Verbose, "Skipped IPV6 address : " + addresses[i].ToString());
#endif
								continue;
							}

							break;
					}
				}

				if (FtpTrace.LogIP) {
					Client.LogStatus(FtpTraceLevel.Info, "Connecting to " + addresses[i].ToString() + ":" + port);
				}
				else {
					Client.LogStatus(FtpTraceLevel.Info, "Connecting to ***:" + port);
				}

				m_socket = new Socket(addresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				BindSocketToLocalIp();
#if CORE
				try {
					if (this.ConnectTimeout > 0) {
						using (var timeoutSrc = CancellationTokenSource.CreateLinkedTokenSource(token)) {
							timeoutSrc.CancelAfter(this.ConnectTimeout);
							await EnableCancellation(m_socket.ConnectAsync(addresses[i], port), timeoutSrc.Token, () => CloseSocket());
							break;
						}
					}
					else {
						await EnableCancellation(m_socket.ConnectAsync(addresses[i], port), token, () => CloseSocket());
						break;
					}
				}
				catch (SocketException ex) {
					// FIX #869: catch "The I/O operation has been aborted because of either a thread exit or an application request."
					// and continue with next address or throw timeout exception if this is the last address available
#if NET50_OR_LATER
					if (ex.ErrorCode == 995 && ex.SocketErrorCode == SocketError.OperationAborted) {
#else
					if (ex.Message.StartsWith("The I/O operation has been aborted because") && ex.SocketErrorCode == SocketError.OperationAborted) {
#endif
						throw new TimeoutException("Timed out trying to connect!");
					}
					else {
						throw;
					}
				}
#else
				var connectResult = m_socket.BeginConnect(addresses[i], port, null, null);
				await EnableCancellation(Task.Factory.FromAsync(connectResult, m_socket.EndConnect), token, () => CloseSocket());
				break;
#endif
					}

			// make sure that we actually connected to
			// one of the addresses returned from GetHostAddresses()
			if (m_socket == null || !m_socket.Connected) {
				Close();
				throw new IOException("Failed to connect to host.");
			}

			m_netStream = new NetworkStream(m_socket);
			m_netStream.ReadTimeout = m_readTimeout;
			m_lastActivity = DateTime.Now;
		}
#endif

#if !NO_SSL
		/// <summary>
		/// Activates SSL on this stream using default protocols. Fires the ValidateCertificate event. 
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will 
		/// not be accepted.
		/// </summary>
		/// <param name="targethost">The host to authenticate the certificate against</param>
		public void ActivateEncryption(string targethost) {
			ActivateEncryption(targethost, null, Client.SslProtocols);
		}

#if ASYNC
		/// <summary>
		/// Activates SSL on this stream using default protocols. Fires the ValidateCertificate event. 
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will 
		/// not be accepted.
		/// </summary>
		/// <param name="targethost">The host to authenticate the certificate against</param>
		public async Task ActivateEncryptionAsync(string targethost) {
			await ActivateEncryptionAsync(targethost, null, Client.SslProtocols);
		}
#endif

		/// <summary>
		/// Activates SSL on this stream using default protocols. Fires the ValidateCertificate event.
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will 
		/// not be accepted.
		/// </summary>
		/// <param name="targethost">The host to authenticate the certificate against</param>
		/// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
		public void ActivateEncryption(string targethost, X509CertificateCollection clientCerts) {
			ActivateEncryption(targethost, clientCerts, Client.SslProtocols);
		}

#if ASYNC
		/// <summary>
		/// Activates SSL on this stream using default protocols. Fires the ValidateCertificate event.
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will 
		/// not be accepted.
		/// </summary>
		/// <param name="targethost">The host to authenticate the certificate against</param>
		/// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
		public async Task ActivateEncryptionAsync(string targethost, X509CertificateCollection clientCerts) {
			await ActivateEncryptionAsync(targethost, clientCerts, Client.SslProtocols);
		}
#endif

		/// <summary>
		/// Activates SSL on this stream using the specified protocols. Fires the ValidateCertificate event.
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will 
		/// not be accepted.
		/// </summary>
		/// <param name="targethost">The host to authenticate the certificate against</param>
		/// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
		/// <param name="sslProtocols">A bitwise parameter for supported encryption protocols.</param>
		/// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
		public void ActivateEncryption(string targethost, X509CertificateCollection clientCerts, SslProtocols sslProtocols) {
			if (!IsConnected) {
				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
			}

			if (m_netStream == null) {
				throw new InvalidOperationException("The base network stream is null.");
			}

			if (m_sslStream != null) {
				throw new InvalidOperationException("SSL Encryption has already been enabled on this stream.");
			}

			try {
				DateTime auth_start;
				TimeSpan auth_time_total;

				CreateBufferStream();

#if CORE
				m_sslStream = new SslStream(GetBufferStream(), true, new RemoteCertificateValidationCallback(
					delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return OnValidateCertificate(certificate, chain, sslPolicyErrors); }
				));
#else
				m_sslStream = new FtpSslStream(GetBufferStream(), true, new RemoteCertificateValidationCallback(
					delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return OnValidateCertificate(certificate, chain, sslPolicyErrors); }
				));
#endif

				auth_start = DateTime.Now;
				try {
#if CORE
					m_sslStream.AuthenticateAsClientAsync(targethost, clientCerts, sslProtocols, Client.ValidateCertificateRevocation).Wait();
#else
					m_sslStream.AuthenticateAsClient(targethost, clientCerts, sslProtocols, Client.ValidateCertificateRevocation);
#endif
				}
				catch (IOException ex) {
					if (ex.InnerException is Win32Exception) {
						var win32Exception = (Win32Exception) ex.InnerException;
						if (win32Exception.NativeErrorCode == 10053) {
							throw new FtpMissingSocketException(ex);
						}
					}

					throw;
				}

				auth_time_total = DateTime.Now.Subtract(auth_start);
				Client.LogStatus(FtpTraceLevel.Info, "FTPS Authentication Successful");
				Client.LogStatus(FtpTraceLevel.Verbose, "Time to activate encryption: " + auth_time_total.Hours + "h " + auth_time_total.Minutes + "m " + auth_time_total.Seconds + "s.  Total Seconds: " + auth_time_total.TotalSeconds + ".");
			}
			catch (AuthenticationException) {
				// authentication failed and in addition it left our 
				// ssl stream in an unusable state so cleanup needs
				// to be done and the exception can be re-thrown for
				// handling down the chain. (Add logging?)
				Close();
				Client.LogStatus(FtpTraceLevel.Error, "FTPS Authentication Failed");
				throw;
			}
		}

		/// <summary>
		/// Conditionally create a SSL BufferStream based on the configuration in FtpClient.SslBuffering.
		/// </summary>
		private void CreateBufferStream() {
			// Fix: SSL BufferStream is automatically disabled when using FTP proxies, and enabled in all other cases
			// Fix: SSL Buffering is disabled on .NET 5.0 and later due to issues in .NET framework - See #682
#if NET50_OR_LATER
				m_bufStream = null;
#else
			if (Client.SslBuffering == FtpsBuffering.On ||
			    Client.SslBuffering == FtpsBuffering.Auto && !Client.IsProxy()) {
				m_bufStream = new BufferedStream(NetworkStream, 81920);
			}
			else {
				m_bufStream = null;
			}
#endif
		}

		/// <summary>
		/// If SSL Buffering is enabled it returns the BufferStream, else returns the internal NetworkStream.
		/// </summary>
		/// <returns></returns>
		private Stream GetBufferStream() {
			return m_bufStream != null ? (Stream)m_bufStream : (Stream)NetworkStream;
		}

#if ASYNC
		/// <summary>
		/// Activates SSL on this stream using the specified protocols. Fires the ValidateCertificate event.
		/// If this event is not handled and there are SslPolicyErrors present, the certificate will 
		/// not be accepted.
		/// </summary>
		/// <param name="targethost">The host to authenticate the certificate against</param>
		/// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
		/// <param name="sslProtocols">A bitwise parameter for supported encryption protocols.</param>
		/// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
		public async Task ActivateEncryptionAsync(string targethost, X509CertificateCollection clientCerts, SslProtocols sslProtocols) {
			if (!IsConnected) {
				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
			}

			if (m_netStream == null) {
				throw new InvalidOperationException("The base network stream is null.");
			}

			if (m_sslStream != null) {
				throw new InvalidOperationException("SSL Encryption has already been enabled on this stream.");
			}

			try {
				DateTime auth_start;
				TimeSpan auth_time_total;

				CreateBufferStream();

#if CORE
				m_sslStream = new SslStream(GetBufferStream(), true, new RemoteCertificateValidationCallback(
					delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return OnValidateCertificate(certificate, chain, sslPolicyErrors); }
				));
#else
				m_sslStream = new FtpSslStream(GetBufferStream(), true, new RemoteCertificateValidationCallback(
					delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return OnValidateCertificate(certificate, chain, sslPolicyErrors); }
				));
#endif

				auth_start = DateTime.Now;
				try {
					await m_sslStream.AuthenticateAsClientAsync(targethost, clientCerts, sslProtocols, Client.ValidateCertificateRevocation);
				}
				catch (IOException ex) {
					if (ex.InnerException is Win32Exception) {
						var win32Exception = (Win32Exception) ex.InnerException;
						if (win32Exception.NativeErrorCode == 10053) {
							throw new FtpMissingSocketException(ex);
						}
					}

					throw;
				}

				auth_time_total = DateTime.Now.Subtract(auth_start);
				Client.LogStatus(FtpTraceLevel.Info, "FTPS Authentication Successful");
				Client.LogStatus(FtpTraceLevel.Verbose, "Time to activate encryption: " + auth_time_total.Hours + "h " + auth_time_total.Minutes + "m " + auth_time_total.Seconds + "s.  Total Seconds: " + auth_time_total.TotalSeconds + ".");
			}
			catch (AuthenticationException) {
				// authentication failed and in addition it left our 
				// ssl stream in an unusable state so cleanup needs
				// to be done and the exception can be re-thrown for
				// handling down the chain. (Add logging?)
				Close();
				Client.LogStatus(FtpTraceLevel.Error, "FTPS Authentication Failed");
				throw;
			}
		}
#endif

#endif

#if !CORE
			/// <summary>
			/// Deactivates SSL on this stream using the specified protocols and reverts back to plain-text FTP.
			/// </summary>
			public void DeactivateEncryption() {
			if (!IsConnected) {
				throw new InvalidOperationException("The FtpSocketStream object is not connected.");
			}

			if (m_sslStream == null) {
				throw new InvalidOperationException("SSL Encryption has not been enabled on this stream.");
			}

			m_sslStream.Close();
			m_sslStream = null;
		}
#endif

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
				m_socket = m_socket.Accept();
			}
		}

#if NET45
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

#if ASYNC && !NET45
		/// <summary>
		/// Accepts a connection from a listening socket
		/// </summary>
		public async Task AcceptAsync() {
			if (m_socket != null) {
				m_socket = await m_socket.AcceptAsync();
#if CORE
				m_netStream = new NetworkStream(m_socket);
				m_netStream.ReadTimeout = m_readTimeout;
#endif
			}
		}

#else
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
				m_socket = m_socket.EndAccept(ar);
				m_netStream = new NetworkStream(m_socket);
				m_netStream.ReadTimeout = m_readTimeout;
			}
		}
#endif
		private void BindSocketToLocalIp() {
#if ASYNC && !CORE14 && !CORE16
			if (Client.SocketLocalIp != null) {

				var localPort = LocalPorts.GetRandomAvailable(Client.SocketLocalIp);
				var localEndpoint = new IPEndPoint(Client.SocketLocalIp, localPort);

#if DEBUG
				Client.LogStatus(FtpTraceLevel.Verbose, $"Will now bind to {localEndpoint}");
#endif

				this.m_socket.Bind(localEndpoint);
			}
#endif
		}

#if CORE
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

			var connectEvent = (ManualResetEvent) args.UserToken;
			if (!connectEvent.WaitOne(timeout)) {
				Close();
				throw new TimeoutException("Timed out waiting for the server to connect to the active data socket.");
			}

			CheckResult(args);
		}

		private void CheckResult(SocketAsyncEventArgs args) {
			if (args.SocketError != SocketError.Success) {
				throw new SocketException((int) args.SocketError);
			}

			m_socket = args.AcceptSocket;
			m_netStream = new NetworkStream(args.AcceptSocket);
			m_netStream.ReadTimeout = m_readTimeout;
		}

#endif
	}
}
