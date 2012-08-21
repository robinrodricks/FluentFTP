using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace System.Net.FtpClient {
    /// <summary>
    /// FtpTransferProgress delegate
    /// </summary>
    /// <param name="e"></param>
    public delegate void FtpTransferProgress(FtpTransferInfo e);

    /// <summary>
    /// FtpClient library
    /// </summary>
    /// <example>
    ///     This example attempts to illustrate a file download.
    ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
    /// </example>
    /// <example>
    ///     This example attempts to illustrate a stream based file download.
    ///     <code source="..\Examples\DownloadStream\Program.cs" lang="cs"></code>
    /// </example>
    /// <example>
    ///     This example attempts to illustrate a file upload.
    ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
    /// </example>
    /// <example>
    ///     This example attempts to illustrate file listings
    ///     <code source="..\Examples\ListingFiles\Program.cs" lang="cs"></code>
    /// </example>
    public class FtpClient : FtpControlConnection {
        event FtpTransferProgress _transfer = null;
        /// <summary>
        /// Event fired from Download() and Upload() methods
        /// </summary>
        public event FtpTransferProgress TransferProgress {
            add { _transfer += value; }
            remove { _transfer -= value; }
        }

        FtpDirectory _currentDirectory = null;
        /// <summary>
        /// Gets the current working directory. Use the SetWorkingDirectory() method
        /// to change the working directory.
        /// </summary>
        public FtpDirectory CurrentDirectory {
            get {
                if (_currentDirectory == null) {
                    FtpReply reply;
                    Match m;

                    if (!(reply = this.Execute("PWD")).Success) {
                        throw new FtpCommandException(reply);
                    }

                    m = Regex.Match(reply.Message, "\"(.*)\"");
                    if (!m.Success || m.Groups.Count < 2) {
                        throw new FtpCommandException(reply.Code,
                            string.Format("Failed to parse current working directory from {0}", reply.Message));
                    }

                    this._currentDirectory = new FtpDirectory(this, m.Groups[1].Value);
                }

                return _currentDirectory;
            }

            private set {
                _currentDirectory = value;
            }
        }

        /// <summary>
        /// Gets the system type that we're connected to
        /// </summary>
        public string System {
            get {
                FtpReply reply;

                if (!(reply = this.Execute("SYST")).Success) {
                    throw new FtpCommandException(reply);
                }

                return reply.Message;
            }
        }

        /// <summary>
        /// Connect using the specified username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void Connect(string username, string password) {
            this.Username = username;
            this.Password = password;
            this.Connect();
        }

        /// <summary>
        /// Connect using the specified username and password to the specified server
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        public void Connect(string username, string password, string server) {
            this.Server = server;
            this.Connect(username, password);
        }

        /// <summary>
        /// Connect using the specified username and password to the specified server on the specified port
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public void Connect(string username, string password, string server, int port) {
            this.Port = port;
            this.Connect(username, password, server);
        }

        /// <summary>
        /// Connect asynchronously using the specified username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public IAsyncResult BeginConnect(string username, string password) {
            this.Username = username;
            this.Password = password;
            return this.BeginConnect();
        }

        /// <summary>
        /// Connect asynchronously using the specified username and password to the specified server
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        public IAsyncResult BeginConnect(string username, string password, string server) {
            this.Server = server;
            return this.BeginConnect(username, password);
        }

        /// <summary>
        /// Connect asynchronously using the specified username and password to the specified server on the specified port
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public IAsyncResult BeginConnect(string username, string password, string server, int port) {
            this.Port = port;
            return this.BeginConnect(username, password, server);
        }

        /// <summary>
        /// Connect to the server asynchronously
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public IAsyncResult BeginConnect(AsyncCallback callback, object state, string username, string password) {
            this.Username = username;
            this.Password = password;
            return this.BeginConnect(callback, state);
        }

        /// <summary>
        /// Connect to the server asynchronously
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public IAsyncResult BeginConnect(AsyncCallback callback, object state, string username, string password, string server) {
            this.Username = username;
            this.Password = password;
            this.Server = server;
            return this.BeginConnect(callback, state);
        }

        /// <summary>
        /// Connect to the server asynchronously
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public IAsyncResult BeginConnect(AsyncCallback callback, object state, string username, string password, string server, int port) {
            this.Username = username;
            this.Password = password;
            this.Server = server;
            this.Port = port;
            return this.BeginConnect(callback, state);
        }

        /// <summary>
        /// Sends the NoOp command. Does nothing other than send a command to the
        /// server and get a response.
        /// </summary>
        public void NoOp() {
            FtpReply reply;

            if (!(reply = this.Execute("NOOP")).Success) {
                throw new FtpCommandException(reply);
            }
        }

        /// <summary>
        /// Gets a raw directory listing of the current working directory. Prefers
        /// the MLSD command to LIST if it's available.
        /// </summary>
        /// <returns></returns>
        public string[] GetRawListing() {
            return this.GetRawListing(this.CurrentDirectory.FullName);
        }

        /// <summary>
        /// Returns a raw file listing, preferring to use the MLSD command
        /// over LIST if it is available
        /// </summary>
        /// <param name="path">The full or relative (to the current working directory) path</param>
        /// <returns>string array of the raw listing</returns>
        public string[] GetRawListing(string path) {
            if (this.HasCapability(FtpCapability.MLSD)) {
                return this.GetRawListing(path, FtpListType.MLSD);
            }

            return this.GetRawListing(path, FtpListType.LIST);
        }

        /// <summary>
        /// Returns a raw file listing using the specified LIST type
        /// </summary>
        /// <param name="path">The full or relative (to the current working directory) path</param>
        /// <param name="type"></param>
        /// <returns>string array of the raw listing</returns>
        public string[] GetRawListing(string path, FtpListType type) {
            List<string> lst = new List<string>();
            string cmd, buf;

            switch (type) {
                case FtpListType.LIST:
                    cmd = "LIST";
                    break;
                case FtpListType.MLSD:
                case FtpListType.MLST:
                    cmd = "MLSD";
                    break;
                default:
                    throw new NotImplementedException("The specified list type has not been implemented.");
            }

            using (FtpDataStream s = this.OpenDataStream(FtpDataType.ASCII)) {
                FtpReply reply;

                if (!(reply = s.Execute("{0} {1}", cmd, path)).Success) {
                    throw new FtpCommandException(reply);
                }

                while ((buf = s.ReadLine()) != null) {
                    lst.Add(buf);
                }
            }

            return lst.ToArray();
        }

        /// <summary>
        /// Gets a file listing, parses it, and returns an array of FtpListItem 
        /// objects that contain the parsed information. Supports MLSD/LIST (DOS and UNIX) formats.
        /// Most people should use the FtpDirectory/FtpFile classes which have more features than
        /// the objects returned from this method.
        /// </summary>
        /// <returns></returns>
        /// <example>
        ///     This example attempts to illustrate file listings
        ///     <code source="..\Examples\ListingFiles\Program.cs" lang="cs"></code>
        /// </example>
        public FtpListItem[] GetListing() {
            return this.GetListing(this.CurrentDirectory.FullName);
        }

        /// <summary>
        /// Gets a file listing, parses it, and returns an array of FtpListItem 
        /// objects that contain the parsed information. Supports MLSD/LIST (DOS and UNIX) formats.
        /// Most people should use the FtpDirectory/FtpFile classes which have more features than
        /// the objects returned from this method.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <example>
        ///     This example attempts to illustrate file listings
        ///     <code source="..\Examples\ListingFiles\Program.cs" lang="cs"></code>
        /// </example>
        public FtpListItem[] GetListing(string path) {
            if (this.HasCapability(FtpCapability.MLSD)) {
                return this.GetListing(path, FtpListType.MLSD);
            }

            return this.GetListing(path, FtpListType.LIST);
        }

        /// <summary>
        /// Gets a file listing, parses it, and returns an array of FtpListItem 
        /// objects that contain the parsed information. Supports MLSD/LIST (DOS and UNIX) formats.
        /// Most people should use the FtpDirectory/FtpFile classes which have more features than
        /// the objects returned from this method. Please note that Date/Time formats in LIST
        /// style listings vary greatly. We try to parse the last write time but we make no
        /// guarntee that it will succeed. If the FtpListItem.Modify == DateTime.MinValue it
        /// means the parsers failed. You can then use FtpClient.GetLastWriteTime() to try to
        /// retrieve the modification time. In some edge cases the parsed date from LIST formats may 
        /// be incorrect. If you encounter such a case please report it to the developers and include
        /// a copy of the raw listing that produced the invalid dates. If you care about the accuracy
        /// of the last write time and you know LIST is being used instead of MLSD you should use
        /// FtpClient.GetLastWriteTime to load the modification time. Example:
        /// 
        /// if(!client.HasCapability(FtpCapability.MLSD) {
        ///   listItem.Modify = client.GetLastWriteTime(full_or_relative_path_to_file);
        /// };
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <example>
        ///     This example attempts to illustrate file listings
        ///     <code source="..\Examples\ListingFiles\Program.cs" lang="cs"></code>
        /// </example>
        public FtpListItem[] GetListing(string path, FtpListType type) {
            return FtpListItem.ParseList(this.GetRawListing(path, type), type);
        }

        /// <summary>
        /// Set permissions on the specified object using the chmod command. Some server may not
        /// support chmod so be prepared to handle the subsequent FtpCommandException that may 
        /// be thrown.
        /// </summary>
        /// <param name="path">Path of the object to change the permissions on</param>
        /// <param name="user">Permissions for the user that owns the object</param>
        /// <param name="group">Permissions for the group the object belongs to</param>
        /// <param name="others">Permissions for other users on the system</param>
        public void SetPermissions(string path, FtpPermission user, FtpPermission group, FtpPermission others) {
            this.SetPermissions(path, (uint)user, (uint)group, (uint)others);
        }

        /// <summary>
        /// Set permissions on the specified object using the chmod command. Some server may not
        /// support chmod so be prepared to handle the subsequent FtpCommandException that may 
        /// be thrown.
        /// </summary>
        /// <param name="path">Path of the object to change the permissions on</param>
        /// <param name="user">Permissions for the user that owns the object</param>
        /// <param name="group">Permissions for the group the object belongs to</param>
        /// <param name="others">Permissions for other users on the system</param>
        public void SetPermissions(string path, uint user, uint group, uint others) {
            this.SetPermissions(path, string.Format("{0}{1}{2}", user, group, others));
        }

        /// <summary>
        /// Set permissions on the specified object using the chmod command. Some server may not
        /// support chmod so be prepared to handle the subsequent FtpCommandException that may 
        /// be thrown.
        /// </summary>
        /// <param name="path">Path of the object to change the permissions on</param>
        /// <param name="mode">3 digit mode of the object</param>
        public void SetPermissions(string path, string mode) {
            FtpReply reply;

            if (!(reply = this.Execute("SITE CHMOD {0} {1}", mode, path)).Success)
                throw new FtpCommandException(reply);
        }

        /// <summary>
        /// Changes the current working directory
        /// </summary>
        /// <param name="path">The full or relative (to the current directory) path</param>
        public void SetWorkingDirectory(string path) {
            FtpReply reply;

            if (!(reply = this.Execute("CWD {0}", path)).Success) {
                throw new FtpCommandException(reply);
            }

            this.CurrentDirectory = null;
        }

        /// <summary>
        /// Gets the last write time if the server supports the MDTM command. If the
        /// server does not support the MDTM NotImplementedException is thrown.
        /// </summary>
        /// <param name="path">The full or relative (to the current working directory) path</param>
        /// <returns>DateTime/DateTime.MinValue if there was a problem parsing the date</returns>
        public DateTime GetLastWriteTime(string path) {
            FtpReply reply;

            if (!this.HasCapability(FtpCapability.MDTM)) {
                throw new NotImplementedException("The connected server does not support the MDTM command.");
            }

            if ((reply = this.Execute("MDTM {0}", path)).Success) {
                return this.ParseLastWriteTime(reply.Message);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Parses the last write time values from the server
        /// </summary>
        /// <param name="mdtm">The string value to parse</param>
        /// <returns>A DateTime object representing what was parsed, DateTime.MinValue if there was a failure</returns>
        protected DateTime ParseLastWriteTime(string mdtm) {
            string[] formats = new string[] { "yyyyMMddHHmmss", "yyyyMMddHHmmss.fff" };
            DateTime modify = DateTime.MinValue;

            if (!DateTime.TryParseExact(mdtm, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out modify)) {
                modify = DateTime.MinValue;
            }

            return modify;
        }

        /// <summary>
        /// Removes the specified directory
        /// </summary>
        /// <param name="path">The full or relative (to the current working directory) path</param>
        public void RemoveDirectory(string path) {
            FtpReply reply;

            if (!(reply = this.Execute("RMD {0}", path)).Success) {
                throw new FtpCommandException(reply);
            }
        }

        /// <summary>
        /// Removes the specified file
        /// </summary>
        /// <param name="path">The full or relative (to the current working directory) path</param>
        public void RemoveFile(string path) {
            FtpReply reply;

            if (!(reply = this.Execute("DELE {0}", path)).Success) {
                throw new FtpCommandException(reply);
            }
        }

        /// <summary>
        /// Creates the specified directory
        /// </summary>
        /// <param name="path">The full or relative (to the current working directory) path</param>
        public void CreateDirectory(string path) {
            FtpReply reply;

            if (!(reply = this.Execute("MKD {0}", path)).Success) {
                throw new FtpCommandException(reply);
            }
        }

        /// <summary>
        /// Gets an FTP list item representing the specified file system object
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public FtpListItem GetObjectInfo(string path) {
            if (this.HasCapability(FtpCapability.MLST)) {
                FtpReply reply;

                if ((reply = this.Execute("MLST {0}", path)).Success) {
                    return new FtpListItem(reply.InfoMessages.Split('\n'), FtpListType.MLST);
                }
            }
            else {
                string directoryName = Path.GetDirectoryName(path.Replace("\\", "/").TrimEnd('/')).Replace("\\", "/");
                string fileName = Path.GetFileName(path.Replace("\\", "/").TrimEnd('/')).Replace("\\", "/");

                foreach (FtpListItem l in this.GetListing(directoryName)) {
                    if (l.Name == fileName) {
                        return l;
                    }
                }
            }

            return new FtpListItem();
        }

        /// <summary>
        /// Checks if the specified directory exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool DirectoryExists(string path) {
            try {
                // if they ask if "/" exists return true because
                // it does.
                if (path.Replace("\\", "/").TrimEnd('/').Length == 0)
                    return true;

                return this.GetObjectInfo(path).Type == FtpObjectType.Directory;
            }
            catch (FtpCommandException) {
                // a command exception can be thrown if the
                // root directory does not exist
                return false;
            }
        }

        /// <summary>
        /// Checks if the specified file exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool FileExists(string path) {
            try {
                return this.GetObjectInfo(path).Type == FtpObjectType.File;
            }
            catch (FtpCommandException) {
                // a command exception can be thrown if the
                // root directory does not exist
                return false;
            }
        }

        /// <summary>
        /// Renames the specified object
        /// </summary>
        /// <param name="from">The full or relative (to the current working directory) path of the existing file</param>
        /// <param name="to">The full or relative (to the current working directory) path of the new file</param>
        public void Rename(string from, string to) {
            try {
                FtpReply reply;

                this.LockControlConnection();

                if (!(reply = this.Execute("RNFR {0}", from)).Success) {
                    throw new FtpCommandException(reply);
                }

                if (!(reply = this.Execute("RNTO {0}", to)).Success) {
                    throw new FtpCommandException(reply);
                }
            }
            finally {
                this.UnlockControlConnection();
            }
        }

        /// <summary>
        /// Fires the TransferProgress event
        /// </summary>
        /// <param name="e"></param>
        protected void OnTransferProgress(FtpTransferInfo e) {
            if (_transfer != null) {
                _transfer(e);
            }
        }

        /// <summary>
        /// Downloads a file from the server to the current working directory
        /// </summary>
        /// <param name="remote">The full or relative path to the remote file</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote) {
            string local = string.Format(@"{0}\{1}",
                Environment.CurrentDirectory, Path.GetFileName(remote));
            this.Download(remote, local);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="local"></param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote, string local) {
            this.Download(remote, local, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">The remote file to download</param>
        /// <param name="ostream">The stream to download the file to</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote, Stream ostream) {
            this.Download(new FtpFile(this, remote), ostream, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote, string local, long rest) {
            this.Download(remote, local, FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="ostream">The stream to download the file to</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote, Stream ostream, long rest) {
            this.Download(new FtpFile(this, remote), ostream, FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote, string local, FtpDataType datatype) {
            this.Download(remote, local, datatype, 0);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="ostream">The stream to download the file to</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote, Stream ostream, FtpDataType datatype) {
            this.Download(new FtpFile(this, remote), ostream, datatype, 0);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(string remote, string local, FtpDataType datatype, long rest) {
            this.Download(new FtpFile(this, remote), local, datatype, rest);
        }

        /// <summary>
        /// Downloads a file from the server to the current working directory
        /// </summary>
        /// <param name="remote"></param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote) {
            this.Download(remote, string.Format(@"{0}\{1}",
                Environment.CurrentDirectory, remote.Name));
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="local"></param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, string local) {
            this.Download(remote, local, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="ostream">The stream to write the file to</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, Stream ostream) {
            this.Download(remote, ostream, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, string local, long rest) {
            this.Download(remote, local, FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="ostream">Local path of the file</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, Stream ostream, long rest) {
            this.Download(remote, ostream, FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, string local, FtpDataType datatype) {
            this.Download(remote, local, datatype, 0);
        }

        /// <summary>
        /// Downloads a file from the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="ostream">The stream to download the file to</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, Stream ostream, FtpDataType datatype) {
            this.Download(remote, ostream, datatype, 0);
        }

        /// <summary>
        /// Downloads the specified file from the server
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="local"></param>
        /// <param name="datatype"></param>
        /// <param name="rest"></param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, string local, FtpDataType datatype, long rest) {
            using (FileStream ostream = new FileStream(local, FileMode.OpenOrCreate, FileAccess.Write)) {
                try {
                    this.Download(remote, ostream, datatype, rest);
                }
                finally {
                    ostream.Close();
                }
            }
        }

        /// <summary>
        /// Downloads the remote file to the specified stream
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="ostream"></param>
        /// <param name="datatype"></param>
        /// <param name="rest"></param>
        /// <example>
        ///     <code source="..\Examples\Download\Program.cs" lang="cs"></code>
        /// </example>
        public void Download(FtpFile remote, Stream ostream, FtpDataType datatype, long rest) {
            long size = 0;
            long total = 0;
            int read = 0;

            if (remote == null) {
                throw new ArgumentException("remote is null");
            }

            if (ostream == null) {
                throw new ArgumentException("ostream is null");
            }

            if (!ostream.CanWrite) {
                throw new ArgumentException("ostream is not writable");
            }

            if (rest > 0 && ostream.CanSeek) { // set reset position
                ostream.Seek(rest, SeekOrigin.Begin);
                total = rest;
            }
            else if (!ostream.CanSeek) {
                rest = 0;
            }

            try {
                using (FtpDataStream ch = this.OpenFile(remote.FullName, datatype, FtpFileAccess.Read, rest)) {
                    byte[] buf = new byte[ch.ReceiveBufferSize];
                    DateTime start = DateTime.Now;
                    FtpTransferInfo e = null;

                    size = ch.Length;

                    while ((read = ch.Read(buf, 0, buf.Length)) > 0) {
                        ostream.Write(buf, 0, read);
                        total += read;
                        e = new FtpTransferInfo(FtpTransferType.Download, remote.FullName, size, rest, total, start, false);

                        this.OnTransferProgress(e);
                        if (e.Cancel) {
                            break;
                        }
                    }

                    // fire one more time to let event handler know that the transfer is complete
                    this.OnTransferProgress(new FtpTransferInfo(FtpTransferType.Download, remote.FullName,
                        size, rest, total, start, true));
                }
            }
            finally {
                ostream.Flush();
            }
        }

        /// <summary>
        /// Uploads a file to the server in the current working directory
        /// </summary>
        /// <param name="local"></param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local) {
            string remote = string.Format("{0}/{1}",
                this.CurrentDirectory.FullName,
                Path.GetFileName(local));
            this.Upload(local, remote);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="local"></param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, string remote) {
            this.Upload(local, remote, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream"></param>
        /// <param name="remote"></param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, string remote) {
            this.Upload(istream, new FtpFile(this, remote), FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, string remote, long rest) {
            this.Upload(local, remote, FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream">Stream to read the file from</param>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, string remote, long rest) {
            this.Upload(istream, new FtpFile(this, remote), FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, string remote, FtpDataType datatype) {
            this.Upload(local, remote, datatype, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream">The stream to read the file from</param>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, string remote, FtpDataType datatype) {
            this.Upload(istream, new FtpFile(this, remote), datatype, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Local path of the file</param>
        /// <param name="local">Remote path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, string remote, FtpDataType datatype, long rest) {
            this.Upload(local, new FtpFile(this, remote), datatype, rest);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="local"></param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, FtpFile remote) {
            this.Upload(local, remote, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream">Stream to read the file from</param>
        /// <param name="remote"></param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, FtpFile remote) {
            this.Upload(istream, remote, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, FtpFile remote, long rest) {
            this.Upload(local, remote, FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream">The file to upload</param>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, FtpFile remote, long rest) {
            this.Upload(istream, remote, FtpDataType.Binary, rest);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, FtpFile remote, FtpDataType datatype) {
            this.Upload(local, remote, datatype, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream">The stream to upload</param>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, FtpFile remote, FtpDataType datatype) {
            this.Upload(istream, remote, datatype, 0);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Local path of the file</param>
        /// <param name="local">Remote path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <param name="rest">Resume location</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, FtpFile remote, FtpDataType datatype, long rest) {
            using (FileStream istream = new FileStream(local, FileMode.Open, FileAccess.Read)) {
                try {
                    this.Upload(istream, remote, datatype, rest);
                }
                finally {
                    istream.Close();
                }
            }
        }

        /// <summary>
        /// Uploads a stream to the specified remote file
        /// </summary>
        /// <param name="istream"></param>
        /// <param name="remote"></param>
        /// <param name="datatype"></param>
        /// <param name="rest"></param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, FtpFile remote, FtpDataType datatype, long rest) {
            long size = 0;
            long total = 0;
            int read = 0;

            if (istream == null) {
                throw new ArgumentException("istream is null");
            }

            if (remote == null) {
                throw new ArgumentException("remote is null");
            }

            if (!istream.CanRead) {
                throw new ArgumentException("istream is not readable");
            }

            if (istream.CanSeek) {
                size = istream.Length;

                if (rest > 0) { // set resume position
                    istream.Seek(rest, SeekOrigin.Begin);
                    total = rest;
                }
            }
            else {
                rest = 0;
            }

            using (FtpDataStream ch = this.OpenFile(remote.FullName, datatype, FtpFileAccess.Write, rest)) {
                byte[] buf = new byte[ch.SendBufferSize];
                DateTime start = DateTime.Now;
                FtpTransferInfo e;

                while ((read = istream.Read(buf, 0, buf.Length)) > 0) {
                    ch.Write(buf, 0, read);
                    total += read;
                    e = new FtpTransferInfo(FtpTransferType.Upload, remote.FullName, size, rest, total, start, false);

                    this.OnTransferProgress(e);
                    if (e.Cancel) {
                        break;
                    }
                }

                // fire one more time to let event handler know the transfer is complete
                this.OnTransferProgress(new FtpTransferInfo(FtpTransferType.Upload, remote.FullName,
                    size, rest, total, start, true));
            }
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="append">Append to the remote file if it exists</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, string remote, bool append) {
            this.Upload(local, remote, FtpDataType.Binary, append);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream">Stream to read the file from</param>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="append">Append to the remote file if it exists</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, string remote, bool append) {
            this.Upload(istream, new FtpFile(this, remote), FtpDataType.Binary, append);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Local path of the file</param>
        /// <param name="local">Remote path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <param name="append">Append to the remote file if it exists</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, string remote, FtpDataType datatype, bool append) {
            this.Upload(local, new FtpFile(this, remote), datatype, append);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="local">Local path of the file</param>
        /// <param name="append">Append to the remote file if it exists</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, FtpFile remote, bool append) {
            this.Upload(local, remote, FtpDataType.Binary, append);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="istream">The file to upload</param>
        /// <param name="remote">Remote path of the file</param>
        /// <param name="append">Append to the remote file if it exists</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, FtpFile remote, bool append) {
            this.Upload(istream, remote, FtpDataType.Binary, append);
        }

        /// <summary>
        /// Uploads a file to the server
        /// </summary>
        /// <param name="remote">Local path of the file</param>
        /// <param name="local">Remote path of the file</param>
        /// <param name="datatype">ASCII/Binary</param>
        /// <param name="append">Append to the remote file if it exists</param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(string local, FtpFile remote, FtpDataType datatype, bool append) {
            using (FileStream istream = new FileStream(local, FileMode.Open, FileAccess.Read)) {
                try {
                    this.Upload(istream, remote, datatype, append);
                }
                finally {
                    istream.Close();
                }
            }
        }

        /// <summary>
        /// Uploads a stream to the specified remote file
        /// </summary>
        /// <param name="istream"></param>
        /// <param name="remote"></param>
        /// <param name="datatype"></param>
        /// <param name="append"></param>
        /// <example>
        ///     <code source="..\Examples\Upload\Program.cs" lang="cs"></code>
        /// </example>
        public void Upload(Stream istream, FtpFile remote, FtpDataType datatype, bool append) {
            long size = 0;
            long total = 0;
            int read = 0;
            long rest = 0;

            if (istream == null) {
                throw new ArgumentException("istream is null");
            }

            if (remote == null) {
                throw new ArgumentException("remote is null");
            }

            if (!istream.CanRead) {
                throw new ArgumentException("istream is not readable");
            }

            if (istream.CanSeek) {
                size = istream.Length;
            }

            rest = istream.Position;

            using (FtpDataStream ch = this.OpenFile(remote.FullName, datatype, append ? FtpFileAccess.Append : FtpFileAccess.Write)) {
                byte[] buf = new byte[ch.SendBufferSize];
                DateTime start = DateTime.Now;
                FtpTransferInfo e;

                while ((read = istream.Read(buf, 0, buf.Length)) > 0) {
                    ch.Write(buf, 0, read);
                    total += read;
                    e = new FtpTransferInfo(FtpTransferType.Upload, remote.FullName, size, rest, total, start, false);

                    this.OnTransferProgress(e);
                    if (e.Cancel) {
                        break;
                    }
                }

                // fire one more time to let event handler know the transfer is complete
                this.OnTransferProgress(new FtpTransferInfo(FtpTransferType.Upload, remote.FullName,
                    size, rest, total, start, true));
            }
        }

        /// <summary>
        /// Creates a new isntance of an FtpClient
        /// </summary>
        public FtpClient()
            : base() {
        }

        /// <summary>
        /// Creates a new isntance of an FtpClient
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public FtpClient(string username, string password)
            : this() {
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Creates a new isntance of an FtpClient
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        public FtpClient(string username, string password, string server)
            : this(username, password) {
            this.Server = server;
        }

        /// <summary>
        /// Creates a new isntance of an FtpClient
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public FtpClient(string username, string password, string server, int port)
            : this(username, password, server) {
            this.Port = port;
        }

        /// <summary>
        /// Initalizes a new FtpClient object based on the given URI
        /// </summary>
        /// <param name="uri">URI to parse</param>
        public FtpClient(Uri uri) {
            if (uri == null)
                throw new ArgumentNullException("uri");
            if (!IsFtpUriScheme(uri))
                throw new ArgumentException("Only FTP or FTPS URIs are supported.", "uri");

            var uriBuilder = new UriBuilder(uri);

            Username = uriBuilder.UserName;
            Password = uriBuilder.Password;
            Server = uriBuilder.Host;
            Port = uriBuilder.Port;

            if (uriBuilder.Scheme.Equals(UriSchemeFtps))
                SslMode = FtpSslMode.Explicit;
            else
                SslMode = FtpSslMode.None;
        }

        static bool IsFtpUriScheme(Uri uri) {
            var ftpSchemes = new StringCollection { Uri.UriSchemeFtp, UriSchemeFtps };
            return ftpSchemes.Contains(uri.Scheme);
        }

        const string UriSchemeFtps = "ftps";
    }
}
