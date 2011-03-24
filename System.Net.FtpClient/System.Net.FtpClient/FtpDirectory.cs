using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpDirectory {
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

		FtpDirectory[] _dirs = null;
		/// <summary>
		/// A list of directories within this directory
		/// </summary>
		public FtpDirectory[] Directories {
			get {
				if (this._dirs == null) {
					this.LoadListing();
				}

				return _dirs;
			}

			private set { _dirs = value; }
		}

		FtpFile[] _files = null;
		/// <summary>
		/// A list of files within this directory
		/// </summary>
		public FtpFile[] Files {
			get {
				if (_files == null) {
					this.LoadListing();
				}

				return _files;
			}

			private set { _files = value; }
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
			if (this.Files.Length > 0 || this.Directories.Length > 0) {
				if (recursive) {
					foreach (FtpFile f in this.Files) {
						f.Delete();
					}

					foreach (FtpDirectory d in this.Directories) {
						d.Delete(true);
					}
				}
				else {
					throw new FtpException(string.Format("Cannot remove {0} because it is not empty.", this.FullName));
				}
			}

			this.Client.RemoveDirectory(this.FullName);
			this.Files = null;
			this.Directories = null;
			this.LastWriteTime = DateTime.MinValue;
		}

		/// <summary>
		/// Loads the file and directory listing
		/// </summary>
		void LoadListing() {
			List<FtpDirectory> dirs = new List<FtpDirectory>();
			List<FtpFile> files = new List<FtpFile>();

			foreach (FtpListItem i in this.Client.GetListing(this.FullName)) {
				if (i.Type == FtpObjectType.Directory) {
					dirs.Add(new FtpDirectory(this.Client, this.FullName, i));
				}
				else if (i.Type == FtpObjectType.File) {
					files.Add(new FtpFile(this.Client, this.FullName, i));
				}
			}

			this.Directories = dirs.ToArray();
			this.Files = files.ToArray();
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

		public FtpDirectory(FtpClient cl, string path) {
			this.FullName = this.CleanPath(path);
			this.Client = cl;
			this.GetInfo();
		}

		public FtpDirectory(FtpClient cl, string root, FtpListItem listing) {
			this.Client = cl;
			this.FullName = this.CleanPath(string.Format("{0}/{1}", root, listing.Name));
			this.LastWriteTime = listing.Modify;
		}
	}
}
