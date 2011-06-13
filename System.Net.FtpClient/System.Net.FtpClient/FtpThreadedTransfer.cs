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

		DateTime _start = DateTime.Now;
		/// <summary>
		/// Gets the time this download started
		/// </summary>
		public DateTime Start {
			get { return _start; }
			private set { _start = value; }
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

		long _rest = 0;
		/// <summary>
		/// Gets the location this download was resumed at.
		/// </summary>
		public long Resume {
			get { return _rest; }
			private set { _rest = value; }
		}

		/// <summary>
		/// Ensures that the server we're connecting to supports threaded downloads
		/// </summary>
		private void CheckCapabilities() {
			if (!this.Client.HasCapability(FtpCapability.SIZE)) {
				throw new FtpException("Threaded downloads are not supported on servers that do not support retrieving file sizes.");
			}

			if (!this.Client.HasCapability(FtpCapability.REST)) {
				throw new FtpException("Threaded downloads are not supported on servers that do not support resetting the stream position.");
			}
		}

		/// <summary>
		/// Clones the main connection only copying connection infor and options
		/// </summary>
		/// <returns></returns>
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
		/// <summary>
		/// Calls the invalid certificate event on the main thread's client
		/// </summary>
		/// <param name="c"></param>
		/// <param name="e"></param>
		void OnInvalidCertificate(FtpChannel c, InvalidCertificateInfo e) {
			lock (this.LockInvalidCertificate) {
				this.Client.OnInvalidSslCerticate(c, e);
			}
		}

		/// <summary>
		/// Writes the specified data to the specified position in the file
		/// </summary>
		/// <param name="buf"></param>
		/// <param name="length"></param>
		/// <param name="position"></param>
		private void Write(byte[] buf, int length, long position) {
			lock (this.LockStream) {
				if (this.Stream == null) {
					throw new FtpException("The output stream is null");
				}

				this.Stream.Seek(position, SeekOrigin.Begin);
				this.Stream.Write(buf, 0, length);
				//this.Stream.Flush(); // this improves the seek time for new files
				// where all of the space is not yet allocated.
			}
		}

		/// <summary>
		/// Pre-Allocate file storage to make the transfer faster
		/// </summary>
		/// <param name="s"></param>
		private void PreAllocateFile(Stream s) {
#if DEBUG
			DateTime dtstart = DateTime.Now;
#endif
			if (s.Length < this.Size) {
				long start = s.Position;
				long written = s.Length;

				try {
					while (written != this.Size) {
						byte[] buf = new byte[this.Client.DefaultFileSystemBufferSize];
						int bufSize = buf.Length;

						if (bufSize + written > this.Size) {
							bufSize = (int)(this.Size - written);
						}

						s.Write(buf, 0, bufSize);
						written += bufSize;
					}
				}
				finally {
					s.Flush();
					s.Seek(start, SeekOrigin.Begin);
				}
			}

#if DEBUG
			TimeSpan ts = DateTime.Now.Subtract(dtstart);

			System.Diagnostics.Debug.WriteLine(string.Format("File allocation time: {0}h {1}m {2}s",
				Math.Round(ts.TotalHours, 0), Math.Round(ts.TotalMinutes, 0),
				Math.Round(ts.TotalSeconds, 0)));
#endif
		}

		/// <summary>
		/// Calls the TransferProgress event on the main connection
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fullname"></param>
		/// <param name="read"></param>
		private void ReportProgress(FtpTransferType type, string fullname, long read) {
			FtpTransferInfo e = null;

			lock (this.LockClient) {
				this.Transferred += read;
				e = new FtpTransferInfo(type, fullname, this.Size, this.Resume, this.Transferred,
					this.Start, this.Transferred == this.Size);
				this.Client.OnTransferProgress(e);
			}

			this.Cancel = e.Cancel;
		}

		/// <summary>
		/// Adds an exception to the exception list
		/// </summary>
		/// <param name="ex"></param>
		private void AddException(Exception ex) {
			lock (this.LockExceptions) {
				this.Exceptions.Add(ex);
			}
		}


		/// <summary>
		/// Worker thread for downloading a specified chunk of data
		/// </summary>
		/// <param name="data"></param>
		private void ThreadDownload(object data) {
			FtpThreadedTransferArgs args = (FtpThreadedTransferArgs)data;

			using (FtpClient client = this.CloneClient()) {
				try {
					using (FtpDataChannel chan = client.OpenRead(args.FullPath, args.TransferMode, args.Start)) {
						byte[] buf = new byte[chan.RecieveBufferSize];
						int read = 0;

						while ((read = chan.Read(buf, 0, args.GetNextReadSize(buf.Length))) > 0) {
							// currently we seek and write, might be
							// more efficient to write to separate streams 
							// instead of seeking
							this.Write(buf, read, args.Start + args.Transferred);
							args.Transferred += read;

							this.ReportProgress(FtpTransferType.Download, args.FullPath, read);
							if (this.Cancel || args.Complete) {
								// end the transfer.
								break;
							}
						}

						chan.Disconnect(true);
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

		/// <summary>
		/// Waits for all of the download threads to finish and
		/// uses the NOOP command to keep the main connection alive
		/// </summary>
		private void WaitForThreads() {
			foreach (Thread t in this.Threads) {
				while (t.IsAlive) {
					t.Join(10000); // timeout so that we can keep the main thread's connection alive
					this.Client.NoOp(); // keep the main connection alive
				}
			}
		}

		/// <summary>
		/// Checks exceptions from the other threads and combines them
		/// into one exception.
		/// </summary>
		private void CheckExceptions() {
			if (this.Exceptions.Count > 0) {
				string sb = "";

				foreach (Exception e in this.Exceptions) {
					sb += string.Format("{1}{0}", e.Message, Environment.NewLine);
				}

				throw new FtpException(sb);
			}
		}

		/// <summary>
		/// Downloads the specified file using the specified number of threads
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		/// <param name="mode"></param>
		/// <param name="rest"></param>
		/// <param name="threads"></param>
		public void Download(FtpFile remote, string local, FtpTransferMode mode, long rest, int threads) {
			using (FileStream stream = new FileStream(local, FileMode.OpenOrCreate, FileAccess.Write)) {
				this.Download(remote, stream, mode, rest, threads);
			}
		}

		/// <summary>
		/// Downloads the specified file using the specified number of threads
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		/// <param name="mode"></param>
		/// <param name="rest"></param>
		/// <param name="threads"></param>
		public void Download(FtpFile remote, Stream local, FtpTransferMode mode, long rest, int threads) {
			long chunk = 0; // chunk size, will reflect the resume location
			long dsize = 0; // download size used for taking into account the resume location

			this.Threads.Clear();
			this.CheckCapabilities();
			this.Stream = local;
			this.Size = remote.Length;
			this.Transferred = rest;
			this.Resume = rest;
			dsize = this.Size - rest;

			if (threads <= 0) {
				throw new FtpException("The number of download threads cannot be less than or equal to 0!");
			}

			if (!local.CanSeek) {
				throw new FtpException("The output stream is not seekable, cannot perform threaded downloads.");
			}

			chunk = (dsize / threads);
			if (chunk <= 0) {
				throw new FtpException("The thread chunk size is less than or equal to 0");
			}

			//////////////////////////////
			// this operation is slow, however
			// it decreases the seek time when
			// each thread needs to write data 
			// to the local file.
			// this.PreAllocateFile(local);
			//////////////////////////////

			this.Stream.Seek(this.Size, SeekOrigin.Begin);
			this.Start = DateTime.Now;

			for (int i = 0; i < threads; i++) {
				long chunkSize = chunk;
				long chunkStart = i * chunk;
				long chunkEnd = chunkStart + chunkSize;
				Thread t = new Thread(new ParameterizedThreadStart(this.ThreadDownload));

				// make sure that the last thread gets the rest of the data
				// incase the chunk size was not an even split of the file size
				if ((this.Size - chunkEnd) < chunkSize) {
					chunkSize += (this.Size - chunkEnd);
				}

				this.Threads.Add(t);
				t.Start(new FtpThreadedTransferArgs(remote.FullName, mode, (i * chunk) + rest, chunkSize));
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
