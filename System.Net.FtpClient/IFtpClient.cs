using System.IO;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.FtpClient
{
    /// <summary>
    /// FTP Control Connection. Speaks the FTP protocol with the server and
    /// provides facilities for performing basic transactions.
    /// 
    /// Debugging problems with FTP transactions is much easier to do when
    /// you can see exactly what is sent to the server and the reply 
    /// System.Net.FtpClient gets in return. Please review the Debug example
    /// below for information on how to add TraceListeners for capturing
    /// the convorsation between System.Net.FtpClient and the server.
    /// </summary>
    /// <example>The following example illustrates how to assist in debugging
    /// System.Net.FtpClient by getting a transaction log from the server.
    /// <code source="..\Examples\Debug.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates adding a custom file
    /// listing parser in the event that you encounter a list format
    /// not already supported.
    /// <code source="..\Examples\CustomParser.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates how to validate
    /// a SSL certificate when using SSL/TLS.
    /// <code source="..\Examples\ValidateCertificate.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonsrates how to download a file.
    /// <code source="..\Examples\OpenRead.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates how to download a file
    /// using a URI object.
    /// <code source="..\Examples\OpenReadURI.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates how to upload a file.
    /// <code source="..\Examples\OpenWrite.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates how to upload a file
    /// using a URI object.
    /// <code source="..\Examples\OpenWriteURI.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates how to append to a file.
    /// <code source="..\Examples\OpenAppend.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates how to append to a file
    /// using a URI object.
    /// <code source="..\Examples\OpenAppendURI.cs" lang="cs" />
    /// </example>
    /// <example>The following example demonstrates how to get a file
    /// listing from the server.
    /// <code source="..\Examples\GetListing.cs" lang="cs" />
    /// </example>
    public interface IFtpClient : IDisposable
    {
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
        /// Gets or sets the length of time in miliseconds
        /// that must pass since the last socket activity
        /// before calling Poll() on the socket to test for
        /// connectivity. Setting this interval too low will
        /// have a negative impact on perfomance. Setting this
        /// interval to 0 disables Poll()'ing all together.
        /// The default value is 15 seconds.
        /// </summary>
        int SocketPollInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a test should be performed to
        /// see if there is stale (unrequested data) sitting on the socket. In some
        /// cases the control connection may time out but before the server closes
        /// the connection it might send a 4xx response that was unexpected and
        /// can cause synchronization errors with transactions. To avoid this
        /// problem the Execute() method checks to see if there is any data
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
        /// value is Encoding.ASCII however upon connection, the client checks
        /// for UTF8 support and if it's there this property is switched over to
        /// Encoding.UTF8. Manually setting this value overrides automatic detection
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
        /// of recursion that DereferenceLink() will follow symbolic
        /// links before giving up. You can also specify the value
        /// to be used as one of the overloaded parameters to the
        /// DereferenceLink() method. The default value is 20. Specifying
        /// -1 here means inifinitly try to resolve a link. This is
        /// not recommended for obvious reasons (stack overflow).
        /// </summary>
        int MaximumDereferenceCount { get; set; }

        /// <summary>
        /// Client certificates to be used in SSL authentication process
        /// </summary>
        X509CertificateCollection ClientCertificates { get; }

        /// <summary>
        /// Data connection type, default is AutoPassive which tries
        /// a connection with EPSV first and if it fails then tries
        /// PASV before giving up. If you know exactly which kind of
        /// connection you need you can slightly increase performance
        /// by defining a speicific type of passive or active data
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
        /// Gets or sets the length of time in miliseconds to wait for a connection 
        /// attempt to succeed before giving up. Default is 15000 (15 seconds).
        /// </summary>
        int ConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets the length of time wait in miliseconds for data to be
        /// read from the underlying stream. The default value is 15000 (15 seconds).
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets the length of time in miliseconds for a data connection
        /// to be established before giving up. Default is 15000 (15 seconds).
        /// </summary>
        int DataConnectionConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets the length of time in miliseconds the data channel
        /// should wait for the server to send data. Default value is 
        /// 15000 (15 seconds).
        /// </summary>
        int DataConnectionReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if SocketOption.KeepAlive should be set on 
        /// the underlying stream's socket. If the connection is alive, the option is
        /// adjusted in real-time. The value is stored and the KeepAlive option is set
        /// accordingly upon any new connections. The value set here is also applied to
        /// all future data streams. It has no affect on cloned control connections or
        /// data connections already in progress. The default value is false.
        /// </summary>
        bool SocketKeepAlive { get; set; }

        /// <summary>
        /// Gets the server capabilties represented by flags
        /// </summary>
        FtpCapability Capabilities { get; }

        /// <summary>
        /// Get the hash types supported by the server, if any. This
        /// is a recent extension to the protocol that is not fully
        /// standardized and is not guarateed to work. See here for
        /// more details:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </summary>
        FtpHashAlgorithm HashAlgorithms { get; }

        /// <summary>
        /// Type of SSL to use, or none. Default is none. Explicit is TLS, Implicit is SSL.
        /// </summary>
        FtpEncryptionMode EncryptionMode { get; set; }

        /// <summary>
        /// Indicates if data channel transfers should be encrypted. Only valid if EncryptionMode
        /// property is not equal to FtpSslMode.None.
        /// </summary>
        bool DataConnectionEncryption { get; set; }

        /// <summary>
        /// Gets the type of system/server that we're
        /// connected to.
        /// </summary>
        string SystemType { get; }

        /// <summary>
        /// Event is fired to validate SSL certificates. If this event is
        /// not handled and there are errors validating the certificate
        /// the connection will be aborted.
        /// </summary>
        /// <example><code source="..\Examples\ValidateCertificate.cs" lang="cs" /></example>
        event FtpSslValidation ValidateCertificate;

        /// <summary>
        /// Performs a bitwise and to check if the specified
        /// flag is set on the Capabilities enum property.
        /// </summary>
        /// <param name="cap">The capability to check for</param>
        /// <returns>True if the feature was found</returns>
        bool HasFeature(FtpCapability cap);

        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="command">The command to execute with optional format place holders</param>
        /// <param name="args">Format parameters to the command</param>
        /// <returns>The servers reply to the command</returns>
        /// <example><code source="..\Examples\Execute.cs" lang="cs" /></example>
        FtpReply Execute(string command, params object[] args);

        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The servers reply to the command</returns>
        /// <example><code source="..\Examples\Execute.cs" lang="cs" /></example>
        FtpReply Execute(string command);

        /// <summary>
        /// Performs an asynchronouse execution of the specified command
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="callback">The AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginExecute.cs" lang="cs" /></example>
        IAsyncResult BeginExecute(string command, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous command
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginExecute</param>
        /// <returns>FtpReply object (never null).</returns>
        /// <example><code source="..\Examples\BeginExecute.cs" lang="cs" /></example>
        FtpReply EndExecute(IAsyncResult ar);

        /// <summary>
        /// Connect to the server. Throws ObjectDisposedException if this object has been disposed.
        /// </summary>
        /// <example><code source="..\Examples\Connect.cs" lang="cs" /></example>
        void Connect();

        /// <summary>
        /// Initiates a connection to the server
        /// </summary>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
        IAsyncResult BeginConnect(AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous connection attempt to the server
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginConnect()</param>
        /// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
        void EndConnect(IAsyncResult ar);

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Initiates a disconnection on the server
        /// </summary>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDisconnect.cs" lang="cs" /></example>
        IAsyncResult BeginDisconnect(AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginDisconnect
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDisconnect</param>
        /// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
        void EndDisconnect(IAsyncResult ar);

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
        /// <param name="restart">Resume location</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        Stream OpenRead(string path, long restart);

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
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        IAsyncResult BeginOpenRead(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        IAsyncResult BeginOpenRead(string path, FtpDataType type, AsyncCallback callback, object state);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="restart">Resume location</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        IAsyncResult BeginOpenRead(string path, long restart, AsyncCallback callback, object state);

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="restart">Resume location</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        IAsyncResult BeginOpenRead(string path, FtpDataType type, long restart, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginOpenRead()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginOpenRead()</param>
        /// <returns>A readable stream</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        Stream EndOpenRead(IAsyncResult ar);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
        Stream OpenWrite(string path);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
        Stream OpenWrite(string path, FtpDataType type);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
        IAsyncResult BeginOpenWrite(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
        IAsyncResult BeginOpenWrite(string path, FtpDataType type, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginOpenWrite()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginOpenWrite()</param>
        /// <returns>A writable stream</returns>
        /// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
        Stream EndOpenWrite(IAsyncResult ar);

        /// <summary>
        /// Opens the specified file to be appended to
        /// </summary>
        /// <param name="path">The full or relative path to the file to be opened</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
        Stream OpenAppend(string path);

        /// <summary>
        /// Opens the specified file to be appended to
        /// </summary>
        /// <param name="path">The full or relative path to the file to be opened</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
        Stream OpenAppend(string path, FtpDataType type);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
        IAsyncResult BeginOpenAppend(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
        IAsyncResult BeginOpenAppend(string path, FtpDataType type, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginOpenAppend()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginOpenWrite()</param>
        /// <returns>A writable stream</returns>
        /// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
        Stream EndOpenAppend(IAsyncResult ar);

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
        /// Derefence a FtpListItem object asynchronously
        /// </summary>
        /// <param name="item">The item to derefence</param>
        /// <param name="recMax">Maximum recursive calls</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State Object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
        IAsyncResult BeginDereferenceLink(FtpListItem item, int recMax, AsyncCallback callback, object state);

        /// <summary>
        /// Derefence a FtpListItem object asynchronously. See the
        /// MaximumDereferenceCount property for controlling
        /// how deep this method will recurse before giving up.
        /// </summary>
        /// <param name="item">The item to derefence</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State Object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
        IAsyncResult BeginDereferenceLink(FtpListItem item, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginDereferenceLink
        /// </summary>
        /// <param name="ar">IAsyncResult</param>
        /// <returns>FtpListItem, null if the link can't be dereferenced</returns>
        /// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
        FtpListItem EndDereferenceLink(IAsyncResult ar);

        /// <summary>
        /// Returns information about a file system object. You should check the Capabilities
        /// flags for the FtpCapability.MLSD flag before calling this method. Failing to do
        /// so will result in an InvalidOperationException being thrown when the server
        /// does not support machine listings. Returns null if the server response can't
        /// be parsed or the server returns a failure completion code. The error for a failure
        /// is logged with FtpTrace. No exception is thrown on error because that would negate
        /// the usefullness of this method for checking for the existence of an object.
        /// </summary>
        /// <param name="path">The path of the object to retrieve information about</param>
        /// <returns>A FtpListItem object</returns>
        FtpListItem GetObjectInfo(string path);

        /// <summary>
        /// Returns information about a file system object. You should check the Capabilities
        /// flags for the FtpCapability.MLSD flag before calling this method. Failing to do
        /// so will result in an InvalidOperationException being thrown when the server
        /// does not support machine listings. Returns null if the server response can't
        /// be parsed or the server returns a failure completion code. The error for a failure
        /// is logged with FtpTrace. No exception is thrown on error because that would negate
        /// the usefullness of this method for checking for the existence of an object.
        /// </summary>
        /// <param name="path">Path of the item to retrieve information about</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginGetObjectInfo(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginGetObjectInfo
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetObjectInfo</param>
        /// <returns>FtpListItem if the command succeeded, null if there was a problem.</returns>
        FtpListItem EndGetObjectInfo(IAsyncResult ar);

        /// <summary>
        /// Gets a file listing from the server. Each FtpListItem object returned
        /// contains information about the file that was able to be retrieved. If
        /// a DateTime property is equal to DateTime.MinValue then it means the 
        /// date in question was not able to be retrieved. If the Size property
        /// is equal to 0 then it means the size of the object could also not
        /// be retrieved.
        /// </summary>
        /// <returns>An array of FtpListItem objects</returns>
        /// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
        FtpListItem[] GetListing();

        /// <summary>
        /// Gets a file listing from the server. Each FtpListItem object returned
        /// contains information about the file that was able to be retrieved. If
        /// a DateTime property is equal to DateTime.MinValue then it means the 
        /// date in question was not able to be retrieved. If the Size property
        /// is equal to 0 then it means the size of the object could also not
        /// be retrieved.
        /// </summary>
        /// <param name="path">The path of the directory to list</param>
        /// <returns>An array of FtpListItem objects</returns>
        /// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
        FtpListItem[] GetListing(string path);

        /// <summary>
        /// Gets a file listing from the server. Each FtpListItem object returned
        /// contains information about the file that was able to be retrieved. If
        /// a DateTime property is equal to DateTime.MinValue then it means the 
        /// date in question was not able to be retrieved. If the Size property
        /// is equal to 0 then it means the size of the object could also not
        /// be retrieved.
        /// </summary>
        /// <param name="path">The path of the directory to list</param>
        /// <param name="options">Options that dictacte how a list is performed and what information is gathered.</param>
        /// <returns>An array of FtpListItem objects</returns>
        /// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
        FtpListItem[] GetListing(string path, FtpListOption options);

        /// <summary>
        /// Gets a file listing from the server asynchronously
        /// </summary>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        IAsyncResult BeginGetListing(AsyncCallback callback, Object state);

        /// <summary>
        /// Gets a file listing from the server asynchronously
        /// </summary>
        /// <param name="path">The path to list</param>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        IAsyncResult BeginGetListing(string path, AsyncCallback callback, Object state);

        /// <summary>
        /// Gets a file listing from the server asynchronously
        /// </summary>
        /// <param name="path">The path to list</param>
        /// <param name="options">Options that dictate how the list operation is performed</param>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        IAsyncResult BeginGetListing(string path, FtpListOption options, AsyncCallback callback, Object state);

        /// <summary>
        /// Ends an asynchronous file listing
        /// </summary>
        /// <param name="ar">IAsyncResult return from BeginGetListing()</param>
        /// <returns>An array of items retrieved in the listing</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        FtpListItem[] EndGetListing(IAsyncResult ar);

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
        /// Asynchronously gets a list of file and directory names for the specified path.
        /// </summary>
        /// <param name="path">The path of the directory to list</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
        IAsyncResult BeginGetNameListing(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Asynchronously gets a list of file and directory names for the specified path.
        /// </summary>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
        IAsyncResult BeginGetNameListing(AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginGetNameListing()
        /// </summary>
        /// <param name="ar">IAsyncResult object returned from BeginGetNameListing</param>
        /// <returns>An array of file and directory names if any were returned.</returns>
        /// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
        string[] EndGetNameListing(IAsyncResult ar);

        /// <summary>
        /// Sets the work directory on the server
        /// </summary>
        /// <param name="path">The path of the directory to change to</param>
        /// <example><code source="..\Examples\SetWorkingDirectory.cs" lang="cs" /></example>
        void SetWorkingDirectory(string path);

        /// <summary>
        /// Asynchronously changes the working directory on the server
        /// </summary>
        /// <param name="path">The directory to change to</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
        IAsyncResult BeginSetWorkingDirectory(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Ends asynchronous directory change
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginSetWorkingDirectory</param>
        /// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
        void EndSetWorkingDirectory(IAsyncResult ar);

        /// <summary>
        /// Gets the current working directory
        /// </summary>
        /// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
        /// <example><code source="..\Examples\GetWorkingDirectory.cs" lang="cs" /></example>
        string GetWorkingDirectory();

        /// <summary>
        /// Asynchronously retrieves the working directory
        /// </summary>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
        IAsyncResult BeginGetWorkingDirectory(AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous call to retrieve the working directory
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetWorkingDirectory</param>
        /// <returns>The current working directory</returns>
        /// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
        string EndGetWorkingDirectory(IAsyncResult ar);

        /// <summary>
        /// Gets the size of the file
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <returns>-1 if the command fails, otherwise the file size</returns>
        /// <example><code source="..\Examples\GetFileSize.cs" lang="cs" /></example>
        long GetFileSize(string path);

        /// <summary>
        /// Asynchronously retrieve the size of the specified file
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
        IAsyncResult BeginGetFileSize(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginGetFileSize()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetFileSize</param>
        /// <returns>The size of the file, -1 if there was a problem.</returns>
        /// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
        long EndGetFileSize(IAsyncResult ar);

        /// <summary>
        /// Gets the modified time of the file
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <returns>The modified time, DateTime.MinValue if there was a problem</returns>
        /// <example><code source="..\Examples\GetModifiedTime.cs" lang="cs" /></example>
        DateTime GetModifiedTime(string path);

        /// <summary>
        /// Gets the modified time of the file
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
        IAsyncResult BeginGetModifiedTime(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginGetModifiedTime()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetModifiedTime()</param>
        /// <returns>The modified time, DateTime.MinValue if there was a problem</returns>
        /// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
        DateTime EndGetModifiedTime(IAsyncResult ar);

        /// <summary>
        /// Deletes a file on the server
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <example><code source="..\Examples\DeleteFile.cs" lang="cs" /></example>
        void DeleteFile(string path);

        /// <summary>
        /// Asynchronously deletes a file from the server
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
        IAsyncResult BeginDeleteFile(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginDeleteFile
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDeleteFile</param>
        /// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
        void EndDeleteFile(IAsyncResult ar);

        /// <summary>
        /// Deletes the specified directory on the server.
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        void DeleteDirectory(string path);

        /// <summary>
        /// Delets the specified directory on the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        void DeleteDirectory(string path, bool force);

        /// <summary>
        /// Deletes the specified directory on the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <param name="options">FtpListOptions for controlling how the directory
        /// contents are retrieved with the force option is true. If you experience problems
        /// the file listing can be fine tuned through this parameter.</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        void DeleteDirectory(string path, bool force, FtpListOption options);

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        IAsyncResult BeginDeleteDirectory(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        IAsyncResult BeginDeleteDirectory(string path, bool force, AsyncCallback callback, object state);

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <param name="options">FtpListOptions for controlling how the directory
        /// contents are retrieved with the force option is true. If you experience problems
        /// the file listing can be fine tuned through this parameter.</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        IAsyncResult BeginDeleteDirectory(string path, bool force, FtpListOption options, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginDeleteDirectory()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDeleteDirectory</param>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        void EndDeleteDirectory(IAsyncResult ar);

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
        /// Checks if a directory exists on the server asynchronously.
        /// </summary>
        /// <returns>IAsyncResult</returns>
        /// <param name='path'>The full or relative path of the directory to check for</param>
        /// <param name='callback'>Async callback</param>
        /// <param name='state'>State object</param>
        /// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
        IAsyncResult BeginDirectoryExists(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginDirectoryExists
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDirectoryExists</param>
        /// <returns>True if the directory exists. False otherwise.</returns>
        /// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
        bool EndDirectoryExists(IAsyncResult ar);

        /// <summary>
        /// Checks if a file exsts on the server by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <returns>True if the file exists</returns>
        /// <example><code source="..\Examples\FileExists.cs" lang="cs" /></example>
        bool FileExists(string path);

        /// <summary>
        /// Checks if a file exsts on the server by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <param name="options">Options for controling the file listing used to
        /// determine if the file exists.</param>
        /// <returns>True if the file exists</returns>
        /// <example><code source="..\Examples\FileExists.cs" lang="cs" /></example>
        bool FileExists(string path, FtpListOption options);

        /// <summary>
        /// Checks if a file exsts on the server by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
        IAsyncResult BeginFileExists(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Checks if a file exsts on the server by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <param name="options">Options for controling the file listing used to
        /// determine if the file exists.</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
        IAsyncResult BeginFileExists(string path, FtpListOption options, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginFileExists
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginFileExists</param>
        /// <returns>True if the file exists</returns>
        /// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
        bool EndFileExists(IAsyncResult ar);

        /// <summary>
        /// Creates a directory on the server. If the preceding
        /// directories do not exist they are created.
        /// </summary>
        /// <param name="path">The full or relative path to the new directory</param>
        /// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
        void CreateDirectory(string path);

        /// <summary>
        /// Creates a directory on the server
        /// </summary>
        /// <param name="path">The full or relative path to the directory to create</param>
        /// <param name="force">Try to force all non-existant pieces of the path to be created</param>
        /// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
        void CreateDirectory(string path, bool force);

        /// <summary>
        /// Creates a directory asynchronously
        /// </summary>
        /// <param name="path">The full or relative path to the directory to create</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
        IAsyncResult BeginCreateDirectory(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Creates a directory asynchronously
        /// </summary>
        /// <param name="path">The full or relative path to the directory to create</param>
        /// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
        IAsyncResult BeginCreateDirectory(string path, bool force, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginCreateDirectory
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginCreateDirectory</param>
        /// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
        void EndCreateDirectory(IAsyncResult ar);

        /// <summary>
        /// Renames an object on the remote file system.
        /// </summary>
        /// <param name="path">The full or relative path to the object</param>
        /// <param name="dest">The old or new full or relative path including the new name of the object</param>
        /// <example><code source="..\Examples\Rename.cs" lang="cs" /></example>
        void Rename(string path, string dest);

        /// <summary>
        /// Asynchronously renames an object on the server
        /// </summary>
        /// <param name="path">The full or relative path to the object</param>
        /// <param name="dest">The old or new full or relative path including the new name of the object</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
        IAsyncResult BeginRename(string path, string dest, AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginRename
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginRename</param>
        /// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
        void EndRename(IAsyncResult ar);

        /// <summary>
        /// Gets the currently selected hash algorith for the HASH
        /// command. This feature is experimental. See this link
        /// for details:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </summary>
        /// <returns>The FtpHashType flag or FtpHashType.NONE if there was a problem.</returns>
        /// <example><code source="..\Examples\GetHashAlgorithm.cs" lang="cs" /></example>
        FtpHashAlgorithm GetHashAlgorithm();

        /// <summary>
        /// Asynchronously get the hash algorithm being used by the HASH command.
        /// </summary>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginGetHashAlgorithm(AsyncCallback callback, object state);

        /// <summary>
        /// Ends a call to BeginGetHashAlgorithm
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetHashAlgorithm</param>
        FtpHashAlgorithm EndGetHashAlgorithm(IAsyncResult ar);

        /// <summary>
        /// Tells the server which hash algorith to use
        /// for the HASH command. If you specifiy an 
        /// algorithm not listed in FtpClient.HashTypes
        /// a NotImplemented() exectpion will be thrown
        /// so be sure to query that list of Flags before
        /// selecting a hash algorithm. Support for the
        /// HASH command is experimental. Please see
        /// the following link for more details:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </summary>
        /// <param name="type">Hash Algorithm</param>
        /// <example><code source="..\Examples\SetHashAlgorithm.cs" lang="cs" /></example>
        void SetHashAlgorithm(FtpHashAlgorithm type);

        /// <summary>
        /// Asynchronously sets the hash algorithm type to be used with the HASH command.
        /// </summary>
        /// <param name="type">Hash algorithm to use</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginSetHashAlgorithm(FtpHashAlgorithm type, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous call to BeginSetHashAlgorithm
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginSetHashAlgorithm</param>
        void EndSetHashAlgorithm(IAsyncResult ar);

        /// <summary>
        /// Gets the hash of an object on the server using the
        /// currently selected hash algorithm. Supported
        /// algorithms, if any, are available in the HashAlgorithms
        /// property. You should confirm that it's not equal
        /// to FtpHashAlgorithm.NONE before calling this method
        /// otherwise the server trigger a FtpCommandException()
        /// due to a lack of support for the HASH command. You can
        /// set the algorithm using the SetHashAlgorithm() method and
        /// you can query the server for the current hash algorithm
        /// using the GetHashAlgorithm() method.
        /// 
        /// This feature is experimental and based on the following draft:
        /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
        /// </summary>
        /// <param name="path">Full or relative path of the object to compute the hash for.</param>
        /// <returns>The hash of the file.</returns>
        /// <example><code source="..\Examples\GetHash.cs" lang="cs" /></example>
        FtpHash GetHash(string path);

        /// <summary>
        /// Asynchronously retrieves the hash for the specified file
        /// </summary>
        /// <param name="path">The file you want the server to compute the hash for</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        IAsyncResult BeginGetHash(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Ends an asynchronous call to BeginGetHash
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetHash</param>
        void EndGetHash(IAsyncResult ar);

        /// <summary>
        /// Disables UTF8 support and changes the Encoding property
        /// back to ASCII. If the server returns an error when trying
        /// to turn UTF8 off a FtpCommandException will be thrown.
        /// </summary>
        void DisableUTF8();
    }
}