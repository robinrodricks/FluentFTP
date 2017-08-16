using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FluentFTP
{
    public interface IFtpClient
    {
        /// <summary>
        /// Deletes a file on the server
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <example><code source="..\Examples\DeleteFile.cs" lang="cs" /></example>
        void DeleteFile(string path);

        /// <summary>
        /// Deletes the specified directory and all its contents.
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        void DeleteDirectory(string path);

        /// <summary>
        /// Deletes the specified directory and all its contents.
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="options">Useful to delete hidden files or dot-files.</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        void DeleteDirectory(string path, FtpListOption options);

        /// <summary>
        /// Tests if the specified directory exists on the server. This
        /// method works by trying to change the working directory to
        /// the path specified. If it succeeds, the directory is changed
        /// back to the old working directory and true is returned. False
        /// is returned otherwise and since the CWD failed it is assumed
        /// the working directory is still the same.
        /// </summary>
        /// <param name="path">The path of the directory</param>
        /// <returns>True if it exists, false otherwise.</returns>
        /// <example><code source="..\Examples\DirectoryExists.cs" lang="cs" /></example>
        bool DirectoryExists(string path);

        /// <summary>
        /// Checks if a file exists on the server.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <returns>True if the file exists</returns>
        /// <example><code source="..\Examples\FileExists.cs" lang="cs" /></example>
        bool FileExists(string path);

        /// <summary>
        /// Creates a directory on the server. If the preceding
        /// directories do not exist, then they are created.
        /// </summary>
        /// <param name="path">The full or relative path to the new remote directory</param>
        /// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
        void CreateDirectory(string path);

        /// <summary>
        /// Creates a directory on the server
        /// </summary>
        /// <param name="path">The full or relative path to the new remote directory</param>
        /// <param name="force">Try to force all non-existent pieces of the path to be created</param>
        /// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
        void CreateDirectory(string path, bool force);

        /// <summary>
        /// Renames an object on the remote file system.
        /// Low level method that should NOT be used in most cases. Prefer MoveFile() and MoveDirectory().
        /// Throws exceptions if the file does not exist, or if the destination file already exists.
        /// </summary>
        /// <param name="path">The full or relative path to the object</param>
        /// <param name="dest">The new full or relative path including the new name of the object</param>
        /// <example><code source="..\Examples\Rename.cs" lang="cs" /></example>
        void Rename(string path, string dest);

        /// <summary>
        /// Moves a file on the remote file system from one directory to another.
        /// Always checks if the source file exists. Checks if the dest file exists based on the `existsMode` parameter.
        /// Only throws exceptions for critical errors.
        /// </summary>
        /// <param name="path">The full or relative path to the object</param>
        /// <param name="dest">The new full or relative path including the new name of the object</param>
        /// <param name="existsMode">Should we check if the dest file exists? And if it does should we overwrite/skip the operation?</param>
        bool MoveFile(string path, string dest, FtpExists existsMode = FtpExists.Overwrite);

        /// <summary>
        /// Moves a directory on the remote file system from one directory to another.
        /// Always checks if the source directory exists. Checks if the dest directory exists based on the `existsMode` parameter.
        /// Only throws exceptions for critical errors.
        /// </summary>
        /// <param name="path">The full or relative path to the object</param>
        /// <param name="dest">The new full or relative path including the new name of the object</param>
        /// <param name="existsMode">Should we check if the dest directory exists? And if it does should we overwrite/skip the operation?</param>
        bool MoveDirectory(string path, string dest, FtpExists existsMode = FtpExists.Overwrite);

        /// <summary>
        /// Modify the permissions of the given file/folder.
        /// Only works on *NIX systems, and not on Windows/IIS servers.
        /// Only works if the FTP server supports the SITE CHMOD command
        /// (requires the CHMOD extension to be installed and enabled).
        /// Throws FtpCommandException if there is an issue.
        /// </summary>
        /// <param name="path">The full or relative path to the item</param>
        /// <param name="permissions">The permissions in CHMOD format</param>
        void SetFilePermissions(string path, int permissions);

        /// <summary>
        /// Modify the permissions of the given file/folder.
        /// Only works on *NIX systems, and not on Windows/IIS servers.
        /// Only works if the FTP server supports the SITE CHMOD command
        /// (requires the CHMOD extension to be installed and enabled).
        /// Throws FtpCommandException if there is an issue.
        /// </summary>
        /// <param name="path">The full or relative path to the item</param>
        /// <param name="permissions">The permissions in CHMOD format</param>
        void Chmod(string path, int permissions);

        /// <summary>
        /// Modify the permissions of the given file/folder.
        /// Only works on *NIX systems, and not on Windows/IIS servers.
        /// Only works if the FTP server supports the SITE CHMOD command
        /// (requires the CHMOD extension to be installed and enabled).
        /// Throws FtpCommandException if there is an issue.
        /// </summary>
        /// <param name="path">The full or relative path to the item</param>
        /// <param name="owner">The owner permissions</param>
        /// <param name="group">The group permissions</param>
        /// <param name="other">The other permissions</param>
        void SetFilePermissions(string path, FtpPermission owner, FtpPermission group, FtpPermission other);

        /// <summary>
        /// Modify the permissions of the given file/folder.
        /// Only works on *NIX systems, and not on Windows/IIS servers.
        /// Only works if the FTP server supports the SITE CHMOD command
        /// (requires the CHMOD extension to be installed and enabled).
        /// Throws FtpCommandException if there is an issue.
        /// </summary>
        /// <param name="path">The full or relative path to the item</param>
        /// <param name="owner">The owner permissions</param>
        /// <param name="group">The group permissions</param>
        /// <param name="other">The other permissions</param>
        void Chmod(string path, FtpPermission owner, FtpPermission group, FtpPermission other);

        /// <summary>
        /// Retrieve the permissions of the given file/folder as an FtpListItem object with all "Permission" properties set.
        /// Throws FtpCommandException if there is an issue.
        /// Returns null if the server did not specify a permission value.
        /// Use `GetChmod` if you required the integer value instead.
        /// </summary>
        /// <param name="path">The full or relative path to the item</param>
        FtpListItem GetFilePermissions(string path);

        /// <summary>
        /// Retrieve the permissions of the given file/folder as an integer in the CHMOD format.
        /// Throws FtpCommandException if there is an issue.
        /// Returns 0 if the server did not specify a permission value.
        /// Use `GetFilePermissions` if you required the permissions in the FtpPermission format.
        /// </summary>
        /// <param name="path">The full or relative path to the item</param>
        int GetChmod(string path);

        /// <summary>
        /// Recursively dereferences a symbolic link. See the
        /// MaximumDereferenceCount property for controlling
        /// how deep this method will recurse before giving up.
        /// </summary>
        /// <param name="item">The symbolic link</param>
        /// <returns>FtpListItem, null if the link can't be dereferenced</returns>
        /// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
        FtpListItem DereferenceLink(FtpListItem item);

        /// <summary>
        /// Recursively dereferences a symbolic link
        /// </summary>
        /// <param name="item">The symbolic link</param>
        /// <param name="recMax">The maximum depth of recursion that can be performed before giving up.</param>
        /// <returns>FtpListItem, null if the link can't be dereferenced</returns>
        /// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
        FtpListItem DereferenceLink(FtpListItem item, int recMax);

        /// <summary>
        /// Sets the work directory on the server
        /// </summary>
        /// <param name="path">The path of the directory to change to</param>
        /// <example><code source="..\Examples\SetWorkingDirectory.cs" lang="cs" /></example>
        void SetWorkingDirectory(string path);

        /// <summary>
        /// Gets the current working directory
        /// </summary>
        /// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
        /// <example><code source="..\Examples\GetWorkingDirectory.cs" lang="cs" /></example>
        string GetWorkingDirectory();

        /// <summary>
        /// Gets the size of a remote file
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <returns>-1 if the command fails, otherwise the file size</returns>
        /// <example><code source="..\Examples\GetFileSize.cs" lang="cs" /></example>
        long GetFileSize(string path);

        /// <summary>
        /// Gets the modified time of a remote file
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <param name="type">Return the date in local timezone or UTC?  Use FtpDate.Original to disable timezone conversion.</param>
        /// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
        /// <example><code source="..\Examples\GetModifiedTime.cs" lang="cs" /></example>
        DateTime GetModifiedTime(string path, FtpDate type = FtpDate.Original);

        /// <summary>
        /// Changes the modified time of a remote file
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <param name="date">The new modified date/time value</param>
        /// <param name="type">Is the date provided in local timezone or UTC? Use FtpDate.Original to disable timezone conversion.</param>
        void SetModifiedTime(string path, DateTime date, FtpDate type = FtpDate.Original);

        /// <summary>
        /// Gets a value indicating if this object has already been disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Flags specifying which versions of the internet protocol to
        /// support when making a connection. All addresses returned during
        /// name resolution are tried until a successful connection is made.
        /// You can fine tune which versions of the internet protocol to use
        /// by adding or removing flags here. I.e., setting this property
        /// to FtpIpVersion.IPv4 will cause the connection process to
        /// ignore IPv6 addresses. The default value is ANY version.
        /// </summary>
        FtpIpVersion InternetProtocolVersions { get; set; }

        /// <summary>
        /// Gets or sets the length of time in milliseconds
        /// that must pass since the last socket activity
        /// before calling <see cref="System.Net.Sockets.Socket.Poll"/> 
        /// on the socket to test for connectivity. 
        /// Setting this interval too low will
        /// have a negative impact on performance. Setting this
        /// interval to 0 disables Polling all together.
        /// The default value is 15 seconds.
        /// </summary>
        int SocketPollInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a test should be performed to
        /// see if there is stale (unrequested data) sitting on the socket. In some
        /// cases the control connection may time out but before the server closes
        /// the connection it might send a 4xx response that was unexpected and
        /// can cause synchronization errors with transactions. To avoid this
        /// problem the <see cref="o:Execute"/> method checks to see if there is any data
        /// available on the socket before executing a command. On Azure hosting
        /// platforms this check can cause an exception to be thrown. In order
        /// to work around the exception you can set this property to false
        /// which will skip the test entirely however doing so eliminates the
        /// best effort attempt of detecting such scenarios. See this thread
        /// for more details about the Azure problem:
        /// https://netftp.codeplex.com/discussions/535879
        /// </summary>
        bool StaleDataCheck { get; set; }

        /// <summary>
        /// Gets a value indicating if the connection is alive
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// When this value is set to true (default) the control connection
        /// is cloned and a new connection the server is established for the
        /// data channel operation. This is a thread safe approach to make
        /// asynchronous operations on a single control connection transparent
        /// to the developer.
        /// </summary>
        bool EnableThreadSafeDataConnections { get; set; }

        /// <summary>
        /// Gets or sets the text encoding being used when talking with the server. The default
        /// value is <see cref="System.Text.Encoding.ASCII"/> however upon connection, the client checks
        /// for UTF8 support and if it's there this property is switched over to
        /// <see cref="System.Text.Encoding.UTF8"/>. Manually setting this value overrides automatic detection
        /// based on the FEAT list; if you change this value it's always used
        /// regardless of what the server advertises, if anything.
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// The server to connect to
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// The port to connect to. If this value is set to 0 (Default) the port used
        /// will be determined by the type of SSL used or if no SSL is to be used it 
        /// will automatically connect to port 21.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Credentials used for authentication
        /// </summary>
        NetworkCredential Credentials { get; set; }

        /// <summary>
        /// Gets or sets a value that controls the maximum depth
        /// of recursion that <see cref="o:DereferenceLink"/> will follow symbolic
        /// links before giving up. You can also specify the value
        /// to be used as one of the overloaded parameters to the
        /// <see cref="o:DereferenceLink"/> method. The default value is 20. Specifying
        /// -1 here means indefinitely try to resolve a link. This is
        /// not recommended for obvious reasons (stack overflow).
        /// </summary>
        int MaximumDereferenceCount { get; set; }

        /// <summary>
        /// Client certificates to be used in SSL authentication process
        /// </summary>
        X509CertificateCollection ClientCertificates { get; }

        /// <summary>
        /// Delegate used for resolving local address, used for active data connections
        /// This can be used in case you're behind a router, but port forwarding is configured to forward the
        /// ports from your router to your internal IP. In that case, we need to send the router's IP instead of our internal IP.
        /// See example: FtpClient.GetPublicIP -> This uses Ipify api to find external IP
        /// </summary>
        Func<string> AddressResolver { get; set; }

        /// <summary>
        /// Ports used for Active Data Connection
        /// </summary>
        IEnumerable<int> ActivePorts { get; set; }

        /// <summary>
        /// Data connection type, default is AutoPassive which tries
        /// a connection with EPSV first and if it fails then tries
        /// PASV before giving up. If you know exactly which kind of
        /// connection you need you can slightly increase performance
        /// by defining a specific type of passive or active data
        /// connection here.
        /// </summary>
        FtpDataConnectionType DataConnectionType { get; set; }

        /// <summary>
        /// Disconnect from the server without sending QUIT. This helps
        /// work around IOExceptions caused by buggy connection resets
        /// when closing the control connection.
        /// </summary>
        bool UngracefullDisconnection { get; set; }

        /// <summary>
        /// Gets or sets the length of time in milliseconds to wait for a connection 
        /// attempt to succeed before giving up. Default is 15000 (15 seconds).
        /// </summary>
        int ConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets the length of time wait in milliseconds for data to be
        /// read from the underlying stream. The default value is 15000 (15 seconds).
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets the length of time in milliseconds for a data connection
        /// to be established before giving up. Default is 15000 (15 seconds).
        /// </summary>
        int DataConnectionConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets the length of time in milliseconds the data channel
        /// should wait for the server to send data. Default value is 
        /// 15000 (15 seconds).
        /// </summary>
        int DataConnectionReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if <see cref="System.Net.Sockets.SocketOptionName.KeepAlive"/> should be set on 
        /// the underlying stream's socket. If the connection is alive, the option is
        /// adjusted in real-time. The value is stored and the KeepAlive option is set
        /// accordingly upon any new connections. The value set here is also applied to
        /// all future data streams. It has no affect on cloned control connections or
        /// data connections already in progress. The default value is false.
        /// </summary>
        bool SocketKeepAlive { get; set; }

        /// <summary>
        /// Gets the server capabilities represented by flags
        /// </summary>
        FtpCapability Capabilities { get; }

        /// <summary>
        /// Get the hash types supported by the server, if any. This
        /// is a recent extension to the protocol that is not fully
        /// standardized and is not guaranteed to work. See here for
        /// more details:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </summary>
        FtpHashAlgorithm HashAlgorithms { get; }

        /// <summary>
        /// Type of SSL to use, or none. Default is none. Explicit is TLS, Implicit is SSL.
        /// </summary>
        FtpEncryptionMode EncryptionMode { get; set; }

        /// <summary>
        /// Indicates if data channel transfers should be encrypted. Only valid if <see cref="EncryptionMode"/>
        /// property is not equal to <see cref="FtpEncryptionMode.None"/>.
        /// </summary>
        bool DataConnectionEncryption { get; set; }

        /// <summary>
        /// Encryption protocols to use. Only valid if EncryptionMode property is not equal to <see cref="FtpEncryptionMode.None"/>.
        /// Default value is .NET Framework defaults from the <see cref="System.Net.Security.SslStream"/> class.
        /// </summary>
        SslProtocols SslProtocols { get; set; }

        /// <summary>
        /// Gets the type of system/server that we're
        /// connected to.
        /// </summary>
        string SystemType { get; }

        /// <summary> Gets the connection type </summary>
        string ConnectionType { get; }

        /// <summary>
        /// File listing parser to be used. 
        /// Automatically calculated based on the type of the server, unless changed.
        /// </summary>
        FtpParser ListingParser { get; set; }

        /// <summary>
        /// Culture used to parse file listings
        /// </summary>
        CultureInfo ListingCulture { get; set; }

        /// <summary>
        /// Time difference between server and client, in hours.
        /// If the server is located in New York and you are in London then the time difference is -5 hours.
        /// </summary>
        double TimeOffset { get; set; }

        /// <summary>
        /// Check if your server supports a recursive LIST command (LIST -R).
        /// If you know for sure that this is unsupported, set it to false.
        /// </summary>
        bool RecursiveList { get; set; }

        /// <summary>
        /// If true, increases performance of GetListing by reading multiple lines
        /// of the file listing at once. If false then GetListing will read file
        /// listings line-by-line. If GetListing is having issues with your server,
        /// set it to false.
        /// 
        /// The number of bytes read is based upon <see cref="BulkListingLength"/>.
        /// </summary>
        bool BulkListing { get; set; }

        /// <summary>
        /// Bytes to read during GetListing. Only honored if <see cref="BulkListing"/> is true.
        /// </summary>
        int BulkListingLength { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes transferred in a single chunk (a single FTP command).
        /// Used by <see cref="o:UploadFile"/>/<see cref="o:UploadFileAsync"/> and <see cref="o:DownloadFile"/>/<see cref="o:DownloadFileAsync"/>
        /// to transfer large files in multiple chunks.
        /// </summary>
        int TransferChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the retry attempts allowed when a verification failure occurs during download or upload.
        /// This value must be set to 1 or more.
        /// </summary>
        int RetryAttempts { get; set; }

        /// <summary>
        /// Rate limit for uploads in kbyte/s. Set this to 0 for unlimited speed.
        /// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
        /// </summary>
        uint UploadRateLimit { get; set; }

        /// <summary>
        /// Rate limit for downloads in kbytes/s. Set this to 0 for unlimited speed.
        /// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
        /// </summary>
        uint DownloadRateLimit { get; set; }

        /// <summary>
        /// Controls if the high-level API uploads files in Binary or ASCII mode.
        /// </summary>
        FtpDataType UploadDataType { get; set; }

        /// <summary>
        /// Controls if the high-level API downloads files in Binary or ASCII mode.
        /// </summary>
        FtpDataType DownloadDataType { get; set; }

        /// <summary>
        /// Event is fired to validate SSL certificates. If this event is
        /// not handled and there are errors validating the certificate
        /// the connection will be aborted.
        /// </summary>
        /// <example><code source="..\Examples\ValidateCertificate.cs" lang="cs" /></example>
        event FtpSslValidation ValidateCertificate;

        /// <summary>
        /// Disconnects from the server, releases resources held by this
        /// object.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The servers reply to the command</returns>
        /// <example><code source="..\Examples\Execute.cs" lang="cs" /></example>
        FtpReply Execute(string command);

        /// <summary>
        /// Retrieves a reply from the server. Do not execute this method
        /// unless you are sure that a reply has been sent, i.e., you
        /// executed a command. Doing so will cause the code to hang
        /// indefinitely waiting for a server reply that is never coming.
        /// </summary>
        /// <returns>FtpReply representing the response from the server</returns>
        /// <example><code source="..\Examples\BeginGetReply.cs" lang="cs" /></example>
        FtpReply GetReply();

        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this object has been disposed.</exception>
        /// <example><code source="..\Examples\Connect.cs" lang="cs" /></example>
        void Connect();

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Performs a bitwise and to check if the specified
        /// flag is set on the <see cref="Capabilities"/>  property.
        /// </summary>
        /// <param name="cap">The <see cref="FtpCapability"/> to check for</param>
        /// <returns>True if the feature was found, false otherwise</returns>
        bool HasFeature(FtpCapability cap);

        /// <summary>
        /// Disables UTF8 support and changes the Encoding property
        /// back to ASCII. If the server returns an error when trying
        /// to turn UTF8 off a FtpCommandException will be thrown.
        /// </summary>
        void DisableUTF8();

        /// <summary>
        /// Returns information about a file system object. Returns null if the server response can't
        /// be parsed or the server returns a failure completion code. The error for a failure
        /// is logged with FtpTrace. No exception is thrown on error because that would negate
        /// the usefulness of this method for checking for the existence of an object.
        /// </summary>
        /// <param name="path">The path of the file or folder</param>
        /// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
        /// <returns>A FtpListItem object</returns>
        FtpListItem GetObjectInfo(string path, bool dateModified = false);

        /// <summary>
        /// Gets a file listing from the server from the current working directory. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
        /// <returns>An array of FtpListItem objects</returns>
        /// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
        FtpListItem[] GetListing();

        /// <summary>
        /// Gets a file listing from the server. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
        /// <param name="path">The path of the directory to list</param>
        /// <returns>An array of FtpListItem objects</returns>
        /// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
        FtpListItem[] GetListing(string path);

        /// <summary>
        /// Gets a file listing from the server. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
        /// <param name="path">The path of the directory to list</param>
        /// <param name="options">Options that dictacte how a list is performed and what information is gathered.</param>
        /// <returns>An array of FtpListItem objects</returns>
        /// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
        FtpListItem[] GetListing(string path, FtpListOption options);

        /// <summary>
        /// Returns a file/directory listing using the NLST command.
        /// </summary>
        /// <returns>A string array of file and directory names if any were returned.</returns>
        string[] GetNameListing();

        /// <summary>
        /// Returns a file/directory listing using the NLST command.
        /// </summary>
        /// <param name="path">The path of the directory to list</param>
        /// <returns>A string array of file and directory names if any were returned.</returns>
        /// <example><code source="..\Examples\GetNameListing.cs" lang="cs" /></example>
        string[] GetNameListing(string path);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path, FtpDataType type);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path, FtpDataType type, bool checkIfFileExists);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="restart">Resume location</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path, FtpDataType type, long restart);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="restart">Resume location</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path, long restart);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="restart">Resume location</param>
        /// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path, long restart, bool checkIfFileExists);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="restart">Resume location</param>
        /// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path, FtpDataType type, long restart, bool checkIfFileExists);

        /// <summary>
        /// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
        Stream OpenWrite(string path);

        /// <summary>
        /// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
        Stream OpenWrite(string path, FtpDataType type);

        /// <summary>
        /// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
        Stream OpenWrite(string path, FtpDataType type, bool checkIfFileExists);

        /// <summary>
        /// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
        /// </summary>
        /// <param name="path">The full or relative path to the file to be opened</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
        Stream OpenAppend(string path);

        /// <summary>
        /// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
        /// </summary>
        /// <param name="path">The full or relative path to the file to be opened</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
        Stream OpenAppend(string path, FtpDataType type);

        /// <summary>
        /// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
        /// </summary>
        /// <param name="path">The full or relative path to the file to be opened</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
        Stream OpenAppend(string path, FtpDataType type, bool checkIfFileExists);

        /// <summary>
        /// Uploads the given file paths to a single folder on the server.
        /// All files are placed directly into the given folder regardless of their path on the local filesystem.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
        /// </summary>
        /// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
        /// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance,
        ///  but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
        /// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
        /// <param name="errorHandling">Used to determine how errors are handled</param>
        /// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
        /// <remarks>
        /// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
        /// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
        /// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
        /// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
        /// to propagate from this method.
        /// </remarks>
        int UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true,
            FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None);

        /// <summary>
        /// Uploads the given file paths to a single folder on the server.
        /// All files are placed directly into the given folder regardless of their path on the local filesystem.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
        /// </summary>
        /// <param name="localFiles">Files to be uploaded</param>
        /// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
        /// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
        /// <param name="errorHandling">Used to determine how errors are handled</param>
        /// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
        /// <remarks>
        /// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
        /// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
        /// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
        /// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
        /// to propagate from this method.
        /// </remarks>
        int UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true,
            FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None);

        /// <summary>
        /// Downloads the specified files into a local single directory.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// Same speed as <see cref="o:DownloadFile"/>.
        /// </summary>
        /// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
        /// <param name="remotePaths">The full or relative paths to the files on the server</param>
        /// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
        /// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
        /// <param name="errorHandling">Used to determine how errors are handled</param>
        /// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
        /// <remarks>
        /// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
        /// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
        /// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
        /// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
        /// to propagate from this method.
        /// </remarks>
        int DownloadFiles(string localDir, IEnumerable<string> remotePaths, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None,
            FtpError errorHandling = FtpError.None);

        /// <summary>
        /// Uploads the specified file directly onto the server.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="localPath">The full or relative path to the file on the local file system</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to  <see cref="FtpExists.NoCheck"/> for fastest performance 
        /// but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
        /// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
        /// <returns>If true then the file was uploaded, false otherwise.</returns>
        /// <remarks>
        /// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
        /// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
        /// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
        /// </remarks>
        bool UploadFile(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false,
            FtpVerify verifyOptions = FtpVerify.None);

        /// <summary>
        /// Uploads the specified stream as a file onto the server.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="fileStream">The full data of the file, as a stream</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance
        /// but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
        bool Upload(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false);

        /// <summary>
        /// Uploads the specified byte array as a file onto the server.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="fileData">The full data of the file, as a byte array</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance 
        /// but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
        bool Upload(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false);

        /// <summary>
        /// Downloads the specified file onto the local file system.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="localPath">The full or relative path to the file on the local file system</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
        /// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
        /// <returns>If true then the file was downloaded, false otherwise.</returns>
        /// <remarks>
        /// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
        /// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
        /// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
        /// </remarks>
        bool DownloadFile(string localPath, string remotePath, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None);

        /// <summary>
        /// Downloads the specified file into the specified stream.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <returns>If true then the file was downloaded, false otherwise.</returns>
        bool Download(Stream outStream, string remotePath);

        /// <summary>
        /// Downloads the specified file and return the raw byte array.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="outBytes">The variable that will receive the bytes.</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <returns>If true then the file was downloaded, false otherwise.</returns>
        bool Download(out byte[] outBytes, string remotePath);

        /// <summary>
        /// Gets the currently selected hash algorithm for the HASH command.
        /// </summary>
        /// <remarks>
        ///  This feature is experimental. See this link for details:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </remarks>
        /// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.NONE"/> if there was a problem.</returns>
        /// <example><code source="..\Examples\GetHashAlgorithm.cs" lang="cs" /></example>
        FtpHashAlgorithm GetHashAlgorithm();

        /// <summary>
        /// Sets the hash algorithm on the server to use for the HASH command. 
        /// </summary>
        /// <remarks>
        /// If you specify an algorithm not listed in <see cref="FtpClient.HashAlgorithms"/>
        /// a <see cref="NotImplementedException"/> will be thrown
        /// so be sure to query that list of Flags before
        /// selecting a hash algorithm. Support for the
        /// HASH command is experimental. Please see
        /// the following link for more details:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </remarks>
        /// <param name="type">Hash Algorithm</param>
        /// <exception cref="System.NotImplementedException">Thrown if the selected algorithm is not available on the server</exception>
        /// <example><code source="..\Examples\SetHashAlgorithm.cs" lang="cs" /></example>
        void SetHashAlgorithm(FtpHashAlgorithm type);

        /// <summary>
        /// Gets the hash of an object on the server using the currently selected hash algorithm. 
        /// </summary>
        /// <remarks>
        /// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
        /// property. You should confirm that it's not equal
        /// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
        /// otherwise the server trigger a <see cref="FtpCommandException"/>
        /// due to a lack of support for the HASH command. You can
        /// set the algorithm using the <see cref="SetHashAlgorithm(FluentFTP.FtpHashAlgorithm)"/> method and
        /// you can query the server for the current hash algorithm
        /// using the <see cref="GetHashAlgorithm()"/> method.
        /// 
        /// This feature is experimental and based on the following draft:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </remarks>
        /// <param name="path">Full or relative path of the object to compute the hash for.</param>
        /// <returns>The hash of the file.</returns>
        /// <exception cref="FtpCommandException">
        /// Thrown if the <see cref="HashAlgorithms"/> property is <see cref="FtpHashAlgorithm.NONE"/>, 
        /// the remote path does not exist, or the command cannot be executed.
        /// </exception>
        /// <exception cref="ArgumentException">Path argument is null</exception>
        /// <exception cref="NotImplementedException">Thrown when an unknown hash algorithm type is returned by the server</exception>
        /// <example><code source="..\Examples\GetHash.cs" lang="cs" /></example>
        FtpHash GetHash(string path);

        /// <summary>
        /// Retrieves a checksum of the given file using a checksum method that the server supports, if any. 
        /// </summary>
        /// <remarks>
        /// The algorithm used goes in this order:
        /// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithm"/>
        /// 2. MD5 / XMD5 commands
        /// 3. XSHA1 command
        /// 4. XSHA256 command
        /// 5. XSHA512 command
        /// 6. XCRC command
        /// </remarks>
        /// <param name="path">Full or relative path of the file to checksum</param>
        /// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
        /// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
        /// the underlying calls.</returns>
        /// <example><code source="..\Examples\GetChecksum.cs" lang="cs" /></example>
        /// <exception cref="FtpCommandException">The command fails</exception>
        FtpHash GetChecksum(string path);

        /// <summary>
        /// Gets the MD5 hash of the specified file using MD5. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the MD5 hash.</returns>
        /// <exception cref="FtpCommandException">The command fails</exception>
        string GetMD5(string path);

        /// <summary>
        /// Get the CRC value of the specified file. This is a non-standard extension of the protocol 
        /// and may throw a FtpCommandException if the server does not support it.
        /// </summary>
        /// <param name="path">The path of the file you'd like the server to compute the CRC value for.</param>
        /// <returns>The response from the server, typically the XCRC value. FtpCommandException thrown on error</returns>
        /// <exception cref="FtpCommandException">The command fails</exception>
        string GetXCRC(string path);

        /// <summary>
        /// Gets the MD5 hash of the specified file using XMD5. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the MD5 hash.</returns>
        /// <exception cref="FtpCommandException">The command fails</exception>
        string GetXMD5(string path);

        /// <summary>
        /// Gets the SHA-1 hash of the specified file using XSHA1. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the SHA-1 hash.</returns>
        /// <exception cref="FtpCommandException">The command fails</exception>
        string GetXSHA1(string path);

        /// <summary>
        /// Gets the SHA-256 hash of the specified file using XSHA256. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the SHA-256 hash.</returns>
        /// <exception cref="FtpCommandException">The command fails</exception>
        string GetXSHA256(string path);

        /// <summary>
        /// Gets the SHA-512 hash of the specified file using XSHA512. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the SHA-512 hash.</returns>
        /// <exception cref="FtpCommandException">The command fails</exception>
        string GetXSHA512(string path);
    }
}