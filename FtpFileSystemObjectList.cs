using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Net.FtpClient {
	/// <summary>
	/// List of FtpFileSystemObject's
	/// </summary>
	/// <typeparam name="FtpFileSystemObject">FtpFile / FtpDirectory</typeparam>
	public class FtpFileSystemObjectList<FtpFileSystemObject> : IEnumerable {
		private List<FtpFileSystemObject> _list = new List<FtpFileSystemObject>();

        /// <summary>
        /// Gets the number of objects in this list
        /// </summary>
		public int Count {
			get { return _list.Count; }
		}

        /// <summary>
        /// Gets an enumerator
        /// </summary>
        /// <returns></returns>
		public IEnumerator GetEnumerator() {
			return (IEnumerator)_list.GetEnumerator();
		}

        /// <summary>
        /// Returns an array of objects contained in this list
        /// </summary>
        /// <returns></returns>
		public FtpFileSystemObject[] ToArray() {
			return _list.ToArray();
		}

        /// <summary>
        /// Adds the specified object to the list
        /// </summary>
        /// <param name="fso"></param>
		public void Add(FtpFileSystemObject fso) {
			this._list.Add(fso);
		}

        /// <summary>
        /// Remove the specified object from the list
        /// </summary>
        /// <param name="fso"></param>
		public void Remove(FtpFileSystemObject fso) {
			this._list.Remove(fso);
		}

        /// <summary>
        /// Checks if the specified object exists.
        /// </summary>
        /// <param name="fso"></param>
        /// <returns></returns>
		public bool Contains(FtpFileSystemObject fso) {
			return this._list.Contains(fso);
		}

        /// <summary>
        /// Clear the list
        /// </summary>
		public void Clear() {
			this._list.Clear();
		}
	}
}
