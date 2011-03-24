using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpFile {
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
		/// Delete this file
		/// </summary>
		public void Delete() {
			this.Client.RemoveFile(this.FullName);
			this.Length = 0;
			this.LastWriteTime = DateTime.MinValue;
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

		public FtpFile(FtpClient cl, string path) {
			this.FullName = this.CleanPath(path);
			this.Client = cl;
		}

		public FtpFile(FtpClient cl, string root, FtpListItem listing) {
			this.Client = cl;
			this.FullName = this.CleanPath(string.Format("{0}/{1}", root, listing.Name));
			this.LastWriteTime = listing.Modify;
			this.Length = listing.Size;
		}
	}
}
