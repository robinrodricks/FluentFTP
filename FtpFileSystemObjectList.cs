using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Net.FtpClient {
	/// <summary>
	/// List of FtpFileSystemObject's
	/// </summary>
	/// <typeparam name="FtpFileSystemObject">FtpFile / FtpDirectory</typeparam>
	public class FtpFileSystemObjectList<FtpFileSystemObject> : IEnumerable {
		public List<FtpFileSystemObject> _list = new List<FtpFileSystemObject>();

		public int Count {
			get { return _list.Count; }
		}

		public IEnumerator GetEnumerator() {
			return (IEnumerator)_list.GetEnumerator();
		}

		public FtpFileSystemObject[] ToArray() {
			return _list.ToArray();
		}

		public void Add(FtpFileSystemObject fso) {
			this._list.Add(fso);
		}

		public void Remove(FtpFileSystemObject fso) {
			this._list.Remove(fso);
		}

		public bool Contains(FtpFileSystemObject fso) {
			return this._list.Contains(fso);
		}

		public void Clear() {
			this._list.Clear();
		}
	}
}
