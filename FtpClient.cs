using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.IO;

namespace System.Net.FtpClient {
	public delegate void FtpTransferProgress(FtpTransferInfo e);

	public class FtpClient : FtpCommandChannel {
		string _username = null;
		/// <summary>
		/// The username to authenticate with
		/// </summary>
		public string Username {
			get { return _username; }
			set { _username = value; }
		}

		string _password = null;
		/// <summary>
		/// The password to authenticate with
		/// </summary>
		public string Password {
			get { return _password; }
			set { _password = value; }
		}

		int _defBufferSize = 8192;
		/// <summary>
		/// Gets or sets the default buffer size to use when
		/// allocating local file storage. Only used in threaded
		/// downloads.
		/// </summary>
		public int DefaultFileSystemBufferSize {
			get { return _defBufferSize; }
			set { _defBufferSize = value; }
		}

		event FtpTransferProgress _transfer = null;
		/// <summary>
		/// Event fired from Download() and Upload() methods
		/// </summary>
		public event FtpTransferProgress TransferProgress {
			add { _transfer += value; }
			remove { _transfer -= value; }
		}

		FtpDirectory _currentDirectory = null;
		/// <summary>
		/// Gets the current working directory. Use the SetWorkingDirectory() method
		/// to change the working directory.
		/// </summary>
		public FtpDirectory CurrentDirectory {
			get {
				if(_currentDirectory == null) {
					Match m;

					this.LockCommandChannel();

					try {
						if(!this.Execute("PWD")) {
							throw new FtpException(this.ResponseMessage);
						}

						m = Regex.Match(this.ResponseMessage, "\"(.*)\"");
						if(!m.Success || m.Groups.Count < 2) {
							throw new FtpException(string.Format("Failed to parse current working directory from {0}", this.ResponseMessage));
						}

						this._currentDirectory = new FtpDirectory(this, m.Groups[1].Value);
					}
					finally {
						this.UnlockCommandChannel();
					}
				}

				return _currentDirectory;
			}

			private set {
				_currentDirectory = value;
			}
		}

		/// <summary>
		/// Gets the system type that we're connected to
		/// </summary>
		public string System {
			get {
				try {
					this.LockCommandChannel();

					if(!this.Execute("SYST")) {
						throw new FtpException(this.ResponseMessage);
					}

					return this.ResponseMessage;
				}
				finally {
					this.UnlockCommandChannel();
				}
			}
		}

		public void Connect(string username, string password) {
			this.Username = username;
			this.Password = password;
			this.Connect();
		}

		public void Connect(string username, string password, string server) {
			this.Server = server;
			this.Connect(username, password);
		}

		public void Connect(string username, string password, string server, int port) {
			this.Port = port;
			this.Connect(username, password, server);
		}

		/// <summary>
		/// This is the ConnectionReady event handler. It performs the FTP login
		/// if a connection to the server has been made.
		/// </summary>
		void Login(FtpChannel c) {
			this.LockCommandChannel();

			try {
				if(this.Username != null) {
					// there's no reason to pipeline here if the password is null
					if(this.EnablePipelining && this.Password != null) {
						FtpCommandResult[] res = this.Execute(new string[] {
							string.Format("USER {0}", this.Username),
							string.Format("PASS {0}", this.Password)
						});

						foreach(FtpCommandResult r in res) {
							if(!r.ResponseStatus) {
								throw new FtpException(r.ResponseMessage);
							}
						}
					}
					else {
						if(!this.Execute("USER {0}", this.Username)) {
							throw new FtpException(this.ResponseMessage);
						}

						if(this.ResponseType == FtpResponseType.PositiveIntermediate) {
							if(this.Password == null) {
								throw new FtpException("The server is asking for a password but it has been set.");
							}

							if(!this.Execute("PASS {0}", this.Password)) {
								throw new FtpException(this.ResponseMessage);
							}
						}
					}
				}
			}
			finally {
				this.UnlockCommandChannel();
			}

			this.CurrentDirectory = new FtpDirectory(this, "/");
		}

