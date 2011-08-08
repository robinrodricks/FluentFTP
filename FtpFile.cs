using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpFile : FtpFileSystemObject {
		/// <summary>
		/// Gets a value indicating if this file exists on the server
		/// </summary>
		public bool Exists {
			get { return this.Client.FileExists(this.FullName); }
		}

		/// <summary>
		/// The size of the file
		/// </summary>
		public override long Length {
			get {
				if(_length < 0) {
					this.Length = this.Client.GetFileSize(this.FullName);
				}

				return _length;
			}

			protected set { _length = value; }
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

		public override void Dispose() {
			base.Dispose();
			this._parent = null;
		}

		public FtpFile(FtpClient cl, string path) : base(cl, path) { }

		public FtpFile(FtpClient cl, FtpDirectory parent, FtpListItem listing)
			: base(cl, string.Format("{0}/{1}", parent.FullName, listing.Name)) {
			this.Length = listing.Size;
			this.LastWriteTime = listing.Modify;
			this.Parent = parent;
		}
	}
}
