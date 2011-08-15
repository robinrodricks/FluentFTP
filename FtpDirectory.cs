using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	/// <summary>
	/// Represents a directory on a FTP server
	/// </summary>
	public class FtpDirectory : FtpFileSystemObject {
		/// <summary>
		/// Gets a value indicating if this directory exists on the server
		/// </summary>
		public bool Exists {
			get { return this.Client.DirectoryExists(this.FullName); }
		}

		/// <summary>
		/// Directory size will always be 0.
		/// </summary>
		public override long Length {
			get {
				return 0;
			}
			protected set {

			}
		}

		/// <summary>
		/// Last modification time
		/// </summary>
		public override DateTime LastWriteTime {
			get {
				if(base._lastWriteTime == DateTime.MinValue && this.Client.HasCapability(FtpCapability.MDTMDIR)) {
					this.LastWriteTime = this.Client.GetLastWriteTime(string.Format("{0}/", this.FullName));

					if(_lastWriteTime == DateTime.MinValue) {
						// remove mdtm capability for directories
						// because this server doesn't support it
						this.Client.RemoveCapability(FtpCapability.MDTMDIR);
					}
				}

				return _lastWriteTime;
			}
			protected set { _lastWriteTime = value; }
		}

		FtpFileSystemObjectList<FtpDirectory> _dirs = null;
		/// <summary>
		/// A list of directories within this directory
		/// </summary>
		public FtpDirectory[] Directories {
			get {
				if(this._dirs == null) {
					this._dirs = new FtpFileSystemObjectList<FtpDirectory>();
					this.LoadListing();
				}

				// .net 2 solution requires me to cast these
				// but the .net 4 solution doesn't?
				return (FtpDirectory[])_dirs.ToArray();
			}
		}

		FtpFileSystemObjectList<FtpFile> _files = null;
		/// <summary>
		/// A list of files within this directory
		/// </summary>
		public FtpFile[] Files {
			get {
				if(this._files == null) {
					this._files = new FtpFileSystemObjectList<FtpFile>();
					this.LoadListing();
				}

				// .net 2 solution requires me to cast these
				// but the .net 4 solution doesn't?
				return (FtpFile[])_files.ToArray();
			}
		}

		/// <summary>
		/// Clears the file listing results
		/// </summary>
		public void ClearListing() {
			this._dirs = null;
		}

		FtpDirectory _parent = null;
		/// <summary>
		/// Gets the parent directory. If this is the top level directory, this property will be null.
		/// </summary>
		public FtpDirectory Parent {
			get {
				if(_parent == null && this.FullName != "/") {
					_parent = new FtpDirectory(this.Client, System.IO.Path.GetDirectoryName(this.FullName));
				}

				return _parent;
			}

			private set {
				_parent = value;
			}
		}

		/// <summary>
		/// Creates this directory on the server. Exception thrown 
		/// if the directory already exists.
		/// </summary>
		public void Create() {
			this.Client.CreateDirectory(this.FullName);
		}

		/// <summary>
		/// Deletes the specified file and removes it from the list of files in this
		/// directory if it's there
		/// </summary>
		/// <param name="file"></param>
		public void Delete(FtpFile file) {
			this.Client.RemoveFile(file.FullName);

			if(this._files.Contains(file)) {
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
			if(recursive) {
				if(dir.Files.Length > 0) {
					foreach(FtpFile f in dir.Files) {
						f.Delete();
					}
				}

				if(dir.Directories.Length > 0) {
					foreach(FtpDirectory d in dir.Directories) {
						d.Delete();
					}
				}
			}

			this.Client.RemoveDirectory(dir.FullName);

			if(this._dirs.Contains(dir)) {
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
			if(this.Parent != null) {
				this.Parent.Delete(this, recursive);
			}
			else {
				throw new FtpException("You can't remove the top level directory!");
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
			if(_dirs.Count > 0) {
				_dirs.Add(fd);
			}

			return fd;
		}

		/// <summary>
		/// Loads the file and directory listing
		/// </summary>
		void LoadListing() {
			FtpFileSystemObjectList<FtpDirectory> dirs = new FtpFileSystemObjectList<FtpDirectory>();
			FtpFileSystemObjectList<FtpFile> files = new FtpFileSystemObjectList<FtpFile>();

			foreach(FtpListItem i in this.Client.GetListing(this.FullName)) {
				if(i.Type == FtpObjectType.Directory) {
					dirs.Add(new FtpDirectory(this.Client, this, i));
				}
				else if(i.Type == FtpObjectType.File) {
					files.Add(new FtpFile(this.Client, this, i));
				}
			}

			this._dirs = dirs;
			this._files = files;
		}

		/// <summary>
		/// Clean up this object and release all of it's resources.
		/// </summary>
		public override void Dispose() {
			base.Dispose();
			this._parent = null;
			this._files = null;
			this._dirs = null;
		}

		/// <summary>
		/// Initialize a new object representing a directory on the FTP server
		/// </summary>
		/// <param name="cl">The client this directory will be associated with</param>
		/// <param name="path">The full path of the object on the server</param>
		public FtpDirectory(FtpClient cl, string path) : base(cl, path) { }

		/// <summary>
		/// Initialize a new object representing a directory on the FTP server
		/// </summary>
		/// <param name="cl">The client this object is associated with</param>
		/// <param name="parent">The parent directory (if any)</param>
		/// <param name="listing">The file listing object that was parsed to get this object's data</param>
		public FtpDirectory(FtpClient cl, FtpDirectory parent, FtpListItem listing)
			: base(cl, string.Format("{0}/{1}", parent.FullName, listing.Name)) {
			this.LastWriteTime = listing.Modify;
			this.Parent = parent;
		}
	}
}