		/// <summary>
		/// Sends the NoOp command. Does nothing other than send a command to the
		/// server and get a response.
		/// </summary>
		public void NoOp() {
			this.LockCommandChannel();

			try {
				if(!this.Execute("NOOP")) {
					throw new FtpException(this.ResponseMessage);
				}
			}
			finally {
				this.UnlockCommandChannel();
			}
		}

		/// <summary>
		/// Gets a raw directory listing of the current working directory. Prefers
		/// the MLSD command to LIST if it's available.
		/// </summary>
		/// <returns></returns>
		public string[] GetRawListing() {
			return this.GetRawListing(this.CurrentDirectory.FullName);
		}

		/// <summary>
		/// Returns a raw file listing, preferring to use the MLSD command
		/// over LIST if it is available
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <returns>string array of the raw listing</returns>
		public string[] GetRawListing(string path) {
			if(this.HasCapability(FtpCapability.MLSD)) {
				return this.GetRawListing(path, FtpListType.MLSD);
			}

			return this.GetRawListing(path, FtpListType.LIST);
		}

		/// <summary>
		/// Returns a raw file listing using the specified LIST type
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <param name="type"></param>
		/// <returns>string array of the raw listing</returns>
		public string[] GetRawListing(string path, FtpListType type) {
			List<string> lst = new List<string>();
			string cmd, buf;

			switch(type) {
				case FtpListType.LIST:
					cmd = "LIST";
					break;
				case FtpListType.MLSD:
				case FtpListType.MLST:
					cmd = "MLSD";
					break;
				default:
					throw new NotImplementedException("The specified list type has not been implemented.");
			}

			this.LockCommandChannel();

			try {
				using(FtpDataChannel dc = this.OpenDataChannel(FtpTransferMode.ASCII)) {
					if(!dc.Execute("{0} {1}", cmd, path)) {
						throw new FtpException(this.ResponseMessage);
					}

					while((buf = dc.ReadLine()) != null) {
						lst.Add(buf);
					}
				}
			}
			finally {
				this.UnlockCommandChannel();
			}

			return lst.ToArray();
		}

		/// <summary>
		/// Gets a file listing, parses it, and returns an array of FtpListItem 
		/// objects that contain the parsed information. Supports MLSD/LIST (DOS and UNIX) formats.
		/// </summary>
		/// <returns></returns>
		public FtpListItem[] GetListing() {
			return this.GetListing(this.CurrentDirectory.FullName);
		}

		/// <summary>
		/// Gets a file listing, parses it, and returns an array of FtpListItem 
		/// objects that contain the parsed information. Supports MLSD/LIST (DOS and UNIX) formats.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public FtpListItem[] GetListing(string path) {
			if(this.HasCapability(FtpCapability.MLSD)) {
				return this.GetListing(path, FtpListType.MLSD);
			}

			return this.GetListing(path, FtpListType.LIST);
		}

		/// <summary>
		/// Gets a file listing, parses it, and returns an array of FtpListItem 
		/// objects that contain the parsed information. Supports MLSD/LIST (DOS and UNIX) formats.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public FtpListItem[] GetListing(string path, FtpListType type) {
			FtpListItem[] list = FtpListItem.ParseList(this.GetRawListing(path, type), type);
			
			// parsing last write time out of most LIST formats is not feasable so it's ignored.
			// if the server supports the MDTM command and pipelining is enable, we 
			// can go ahead and retrieve the last write time's of the files in this list.
			if(list.Length > 0 && this.EnablePipelining && this.HasCapability(FtpCapability.MDTM)) {
				List<FtpListItem> items = new List<FtpListItem>();

				for(int i = 0; i < list.Length; i++) {
					if(list[i].Type == FtpObjectType.File && list[i].Modify == DateTime.MinValue) {
						items.Add(list[i]);
					}
				}

				if(items.Count > 0) {
					this.BeginExecute();

					foreach(FtpListItem i in items) {
						this.Execute("MDTM {0}/{1}", path, i.Name);
					}

					FtpCommandResult[] res = this.EndExecute();

					for(int i = 0; i < res.Length; i++) {
						if(res[i].ResponseStatus) {
							items[i].Modify = this.ParseLastWriteTime(res[i].ResponseMessage);
						}
					}
				}
			}

