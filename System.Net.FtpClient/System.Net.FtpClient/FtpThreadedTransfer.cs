using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace System.Net.FtpClient {
	internal class FtpThreadedTransferArgs {
		long _chunkSize = 0;
		/// <summary>
		/// Gets the size of the chunk this thread is
		/// responsible for downloading
		/// </summary>
		public long ChunkSize {
			get { return _chunkSize; }
			private set { _chunkSize = value; }
		}

		long _start = 0;
		/// <summary>
		/// Gets the starting location for this download
		/// </summary>
		public long Start {
			get { return _start; }
			private set { _start = value; }
		}

		/// <summary>
		/// Gets the ending location for this download
		/// </summary>
		public long End {
			get {
				return this.Start + this.ChunkSize;
			}
		}

		long _transferred = 0;
		/// <summary>
		/// Gets the number of bytes transferred
		/// </summary>
		public long Transferred {
			get { return _transferred; }
			set { _transferred = value; }
		}

		/// <summary>
		/// Gets a value indicating if this chunk has been completed
		/// </summary>
		public bool Complete {
			get { return this.Transferred == this.ChunkSize; }
		}

		/// <summary>
		/// Gets the appropriate size of the next read for this chunk
		/// </summary>
		/// <param name="bufsize"></param>
		/// <returns></returns>
		public int GetNextReadSize(int bufsize) {
			if (bufsize > (this.ChunkSize - this.Transferred)) {
				return (int)(this.ChunkSize - this.Transferred);
			}

			return bufsize;
		}

		string _fileName = null;
		/// <summary>
		/// Gets the full path of the file to be transferred
		/// </summary>
		public string FullPath {
			get { return _fileName; }
			private set { _fileName = value; }
		}

		FtpTransferMode _mode = FtpTransferMode.Binary;
		/// <summary>
		/// Gets the transfer mode (ASCII/Binary)
		/// </summary>
		public FtpTransferMode TransferMode {
			get { return _mode; }
			private set { _mode = value; }
		}

		public FtpThreadedTransferArgs(string path, FtpTransferMode mode, long start, long chunk) {
			this.Start = start;
			this.ChunkSize = chunk;
			this.FullPath = path;
			this.TransferMode = mode;
		}
	}

	internal class FtpThreadedTransfer : IDisposable {
		object LockClient = new object();
		FtpClient _ftpClient = null;
		/// <summary>
		/// The FTP client used to perform this operation
		/// </summary>
		public FtpClient Client {
			get {
				return _ftpClient;
			}
			private set {
				_ftpClient = value;
			}
		}

		List<Thread> _threads = new List<Thread>();
		/// <summary>
		/// The threads being used to carry out the operation
		/// </summary>
		List<Thread> Threads {
			get {
				return _threads;
			}
			set {
				_threads = value;
			}
		}

		object LockExceptions = new object();
		List<Exception> _exceptions = new List<Exception>();
		/// <summary>
		/// A list of exceptions that were thrown by the work threads, if any
		/// </summary>
		public List<Exception> Exceptions {
			get {
				return this._exceptions;
			}
			private set {
				this._exceptions = value;
			}
		}

		object LockStream = new object();
		Stream _stream = null;
		/// <summary>
		/// The input or output stream depending if this is an
		/// upload or download
		/// </summary>
		Stream Stream {
			get { return _stream; }
			set { _stream = value; }
		}

		object LockCancel = new object();
		bool _cancel = false;
		/// <summary>
		/// Abort the transfer
		/// </summary>
		public bool Cancel {
			get {
				lock (this.LockCancel) {
					return _cancel;
				}
			}
			private set {
				lock (this.LockCancel) {
					this._cancel = value;
				}
			}
		}

		long _size = 0;
		/// <summary>
		/// Gets the size of the file being transferred
		/// </summary>
		public long Size {
			get { return _size; }
			private set { _size = value; }
		}

		long _transferred = 0;
		/// <summary>
		/// Gets the number of bytes that have been transferred
		/// </summary>
		public long Transferred {
			get { return _transferred; }
			private set { _transferred = value; }
		}

		private void CheckCapabilities() {
			if (!this.Client.HasCapability(FtpCapability.SIZE)) {
				throw new FtpException("Threaded downloads are not supported on servers that do not support retrieving file sizes.");
			}

			if (!this.Client.HasCapability(FtpCapability.REST)) {
				throw new FtpException("Threaded downloads are not supported on servers that do not support resetting the stream position.");
			}
		}

		private FtpClient CloneClient() {
			FtpClient cl = null;

			lock (this.LockClient) {
				cl = new FtpClient() {
					Server = this.Client.Server,
					Username = this.Client.Username,
					Password = this.Client.Password,
					Port = this.Client.Port,
					SslMode = this.Client.SslMode,
					WriteTimeout = this.Client.WriteTimeout,
					ReadTimeout = this.Client.ReadTimeout,
					RecieveBufferSize = this.Client.RecieveBufferSize,
					SendBufferSize = this.Client.SendBufferSize,
				};
			}

			cl.InvalidCertificate += new FtpInvalidCertificate(OnInvalidCertificate);

			return cl;
		}

		object LockInvalidCertificate = new object();

		void OnInvalidCertificate(FtpChannel c, InvalidCertificateInfo e) {
			lock (this.LockInvalidCertificate) {
				this.Client.OnInvalidSslCerticate(c, e);
			}
		}

		private void Write(byte[] buf, int length, long position) {
			lock (this.LockStream) {
				if (this.Stream == null) {
					throw new FtpException("The output stream is null");
				}

				this.Stream.Seek(position, SeekOrigin.Begin);
				this.Stream.Write(buf, 0, length);
				this.Stream.Flush();
			}
		}

		private void ReportProgress(FtpTransferType type, string fullname, long read, DateTime start) {
			FtpTransferInfo e = null;

			lock (this.LockClient) {
				this.Transferred += read;
				e = new FtpTransferInfo(type, fullname, this.Size, this.Transferred,
					start, this.Transferred == this.Size);
				this.Client.OnTransferProgress(e);
			}

			this.Cancel = e.Cancel;
		}

		private void AddException(Exception ex) {
			lock (this.LockExceptions) {
				this.Exceptions.Add(ex);
			}
		}

		private void ThreadDownload(object data) {
			FtpThreadedTransferArgs args = (FtpThreadedTransferArgs)data;

			using (FtpClient client = this.CloneClient()) {
				try {
					using (FtpDataChannel chan = client.OpenRead(args.FullPath, args.TransferMode, args.Start)) {
						byte[] buf = new byte[chan.RecieveBufferSize];
						int read = 0;
						DateTime start = DateTime.Now;

						while ((read = chan.Read(buf, 0, args.GetNextReadSize(buf.Length))) > 0) {
							// currently we seek and write, might be
							// more efficient to write to separate streams 
							// instead of seeking
							//this.Write(buf, read, args.Start + args.Transferred);
							args.Transferred += read;

							this.ReportProgress(FtpTransferType.Download, args.FullPath, read, start);
							if (this.Cancel || args.Complete) {
								// end the transfer.
								break;
							}
						}

						try {
							// this is going to trigger an exception
							// because of early termination of the stream
							// however we do not care.
							chan.Disconnect();
						}
						catch (FtpException) { }
					}

					client.Disconnect();
				}
				catch (Exception ex) {
					this.AddException(ex);
					this.Cancel = true;
				}
			}

#if DEBUG
			System.Diagnostics.Debug.WriteLine(string.Format("Thread #{0} has finished.", Thread.CurrentThread.ManagedThreadId));
#endif
		}

		private void WaitForThreads() {
			foreach (Thread t in this.Threads) {
				t.Join();
			}
		}

		private void CheckExceptions() {
			if (this.Exceptions.Count > 0) {
				string sb = "";

				foreach (Exception e in this.Exceptions) {
					sb += string.Format("{1}{0}", e.Message, Environment.NewLine);
				}

				throw new FtpException(sb);
			}
		}

		public void Download(FtpFile remote, string local, FtpTransferMode mode, long rest, int threads) {
			using (FileStream stream = new FileStream(local, FileMode.OpenOrCreate, FileAccess.Write)) {
				this.Download(remote, stream, mode, rest, threads);
			}
		}

		public void Download(FtpFile remote, Stream local, FtpTransferMode mode, long rest, int threads) {
			long chunk = 0;

			this.Threads.Clear();
			this.CheckCapabilities();
			this.Stream = local;
			this.Size = remote.Length;

			if (threads <= 0) {
				throw new FtpException("The number of download threads cannot be less than or equal to 0!");
			}

			if (!local.CanSeek) {
				throw new FtpException("The output stream is not seekable, cannot perform threaded downloads.");
			}

			chunk = (this.Size / threads);
			if (chunk <= 0) {
				throw new FtpException("The thread chunk size is less than or equal to 0");
			}

			for (int i = 0; i < threads; i++) {
				Thread t = new Thread(new ParameterizedThreadStart(this.ThreadDownload));
				this.Threads.Add(t);
				t.Start(new FtpThreadedTransferArgs(remote.FullName, mode, i * chunk, chunk));
			}

			this.WaitForThreads();
			this.CheckExceptions();
		}

		public void Dispose() {
			this.Client = null;
			this.Stream = null;
			this.Threads = null;
		}

		public FtpThreadedTransfer(FtpClient cl) {
			this.Client = cl;
		}
	}
}
