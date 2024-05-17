using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.BaseClient;

namespace FluentFTP {
	/// <summary>
	/// Base class for data stream connections
	/// </summary>
	public class FtpDataStream : FtpSocketStream {
		private FtpReply m_commandStatus;

		/// <summary>
		/// Gets the status of the command that was used to open
		/// this data channel
		/// </summary>
		public FtpReply CommandStatus {
			get => m_commandStatus;
			set => m_commandStatus = value;
		}

		private BaseFtpClient m_control = null;

		/// <summary>
		/// Gets or sets the control connection for this data stream. Setting
		/// the control connection causes the object to be cloned and a new
		/// connection is made to the server to carry out the task. This ensures
		/// that multiple streams can be opened simultaneously.
		/// </summary>
		public BaseFtpClient ControlConnection {
			get => m_control;
			set => m_control = value;
		}

		private long m_length = 0;

		/// <summary>
		/// Gets or sets the length of the stream. Only valid for file transfers
		/// and only valid on servers that support the Size command.
		/// </summary>
		public override long Length => m_length;

		private long m_position = 0;

		/// <summary>
		/// Gets or sets the position of the stream
		/// </summary>
		public override long Position {
			get => m_position;
			set => throw new InvalidOperationException("You cannot modify the position of a FtpDataStream. This property is updated as data is read or written to the stream.");
		}

		/// <summary>
		/// Reads data off the stream
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <param name="offset">Where to start in the buffer</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns>The number of bytes read</returns>
		public override int Read(byte[] buffer, int offset, int count) {
			var read = base.Read(buffer, offset, count);
			m_position += read;
			return read;
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Reads data off the stream
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <returns>The number of bytes read</returns>
		public override int Read(Span<byte> buffer) {
			var read = base.Read(buffer);
			m_position += read;
			return read;
		}
#endif

		/// <summary>
		/// Reads data off the stream asynchronously
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <param name="offset">Where to start in the buffer</param>
		/// <param name="count">Number of bytes to read</param>
		/// <param name="token">The cancellation token for this task</param>
		/// <returns>The number of bytes read</returns>
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) {
			int read = await base.ReadAsync(buffer, offset, count, token);
			m_position += read;
			return read;
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Reads data off the stream asynchronously
		/// </summary>
		/// <param name="buffer">The buffer to read into</param>
		/// <param name="token">The cancellation token for this task</param>
		/// <returns>The number of bytes read</returns>
		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token) {
			int read = await base.ReadAsync(buffer, token);
			m_position += read;
			return read;
		}
#endif

		/// <summary>
		/// Writes data to the stream
		/// </summary>
		/// <param name="buffer">The buffer to write to the stream</param>
		/// <param name="offset">Where to start in the buffer</param>
		/// <param name="count">The number of bytes to write to the buffer</param>
		public override void Write(byte[] buffer, int offset, int count) {
			base.Write(buffer, offset, count);
			m_position += count;
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Writes data to the stream
		/// </summary>
		/// <param name="buffer">The buffer to write to the stream</param>
		public override void Write(ReadOnlySpan<byte> buffer) {
			base.Write(buffer);
			m_position += buffer.Length;
		}
#endif

		/// <summary>
		/// Writes data to the stream asynchronously
		/// </summary>
		/// <param name="buffer">The buffer to write to the stream</param>
		/// <param name="offset">Where to start in the buffer</param>
		/// <param name="count">The number of bytes to write to the buffer</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) {
			await base.WriteAsync(buffer, offset, count, token);
			m_position += count;
		}

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		/// <summary>
		/// Writes data to the stream asynchronously
		/// </summary>
		/// <param name="buffer">The buffer to write to the stream</param>
		/// <param name="token">The <see cref="CancellationToken"/> for this task</param>
		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token) {
			await base.WriteAsync(buffer, token);
			m_position += buffer.Length;
		}
#endif

		/// <summary>
		/// Sets the length of this stream
		/// </summary>
		/// <param name="value">Value to apply to the Length property</param>
		public override void SetLength(long value) {
			m_length = value;
		}

		/// <summary>
		/// Sets the position of the stream. Intended to be used
		/// internally by FtpControlConnection.
		/// </summary>
		/// <param name="pos">The position</param>
		public void SetPosition(long pos) {
			m_position = pos;
		}

		/// <summary>
		/// Closes the connection and reads (and discards) the server's reply
		/// </summary>
		public new void Close() {
			base.Close();

			try {
				if (ControlConnection != null) {
					((IInternalFtpClient)ControlConnection).CloseDataStreamInternal(this);
				}
			}
			finally {
				m_commandStatus = new FtpReply();
				m_control = null;
			}

			return;
		}

		/// <summary>
		/// Closes the connection and reads (and discards) the server's reply
		/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		public override async ValueTask CloseAsync(CancellationToken token = default(CancellationToken)) {
#else
		public override async Task CloseAsync(CancellationToken token = default(CancellationToken)) {
#endif
			await base.CloseAsync(token);

			try {
				if (ControlConnection != null) {
					await ((IInternalFtpClient)ControlConnection).CloseDataStreamInternal(this, token);
				}
			}
			finally {
				m_commandStatus = new FtpReply();
				m_control = null;
			}

			return;
		}

		/// <summary>
		/// Creates a new data stream object
		/// </summary>
		/// <param name="conn">The control connection to be used for carrying out this operation</param>
		public FtpDataStream(BaseFtpClient conn) : base(conn) {
			ControlConnection = conn ?? throw new ArgumentException("The control connection cannot be null.");

			// always accept certificate no matter what because if code execution ever
			// gets here it means the certificate on the control connection object being
			// cloned was already accepted.
			ValidateCertificate += new FtpSocketStreamSslValidation(delegate (FtpSocketStream obj, FtpSslValidationEventArgs e) { e.Accept = true; });

			m_position = 0;

			IsControlConnection = false;
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~FtpDataStream() {
			// Fix: Hard catch and suppress all exceptions during disposing as there are constant issues with this method
			try {
				if (Client is AsyncFtpClient) {
					DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
				}
				else {
					Dispose();
				}
			}
			catch {
			}
		}
	}
}