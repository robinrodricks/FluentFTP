using System;

namespace System.Net.FtpClient {
    /// <summary>
    /// Base class for remote file system objects
    /// </summary>
    public abstract class FtpFileSystemObject {
        private FtpClient _client = null;
        /// <summary>
        /// The FtpClient object this directory is associated with
        /// </summary>
        public FtpClient Client {
            get { return _client; }
            protected set { _client = value; }
        }

        /// <summary>
        /// The name of this object
        /// </summary>
        public string Name {
            get { return System.IO.Path.GetFileName(this.FullName); }
            set {
                if (this.FullName != null) {
                    this.FullName = string.Format("{0}/{1}",
                        System.IO.Path.GetDirectoryName(this.FullName), value);
                }
            }
        }

        private string _path = null;
        /// <summary>
        /// The full or relative path of this directory on the server
        /// </summary>
        public string FullName {
            get { return _path; }
            set { _path = value; }
        }

        /// <summary>
        /// The size of the object, -1 means it hasn't been loaded.
        /// </summary>
        protected long _length = -1;
        /// <summary>
        /// Gets the file system size of this object if
        /// applicable.
        /// </summary>
        public virtual long Length {
            get {
                if (_length == -1) {
                    this.GetInfo();

                    // if the length is still -1 try to get it using the SIZE command
                    if (_length == -1) {
                        this.Length = this.Client.GetFileSize(this.FullName);
                    }
                }

                return _length;
            }
            protected set {
                _length = value;
            }
        }

        /// <summary>
        /// The last write time of the object. DateTime.MinValue means
        /// that is hasn't been loaded.
        /// </summary>
        protected DateTime _lastWriteTime = DateTime.MinValue;
        /// <summary>
        /// Last modification time
        /// </summary>
        public virtual DateTime LastWriteTime {
            get {
                if (_lastWriteTime == DateTime.MinValue) {
                    this.GetInfo();
                }

                return _lastWriteTime;
            }

            protected set { _lastWriteTime = value; }
        }

        /// <summary>
        /// Tries to load the object information
        /// </summary>
        protected void GetInfo() {
            if (this.Client.HasCapability(FtpCapability.MLST)) {
                FtpReply reply;

                if ((reply = this.Client.Execute("MLST {0}", this.FullName)).Success) {
                    foreach (string s in reply.InfoMessages.Split('\n')) {
                        if (s.StartsWith(" ")) { // MLST response begins with space according to internet draft
                            FtpListItem i = new FtpListItem(s, FtpListType.MLST);

                            if (i.Type == FtpObjectType.Directory) {
                                this.LastWriteTime = i.Modify;
                                return;
                            }
                            else if (i.Type == FtpObjectType.File) {
                                this.LastWriteTime = i.Modify;
                                this.Length = i.Size;
                                return;
                            }
                        }
                    }
                }
            }
            else {
                this.LastWriteTime = this.Client.GetLastWriteTime(this.FullName);
            }
        }

        /// <summary>
        /// Fixes directory separators
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected virtual string CleanPath(string path) {
            return System.Text.RegularExpressions.Regex.Replace(path.Replace('\\', '/'), @"/+", "/");
        }

        /// <summary>
        /// Returns the full path of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return this.FullName;
        }

        /// <summary>
        /// Cleanup an release resources
        /// </summary>
        public virtual void Dispose() {
            this._client = null;
            this._path = null;
            this._lastWriteTime = DateTime.MinValue;
            this._length = -1;
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public FtpFileSystemObject() { }

        /// <summary>
        /// Create new object lined to the specified client and pointing at the specified path
        /// </summary>
        /// <param name="client">The client to link this objec to</param>
        /// <param name="path">The full path of the remote object</param>
        public FtpFileSystemObject(FtpClient client, string path) {
            this.Client = client;
            this.FullName = this.CleanPath(path);
        }
    }
}
