using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpDirectory : IDisposable {
		FtpClient _client = null;
		/// <summary>
		/// The FtpClient object this directory is associated with
		/// </summary>
		public FtpClient Client {
			get { return _client; }
			private set { _client = value; }
		}

		string _path = null;
		/// <summary>
		/// The full or relative path of this directory on the server
		/// </summary>
		public string FullName {
			get { return _path; }
			set { _path = value; }
		}

		/// <summary>
		/// The name of this directory
		/// </summary>
		public string Name {
			get { return System.IO.Path.GetFileName(this.FullName); }
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

		List<FtpDirectory> _dirs = new List<FtpDirectory>();
		/// <summary>
		/// A list of directories within this directory
		/// </summary>
		public FtpDirectory[] Directories {
			get {
				if (this._dirs.Count < 1) {
					this.LoadListing();
				}

				return _dirs.ToArray();
			}
		}

		List<FtpFile> _files = new List<FtpFile>();
		/// <summary>
		/// A list of files within this directory
		/// </summary>
		public FtpFile[] Files {
			get {
				if (this._files.Count < 1) {
					this.LoadListing();
				}

				return _files.ToArray();
			}
		}

		FtpDirectory _parent = null;
		/// <summary>
		/// Gets the parent directory. If this is the top level directory, this property will be null.
		/// </summary>
		public FtpDirectory Parent {
			get {
				if (_parent == null && this.FullName != "/") {
					_parent = new FtpDirectory(this.Client, System.IO.Path.GetDirectoryName(this.FullName));
				}

				return _parent;
			}

			private set {
				_parent = value;
			}
		}

		/// <summary>
		/// Deletes the specified file and removes it from the list of files in this
		/// directory if it's there
		/// </summary>
		/// <param name="file"></param>
		public void Delete(FtpFile file) {
			this.Client.RemoveFile(file.FullName);

			if (this._files.Contains(file)) {
				this._files.Remove(file);
#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("Removed {0} from my file list!", file.FullName));
#endif
			}
		}

		/// <summary>
		/// Deletes the specified directory and removes it from the list of directories
		/// in this directory if it's there
		/// </summary>
		/// <param name="dir"></param>
		public void Delete(FtpDirectory dir) {
			this.Delete(dir, false);
		}

		/// <summary>
		/// Deletes the specified directory and removes it from the list of directories
		/// in this directory if it's there
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="recursive"></param>
		public void Delete(FtpDirectory dir, bool recursive) {
			if (recursive) {
				if (dir.Files.Length > 0) {
					foreach (FtpFile f in dir.Files) {
						f.Delete();
					}
				}

				if (dir.Directories.Length > 0) {
					foreach (FtpDirectory d in dir.Directories) {
						d.Delete();
					}
				}
			}

			this.Client.RemoveDirectory(dir.FullName);

			if (this._dirs.Contains(dir)) {
				this._dirs.Remove(dir);

#if DEBUG
				System.Diagnostics.Debug.WriteLine(string.Format("Removed {0} from my directory list!", dir.FullName));
#endif
			}
		}

		/// <summary>
		/// Delete this directory, throw exception if not empty.
		/// </summary>
		public void Delete() {
			this.Delete(false);
		}

		/// <summary>
		/// Delete this directory and all files and directories beneath it
		/// </summary>
		/// <param name="recursive"></param>
		public void Delete(bool recursive) {
			if (this.Parent != null) {
				this.Parent.Delete(this, recursive);
			}
			else {
				throw new Exception("You can't remove the top level directory!");
			}

			this._files.Clear();
			this._dirs.Clear();
			this.Parent = null;
			this.LastWriteTime = DateTime.MinValue;
		}

		/// <summary>
		/// Checks if the specified directory exists. Will fail
		/// if the server doesn't support MLST
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool DirectoryExists(string name) {
			return this.Client.DirectoryExists(name);
		}

		/// <summary>
		/// Checks if the specified file exists. Will fail
		/// if the server doesn't support MLST
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool FileExists(string name) {
			return this.Client.FileExists(name);
		}

		/// <summary>
		/// Creates the specified sub directory
		/// </summary>
		/// <param name="name"></param>
		public FtpDirectory CreateDirectory(string name) {
			FtpDirectory fd = new FtpDirectory(this.Client, string.Format("{0}/{1}", this.FullName, name));
			
			fd.Parent = this;
			this.Client.CreateDirectory(string.Format("{0}/{1}", this.FullName, name));
			if (_dirs.Count > 0) {
				_dirs.Add(fd);
			}

			return fd;
		}

		/// <summary>
		/// Loads the file and directory listing
		/// </summary>
		void LoadListing() {
			List<FtpDirectory> dirs = new List<FtpDirectory>();
			List<FtpFile> files = new List<FtpFile>();

			foreach (FtpListItem i in this.Client.GetListing(this.FullName)) {
				if (i.Type == FtpObjectType.Directory) {
					dirs.Add(new FtpDirectory(this.Client, this, i));
				}
				else if (i.Type == FtpObjectType.File) {
					files.Add(new FtpFile(this.Client, this, i));
				}
			}

			this._dirs = dirs;
			this._files = files;
		}

		string CleanPath(string path) {
			path = path.Replace('\\', '/');
			return System.Text.RegularExpressions.Regex.Replace(path, @"/+", "/");
		}

		void GetInfo() {
			if (this.Client.HasCapability(FtpCapability.MLST)) {
				if (this.Client.Execute("MLST {0}", this.FullName)) {
					foreach (string s in this.Client.Messages) {
						if (s.StartsWith(" ")) { // MLST response begins with space according to internet draft
							FtpListItem i = new FtpListItem(s, FtpListType.MLST);
							if (i.Type == FtpObjectType.Directory) {
								this.LastWriteTime = i.Modify;
								return;
							}
						}
					}
				}
			}
		}

		public void Dispose() {
			this._files.Clear();
			this._dirs.Clear();
			this._client = null;
			this._lastWriteTime = DateTime.MinValue;
			this._path = null;
		}

		public FtpDirectory(FtpClient cl, string path) {
			this.FullName = this.CleanPath(path);
			this.Client = cl;
			this.GetInfo();
		}

		public FtpDirectory(FtpClient cl, FtpDirectory parent, FtpListItem listing) {
			this.Client = cl;
			this.FullName = this.CleanPath(string.Format("{0}/{1}", parent.FullName, listing.Name));
			this.LastWriteTime = listing.Modify;
			this.Parent = parent;
		}
	}
}
