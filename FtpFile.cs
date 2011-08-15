using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace System.Net.FtpClient {
	/// <summary>
	/// A file on a FTP server
	/// </summary>
	public class FtpFile : FtpFileSystemObject {
		/// <summary>
		/// Gets a value indicating if this file exists on the server
		/// </summary>
		public bool Exists {
			get { return this.Client.FileExists(this.FullName); }
		}

		FtpDirectory _parent = null;
		/// <summary>
		/// Gets the parent directory.
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
		/// Opens this file for reading
		/// </summary>
		/// <returns></returns>
		public FtpDataStream OpenRead() {
			return this.OpenRead(FtpDataType.Binary, 0);
		}

		/// <summary>
		/// Opens this file for reading
		/// </summary>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataStream OpenRead(long rest) {
			return this.OpenRead(FtpDataType.Binary, 0);
		}

		/// <summary>
		/// Opens this file for reading
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		public FtpDataStream OpenRead(FtpDataType mode) {
			return this.OpenRead(mode, 0);
		}

		/// <summary>
		/// Opens this file for reading
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataStream OpenRead(FtpDataType mode, long rest) {
			return this.Client.OpenRead(this.FullName, mode, rest);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <returns></returns>
		public FtpDataStream OpenWrite() {
			return this.OpenWrite(FtpDataType.Binary, 0);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataStream OpenWrite(long rest) {
			return this.OpenWrite(FtpDataType.Binary, 0);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		public FtpDataStream OpenWrite(FtpDataType mode) {
			return this.OpenWrite(mode, 0);
		}

		/// <summary>
		/// Opens this file for writing
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="rest"></param>
		/// <returns></returns>
		public FtpDataStream OpenWrite(FtpDataType mode, long rest) {
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
		/// <param name="datatype">ASCII/Binary</param>
		public void Download(string local, FtpDataType datatype) {
			this.Client.Download(this, local, datatype);
		}

		/// <summary>
		/// Download this file
		/// </summary>
		/// <param name="local">Local path</param>
		/// <param name="datatype">ASCII/Binary</param>
		/// <param name="rest">Restart location</param>
		public void Download(string local, FtpDataType datatype, long rest) {
			this.Client.Download(this, local, datatype, rest);
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
		/// <param name="datatype"></param>
		public void Upload(string local, FtpDataType datatype) {
			this.Client.Upload(local, this, datatype);
		}

		/// <summary>
		/// Uploads the specified file
		/// </summary>
		/// <param name="local"></param>
		/// <param name="datatype"></param>
		/// <param name="rest"></param>
		public void Upload(string local, FtpDataType datatype, long rest) {
			this.Client.Upload(local, this, datatype, rest);
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

		/// <summary>
		/// Cleans up this object's resources
		/// </summary>
		public override void Dispose() {
			base.Dispose();
			this._parent = null;
		}

		/// <summary>
		/// Constructs a new FtpFile object
		/// </summary>
		/// <param name="cl">The FtpClient to associate this FtpFile with</param>
		/// <param name="path">The remote path to the file</param>
		public FtpFile(FtpClient cl, string path) : base(cl, path) { }

		/// <summary>
		/// Constructs a new FtpFile object
		/// </summary>
		/// <param name="cl">The FtpClient to associate this FtpFile with</param>
		/// <param name="parent">The parent FtpDirectory if any</param>
		/// <param name="listing">The FtpListItem object that was acquired from parsing flie list from the server.</param>
		public FtpFile(FtpClient cl, FtpDirectory parent, FtpListItem listing)
			: base(cl, string.Format("{0}/{1}", parent.FullName, listing.Name)) {
			this.Length = listing.Size;
			this.LastWriteTime = listing.Modify;
			this.Parent = parent;
		}
	}
}