			return list;
		}

		/// <summary>
		/// Changes the current working directory
		/// </summary>
		/// <param name="path">The full or relative (to the current directory) path</param>
		public void SetWorkingDirectory(string path) {
			this.LockCommandChannel();

			try {
				if(!this.Execute("CWD {0}", path)) {
					throw new FtpException(this.ResponseMessage);
				}
			}
			finally {
				this.UnlockCommandChannel();
			}

			this.CurrentDirectory = null;
		}

		/// <summary>
		/// Gets the last write time if the server supports the MDTM command. If the
		/// server does not support the MDTM NotImplementedException is thrown.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <returns>DateTime/DateTime.MinValue if there was a problem parsing the date</returns>
		public DateTime GetLastWriteTime(string path) {
			string[] formats = new string[] { "yyyyMMddHHmmss", "yyyyMMddHHmmss.fff" };
			DateTime modify = DateTime.MinValue;

			if(!this.HasCapability(FtpCapability.MDTM)) {
				throw new NotImplementedException("The connected server does not support the MDTM command.");
			}

			this.LockCommandChannel();

			try {
				if(this.Execute("MDTM {0}", path)) {
					/*if(DateTime.TryParseExact(this.ResponseMessage, formats,
						CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out modify)) {
						return modify;
					}*/

					return this.ParseLastWriteTime(this.ResponseMessage);
				}
			}
			finally {
				this.UnlockCommandChannel();
			}

			return DateTime.MinValue;
		}

		protected DateTime ParseLastWriteTime(string mdtm) {
			string[] formats = new string[] { "yyyyMMddHHmmss", "yyyyMMddHHmmss.fff" };
			DateTime modify = DateTime.MinValue;

			if(!DateTime.TryParseExact(mdtm, formats,CultureInfo.InvariantCulture, 
				DateTimeStyles.AssumeLocal, out modify)) {
				modify = DateTime.MinValue;
			}

			return modify;
		}

		/// <summary>
		/// Gets the size of the specified file. Prefer the MLST command since some servers don't
		/// support large files. If there are any errors getting the file size, 0 will be returned
		/// rather than throwing an exception.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <returns>The file size, 0 if there was a problem parsing the size</returns>
		public long GetFileSize(string path) {
			// prefer MLST for getting the file size because some
			// servers won't give the file size back for large files
			if(this.HasCapability(FtpCapability.MLST)) {
				this.LockCommandChannel();

				try {
					if(!this.Execute("MLST {0}", path)) {
						throw new FtpException(this.ResponseMessage);
					}

					foreach(string s in this.Messages) {
						// MLST response starts with a space according to draft-ietf-ftpext-mlst-16
						if(s.StartsWith(" ") && s.ToLower().Contains("size")) {
							Match m = Regex.Match(s, @"Size=(\d+);", RegexOptions.IgnoreCase);
							long size = 0;

							if(m.Success && !long.TryParse(m.Groups[1].Value, out size)) {
								size = 0;
							}

							return size;
						}
					}
				}
				finally {
					this.UnlockCommandChannel();
				}
			}
			// used for older servers, has limitations, will error
			// if the file size is too big.
			else if(this.HasCapability(FtpCapability.SIZE)) {
				long size = 0;
				Match m;

				this.LockCommandChannel();

				try {
					// ignore errors, return 0 if there is one. some servers
					// don't support large file sizes.
					if(this.Execute("SIZE {0}", path)) {
						m = Regex.Match(this.ResponseMessage, @"(\d+)");
						if(m.Success && !long.TryParse(m.Groups[1].Value, out size)) {
							size = 0;
						}
					}
				}
				finally {
					this.UnlockCommandChannel();
				}

				return size;
			}

			// we failed to get a file size due to server or code errors however
			// we don't want to trigger an exception because this is not a fatal
			// error. people implementing this code need to be aware of this fact.
			return 0;
		}

		/// <summary>
		/// Removes the specified directory
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		public void RemoveDirectory(string path) {
			this.LockCommandChannel();

			try {
				if(!this.Execute("RMD {0}", path)) {
					throw new FtpException(this.ResponseMessage);
				}
			}
			finally {
				this.UnlockCommandChannel();
			}
		}

		/// <summary>
		/// Removes the specified file
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		public void RemoveFile(string path) {
			this.LockCommandChannel();

			try {
				if(!this.Execute("DELE {0}", path)) {
					throw new FtpException(this.ResponseMessage);
				}
			}
			finally {
				this.UnlockCommandChannel();
			}
		}

		/// <summary>
		/// Creates the specified directory
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		public void CreateDirectory(string path) {
			this.LockCommandChannel();

			try {
				if(!this.Execute("MKD {0}", path)) {
					throw new FtpException(this.ResponseMessage);
				}
			}
			finally {
				this.UnlockCommandChannel();
			}
		}

		/// <summary>
		/// Gets an FTP list item representing the specified file system object
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public FtpListItem GetObjectInfo(string path) {
			if(this.HasCapability(FtpCapability.MLST)) {
				this.LockCommandChannel();

				try {
					if(this.Execute("MLST {0}", path)) {
						foreach(string s in this.Messages) {
							// MLST response starts with a space according to draft-ietf-ftpext-mlst-16
							if(s.StartsWith(" ")) {
								return new FtpListItem(s, FtpListType.MLST);
							}
						}
					}
				}
				finally {
					this.UnlockCommandChannel();
				}
			}
			else {
				// the server doesn't support MLS* functions so
				// we have to do it the hard and inefficient way
				foreach(FtpListItem l in this.GetListing(Path.GetDirectoryName(path))) {
					if(l.Name == Path.GetFileName(path)) {
						return l;
					}
				}
			}

			return new FtpListItem();
		}

		/// <summary>
		/// Checks if the specified directory exists
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool DirectoryExists(string path) {
			return this.GetObjectInfo(path).Type == FtpObjectType.Directory;
		}

		/// <summary>
		/// Checks if the specified file exists
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool FileExists(string path) {
			return this.GetObjectInfo(path).Type == FtpObjectType.File;
		}

		/// <summary>
		/// Renames the specified object
		/// </summary>
		/// <param name="from">The full or relative (to the current working directory) path of the existing file</param>
		/// <param name="to">The full or relative (to the current working directory) path of the new file</param>
		public void Rename(string from, string to) {
			this.LockCommandChannel();

			try {
				if(!this.Execute("RNFR {0}", from)) {
					throw new FtpException(this.ResponseMessage);
				}

				if(!this.Execute("RNTO {0}", to)) {
					throw new FtpException(this.ResponseMessage);
				}
			}
			finally {
				this.UnlockCommandChannel();
			}
		}

		/// <summary>
		/// Opens a file for reading. If you want the file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenRead(string path) {
			return this.OpenRead(path, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Opens a file for reading. If you want the file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path"The full or relative (to the current working directory) path></param>
		/// <param name="rest">Resume location, if specified and server doesn't support REST STREAM, a NotImplementedException is thrown</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenRead(string path, long rest) {
			return this.OpenRead(path, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Opens a file for reading. If you want the file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenRead(string path, FtpTransferMode xferMode) {
			return this.OpenRead(path, xferMode, 0);
		}

		/// <summary>
		/// Opens a file for reading. If you want the file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <param name="rest">Resume location, if specified and server doesn't support REST STREAM, a NotImplementedException is thrown</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenRead(string path, FtpTransferMode xferMode, long rest) {
			FtpDataChannel dc = this.OpenDataChannel(xferMode);

			if(rest > 0) {
				if(!this.HasCapability(FtpCapability.REST)) {
					dc.Disconnect();
					throw new NotImplementedException("The connected server does not support resuming.");
				}

				if(!this.Execute("REST {0}", rest)) {
					dc.Disconnect();
					throw new FtpException(this.ResponseMessage);
				}
			}

			if(!dc.Execute("RETR {0}", path)) {
				dc.Disconnect();
				throw new FtpException(this.ResponseMessage);
			}

			return dc;
		}

		/// <summary>
		/// Opens a file for writing. If you want the file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenWrite(string path) {
			return this.OpenWrite(path, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Opens a file for writing. If you want the file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path"The full or relative (to the current working directory) path></param>
		/// <param name="rest">Resume location, if specified and server doesn't support REST STREAM, a NotImplementedException is thrown</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenWrite(string path, long rest) {
			return this.OpenWrite(path, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Opens a file for writing. If you want the file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenWrite(string path, FtpTransferMode xferMode) {
			return this.OpenWrite(path, xferMode, 0);
		}

		/// <summary>
		/// Opens a file for writing. If you want the existing file size, be sure to retrieve
		/// it before attempting to open a file on the server.
		/// </summary>
		/// <param name="path">The full or relative (to the current working directory) path</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <param name="rest">Resume location, if specified and server doesn't support REST STREAM, a NotImplementedException is thrown</param>
		/// <returns>FtpDataChannel used for reading the data stream. Be sure to disconnect when finished.</returns>
		public FtpDataChannel OpenWrite(string path, FtpTransferMode xferMode, long rest) {
			FtpDataChannel dc = this.OpenDataChannel(xferMode);

			if(rest > 0) {
				this.LockCommandChannel();

				try {
					if(!this.HasCapability(FtpCapability.REST)) {
						dc.Disconnect();
						throw new NotImplementedException("The connected server does not support resuming.");
					}

					if(!this.Execute("REST {0}", rest)) {
						dc.Disconnect();
						throw new FtpException(this.ResponseMessage);
					}
				}
				finally {
					this.UnlockCommandChannel();
				}
			}

			if(!dc.Execute("STOR {0}", path)) {
				dc.Disconnect();
				throw new FtpException(this.ResponseMessage);
			}

			return dc;
		}

		/// <summary>
		/// Fires the TransferProgress event
		/// </summary>
		/// <param name="e"></param>
		public void OnTransferProgress(FtpTransferInfo e) {
			if(_transfer != null) {
				_transfer(e);
			}
		}

		/// <summary>
		/// Downloads a file from the server to the current working directory
		/// </summary>
		/// <param name="remote"></param>
		public void Download(string remote) {
			string local = string.Format(@"{0}\{1}",
				Environment.CurrentDirectory, Path.GetFileName(remote));
			this.Download(remote, local);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Download(string remote, string local) {
			this.Download(remote, local, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Download(string remote, Stream ostream) {
			this.Download(new FtpFile(this, remote), ostream, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Download(string remote, string local, long rest) {
			this.Download(remote, local, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Download(string remote, Stream ostream, long rest) {
			this.Download(new FtpFile(this, remote), ostream, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Download(string remote, string local, FtpTransferMode xferMode) {
			this.Download(remote, local, xferMode, 0);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Download(string remote, Stream ostream, FtpTransferMode xferMode) {
			this.Download(new FtpFile(this, remote), ostream, xferMode, 0);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <param name="rest">Resume location</param>
		public void Download(string remote, string local, FtpTransferMode xferMode, long rest) {
			this.Download(new FtpFile(this, remote), local, xferMode, rest);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		/// <param name="xferMode"></param>
		/// <param name="rest"></param>
		/// <param name="threads"></param>
		public void Download(string remote, string local, FtpTransferMode xferMode, long rest, int threads) {
			this.Download(new FtpFile(this, remote), local, xferMode, rest, threads);
		}

		/// <summary>
		/// Downloads a file from the server to the current working directory
		/// </summary>
		/// <param name="remote"></param>
		public void Download(FtpFile remote) {
			this.Download(remote, string.Format(@"{0}\{1}",
				Environment.CurrentDirectory, remote.Name));
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Download(FtpFile remote, string local) {
			this.Download(remote, local, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Download(FtpFile remote, Stream ostream) {
			this.Download(remote, ostream, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Download(FtpFile remote, string local, long rest) {
			this.Download(remote, local, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Download(FtpFile remote, Stream ostream, long rest) {
			this.Download(remote, ostream, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Download(FtpFile remote, string local, FtpTransferMode xferMode) {
			this.Download(remote, local, xferMode, 0);
		}

		/// <summary>
		/// Downloads a file from the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Download(FtpFile remote, Stream ostream, FtpTransferMode xferMode) {
			this.Download(remote, ostream, xferMode, 0);
		}

		/// <summary>
		/// Downloads the specified file from the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		/// <param name="xferMode"></param>
		/// <param name="rest"></param>
		public void Download(FtpFile remote, string local, FtpTransferMode xferMode, long rest) {
			using(FileStream ostream = new FileStream(local, FileMode.OpenOrCreate, FileAccess.Write)) {
				try {
					this.Download(remote, ostream, xferMode, rest);
				}
				finally {
					ostream.Close();
				}
			}
		}

		/// <summary>
		/// Downloads the remote file to the specified stream
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="ostream"></param>
		/// <param name="xferMode"></param>
		/// <param name="rest"></param>
		public void Download(FtpFile remote, Stream ostream, FtpTransferMode xferMode, long rest) {
			long size = remote.Length;
			long total = 0;
			int read = 0;

			if(remote == null) {
				throw new ArgumentException("remote is null");
			}

			if(ostream == null) {
				throw new ArgumentException("ostream is null");
			}

			if(!ostream.CanWrite) {
				throw new ArgumentException("ostream is not writable");
			}

			if(rest > 0 && ostream.CanSeek) { // set reset position
				ostream.Seek(rest, SeekOrigin.Begin);
				total = rest;
			}
			else if(!ostream.CanSeek) {
				rest = 0;
			}

			try {
				using(FtpDataChannel ch = this.OpenRead(remote.FullName, xferMode, rest)) {
					byte[] buf = new byte[ch.RecieveBufferSize];
					DateTime start = DateTime.Now;
					FtpTransferInfo e = null;

					while((read = ch.Read(buf, 0, buf.Length)) > 0) {
						ostream.Write(buf, 0, read);
						total += read;
						e = new FtpTransferInfo(FtpTransferType.Download, remote.FullName, size, rest, total, start, false);

						this.OnTransferProgress(e);
						if(e.Cancel) {
							break;
						}
					}

					// fire one more time to let event handler know that the transfer is complete
					this.OnTransferProgress(new FtpTransferInfo(FtpTransferType.Download, remote.FullName,
						size, rest, total, start, true));
				}
			}
			finally {
				ostream.Flush();
			}
		}

		/// <summary>
		/// Downloads the specified file using the specified number of threads
		/// to complete the operation
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="ostream"></param>
		/// <param name="xferMode"></param>
		/// <param name="rest"></param>
		/// <param name="threads"></param>
		public void Download(FtpFile remote, string local, FtpTransferMode xferMode, long rest, int threads) {
			using(FileStream stream = new FileStream(local, FileMode.OpenOrCreate, FileAccess.Write)) {
				this.Download(remote, stream, xferMode, rest, threads);
			}
		}

		/// <summary>
		/// Downloads the specified file using the specified number of threads
		/// to complete the operation
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="ostream"></param>
		/// <param name="xferMode"></param>
		/// <param name="rest"></param>
		/// <param name="threads"></param>
		public void Download(FtpFile remote, Stream ostream, FtpTransferMode xferMode, long rest, int threads) {
			using(FtpThreadedTransfer txfer = new FtpThreadedTransfer(this)) {
				txfer.Download(remote, ostream, xferMode, rest, threads);
			}
		}

		/// <summary>
		/// Uploads a file to the server in the current working directory
		/// </summary>
		/// <param name="remote"></param>
		public void Upload(string local) {
			string remote = string.Format("{0}/{1}",
				this.CurrentDirectory.FullName,
				Path.GetFileName(local));
			this.Upload(local, remote);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Upload(string local, string remote) {
			this.Upload(local, remote, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Upload(Stream istream, string remote) {
			this.Upload(istream, new FtpFile(this, remote), FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Upload(string local, string remote, long rest) {
			this.Upload(local, remote, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Upload(Stream istream, string remote, long rest) {
			this.Upload(istream, new FtpFile(this, remote), FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Upload(string local, string remote, FtpTransferMode xferMode) {
			this.Upload(local, remote, xferMode, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Upload(Stream istream, string remote, FtpTransferMode xferMode) {
			this.Upload(istream, new FtpFile(this, remote), xferMode, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Local path of the file</param>
		/// <param name="local">Remote path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <param name="rest">Resume location</param>
		public void Upload(string local, string remote, FtpTransferMode xferMode, long rest) {
			this.Upload(local, new FtpFile(this, remote), xferMode, rest);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Upload(string local, FtpFile remote) {
			this.Upload(local, remote, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote"></param>
		/// <param name="local"></param>
		public void Upload(Stream istream, FtpFile remote) {
			this.Upload(istream, remote, FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Upload(string local, FtpFile remote, long rest) {
			this.Upload(local, remote, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="rest">Resume location</param>
		public void Upload(Stream istream, FtpFile remote, long rest) {
			this.Upload(istream, remote, FtpTransferMode.Binary, rest);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Upload(string local, FtpFile remote, FtpTransferMode xferMode) {
			this.Upload(local, remote, xferMode, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Remote path of the file</param>
		/// <param name="local">Local path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Upload(Stream istream, FtpFile remote, FtpTransferMode xferMode) {
			this.Upload(istream, remote, xferMode, 0);
		}

		/// <summary>
		/// Uploads a file to the server
		/// </summary>
		/// <param name="remote">Local path of the file</param>
		/// <param name="local">Remote path of the file</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <param name="rest">Resume location</param>
		public void Upload(string local, FtpFile remote, FtpTransferMode xferMode, long rest) {
			using(FileStream istream = new FileStream(local, FileMode.Open, FileAccess.Read)) {
				try {
					this.Upload(istream, remote, xferMode, rest);
				}
				finally {
					istream.Close();
				}
			}
		}

		/// <summary>
		/// Uploads a stream to the specified remote file
		/// </summary>
		/// <param name="istream"></param>
		/// <param name="remote"></param>
		/// <param name="xferMode"></param>
		/// <param name="rest"></param>
		public void Upload(Stream istream, FtpFile remote, FtpTransferMode xferMode, long rest) {
			long size = 0;
			long total = 0;
			int read = 0;

			if(istream == null) {
				throw new ArgumentException("istream is null");
			}

			if(remote == null) {
				throw new ArgumentException("remote is null");
			}

			if(!istream.CanRead) {
				throw new ArgumentException("istream is not readable");
			}

			if(istream.CanSeek) {
				size = istream.Length;

				if(rest > 0) { // set resume position
					istream.Seek(rest, SeekOrigin.Begin);
					total = rest;
				}
			}
			else {
				rest = 0;
			}

			using(FtpDataChannel ch = this.OpenWrite(remote.FullName, xferMode, rest)) {
				byte[] buf = new byte[ch.RecieveBufferSize];
				DateTime start = DateTime.Now;
				FtpTransferInfo e;

				while((read = istream.Read(buf, 0, buf.Length)) > 0) {
					ch.Write(buf, 0, read);
					total += read;
					e = new FtpTransferInfo(FtpTransferType.Upload, remote.FullName, size, rest, total, start, false);

					this.OnTransferProgress(e);
					if(e.Cancel) {
						break;
					}
				}

				// fire one more time to let event handler know the transfer is complete
				this.OnTransferProgress(new FtpTransferInfo(FtpTransferType.Upload, remote.FullName,
					size, rest, total, start, true));
			}
		}

		public FtpClient()
			: base() {
			this.ConnectionReady += new FtpChannelConnected(Login);
		}

		public FtpClient(string username, string password)
			: this() {
			this.Username = username;
			this.Password = password;
		}

		public FtpClient(string username, string password, string server)
			: this(username, password) {
			this.Server = server;
		}

		public FtpClient(string username, string password, string server, int port)
			: this(username, password, server) {
			this.Port = port;
		}
	}
}
