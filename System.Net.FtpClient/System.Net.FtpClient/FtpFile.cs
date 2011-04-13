using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpFile : IDisposable {
		FtpClient _client = null;
		/// <summary>
		/// The FtpClient object this directory is associated with
		/// </summary>
		public FtpClient Client {
			get { return _client; }
			private set { _client = value; }
		}

		/// <summary>
		/// Gets a value indicating if this file exists on the server
		/// </summary>
		public bool Exists {
			get { return this.Client.FileExists(this.FullName); }
		}

		string _path = null;
		/// <summary>
		/// The full or relative path of this directory on the server
		/// </summary>
		public string FullName {
			get { return _path; }
			set { _path = value; }
		}

		DateTime _lastWriteTime = DateTime.MinValue;
		/// <summary>
		/// Last modification time
		/// </summary>
		public DateTime LastWriteTime {
			get {
				if (_lastWriteTime == DateTime.MinValue) {
					this.LastWriteTime = this.Client.GetLastWriteTime(this.FullName);
				}

				return _lastWriteTime; 
			}

			private set { _lastWriteTime = value; }
		}

		long _length = -1;
		/// <summary>
		/// The size of the file
		/// </summary>
		public long Length {
			get {
				if (_length < 0) {
					this.Length = this.Client.GetFileSize(this.FullName);
				}

				return _length; 
			}

			private set { _length = value; }
		}

		/// <summary>
		/// The name of this directory
		/// </summary>
		public string Name {
			get { return System.IO.Path.GetFileName(this.FullName); }
		}

		FtpDirectory _parent = null;
		/// <summary>
		/// Gets the parent directory.
		/// </summary>
		public FtpDirectory Parent {
			get {
				if (_parent == null) {
					_parent = new FtpDirectory(this.Client, System.IO.Path.GetDirectoryName(this.FullName));
				}

				return _parent;
			}

			private set {
				_parent = value;
			}
		}

		/// <summary>
		/// Opens this file for reading
		/// </summary>
		/// <returns></returns>
		public FtpDataChannel OpenRead() {
			return this.OpenRead(FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Opens this file for reading
		/// </summary>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataChannel OpenRead(long rest) {
			return this.OpenRead(FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Opens this file for reading
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		public FtpDataChannel OpenRead(FtpTransferMode mode) {
			return this.OpenRead(mode, 0);
		}

		/// <summary>
		/// Opens this file for reading
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataChannel OpenRead(FtpTransferMode mode, long rest) {
			return this.Client.OpenRead(this.FullName, mode, rest);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <returns></returns>
		public FtpDataChannel OpenWrite() {
			return this.OpenWrite(FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataChannel OpenWrite(long rest) {
			return this.OpenWrite(FtpTransferMode.Binary, 0);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		public FtpDataChannel OpenWrite(FtpTransferMode mode) {
			return this.OpenWrite(mode, 0);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataChannel OpenWrite(FtpTransferMode mode, long rest) {
			return this.Client.OpenWrite(this.FullName, mode, rest);
		}

		/// <summary>
		/// Download this file to the current working directory
		/// </summary>
		public void Download() {
			this.Client.Download(this);
		}

		/// <summary>
		/// Download this file
		/// </summary>
		/// <param name="local">Local path</param>
		public void Download(string local) {
			this.Client.Download(this, local);
		}

		/// <summary>
		/// Download this file
		/// </summary>
		/// <param name="local">Local path</param>
		/// <param name="rest">Restart position</param>
		public void Download(string local, long rest) {
			this.Client.Download(this, local, rest);
		}

		/// <summary>
		/// Download this file
		/// </summary>
		/// <param name="local">Local path</param>
		/// <param name="xferMode">ASCII/Binary</param>
		public void Download(string local, FtpTransferMode xferMode) {
			this.Client.Download(this, local, xferMode);
		}

		/// <summary>
		/// Download this file
		/// </summary>
		/// <param name="local">Local path</param>
		/// <param name="xferMode">ASCII/Binary</param>
		/// <param name="rest">Restart location</param>
		public void Download(string local, FtpTransferMode xferMode, long rest) {
			this.Client.Download(this, local, xferMode, rest);
		}

		/// <summary>
		/// Uploads the specified file
		/// </summary>
		/// <param name="local"></param>
		public void Upload(string local) {
			this.Client.Upload(local, this);
		}

		/// <summary>
		/// Uploads the specified file 
		/// </summary>
		/// <param name="local"></param>
		/// <param name="rest"></param>
		public void Upload(string local, long rest) {
			this.Client.Upload(local, this, rest);
		}

		/// <summary>
		/// Uploads the specified file
		/// </summary>
		/// <param name="local"></param>
		/// <param name="xferMode"></param>
		public void Upload(string local, FtpTransferMode xferMode) {
			this.Client.Upload(local, this, xferMode);
		}

		/// <summary>
		/// Uploads the specified file
		/// </summary>
		/// <param name="local"></param>
		/// <param name="xferMode"></param>
		/// <param name="rest"></param>
		public void Upload(string local, FtpTransferMode xferMode, long rest) {
			this.Client.Upload(local, this, xferMode, rest);
		}

		/// <summary>
		/// Delete this file
		/// </summary>
		public void Delete() {
			this.Parent.Delete(this);
			this.Length = 0;
			this.LastWriteTime = DateTime.MinValue;
			this.Parent = null;
		}

		void GetInfo() {
			if (this.Client.HasCapability(FtpCapability.MLST)) {
				if (this.Client.Execute("MLST {0}", this.FullName)) {
					foreach (string s in this.Client.Messages) {
						if (s.StartsWith(" ")) { // MLST response begins with space according to internet draft
							FtpListItem i = new FtpListItem(s, FtpListType.MLST);
							if (i.Type == FtpObjectType.File) {
								this.LastWriteTime = i.Modify;
								this.Length = i.Size;
								return;
							}
						}
					}
				}
			}
		}

		string CleanPath(string path) {
			path = path.Replace('\\', '/');
			return System.Text.RegularExpressions.Regex.Replace(path, @"/+", "/");
		}

		public void Dispose() {
			this._client = null;
			this._lastWriteTime = DateTime.MinValue;
			this._length = -1;
			this._parent = null;
			this._path = null;
		}

		public FtpFile(FtpClient cl, string path) {
			this.FullName = this.CleanPath(path);
			this.Client = cl;
		}

		public FtpFile(FtpClient cl, FtpDirectory parent, FtpListItem listing) {
			this.Client = cl;
			this.FullName = this.CleanPath(string.Format("{0}/{1}", parent.FullName, listing.Name));
			this.LastWriteTime = listing.Modify;
			this.Length = listing.Size;
			this.Parent = parent;
		}
	}
}
