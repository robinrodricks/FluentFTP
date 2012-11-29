using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.FtpClient {
    /// <summary>
    /// Event is fired when a ssl certificate needs to be validated
    /// </summary>
    /// <param name="control">The contol connection that triggered the event</param>
    /// <param name="e">Event args</param>
    public delegate void FtpSslValidation(FtpClient control, FtpSslValidationEventArgs e);

    /// <summary>
    /// FTP Control Connection. Speaks the FTP protocol with the server and
    /// provides facilities for performing basic transactions.
    /// 
    /// Debugging problems with the FTP transactions is much easier to do when
    /// you can see exactly what is sent to the server and the reply 
    /// System.Net.FtpClient gets in return. In order to view this information
    /// you need to build System.Net.FtpClient with DEBUG defined. When enable
    /// System.Net.FtpClient will log to the System.Diagnostics.Debug trace
    /// listener. You can access what is being sent there by either defining 
    /// your TraceListener object or by using one of the pre-existing ones.
    /// </summary>
    /// <example>The following example illustrates how to assist in debugging
    /// System.Net.FtpClient by getting a transaction log from the server. In
    /// order for this code to work System.Net.FtpClient must be built with
    /// #DEBUG defined. Writing the Debug.Listener is omitted from release builds.
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
    public class FtpClient : IDisposable {
        /// <summary>
        /// Used for internally syncrhonizing access to this
        /// object from multiple threads
        /// </summary>
        readonly Mutex m_lock = new Mutex();

        /// <summary>
        /// A list of asynchronoous methods that are in progress
        /// </summary>
        readonly Dictionary<IAsyncResult, object> m_asyncmethods = new Dictionary<IAsyncResult, object>();

        /// <summary>
        /// Control connection socket stream
        /// </summary>
        FtpSocketStream m_stream = null;

        /// <summary>
        /// Gets the base stream for talking to the server via
        /// the control connection.
        /// </summary>
        protected Stream BaseStream {
            get {
                return m_stream;
            }
        }

        /// <summary>
        /// Gets a value indicating if the connection is alive
        /// </summary>
        public bool IsConnected {
            get {
                if (m_stream != null)
                    return m_stream.IsConnected;
                return false;
            }
        }

        bool m_threadSafeDataChannels = true;
        /// <summary>
        /// When this value is set to true (default) the control connection
        /// is cloned and a new connection the server is established for the
        /// data channel operation. This is a thread safe approach to make
        /// asynchronous operations on a single control connection transparent
        /// to the developer.
        /// </summary>
        [FtpControlConnectionClone]
        public bool EnableThreadSafeDataConnections {
            get {
                return m_threadSafeDataChannels;
            }
            set {
                m_threadSafeDataChannels = value;
            }
        }

        bool m_isClone = false;
        /// <summary>
        /// Gets a value indicating if this control connection is a clone. This property
        /// is used with data streams to determine if the connection should be closed
        /// when the stream is closed. Servers typically only allow 1 data connection
        /// per control connection. If you try to open multiple data connections this
        /// object will be cloned for 2 or more resulting in N new connections to the
        /// server.
        /// </summary>
        internal bool IsClone {
            get {
                return m_isClone;
            }
            private set {
                m_isClone = value;
            }
        }

        Encoding m_textEncoding = Encoding.ASCII;
        /// <summary>
        /// Gets the text encoding being used when talking with the server. The default
        /// value is Encoding.ASCII however upon connection, the client checks
        /// for UTF8 support and if it's there this property is switched over to
        /// Encoding.UTF8.
        /// </summary>
        public Encoding Encoding {
            get {
                return m_textEncoding;
            }
            private set {
                m_textEncoding = value;
            }
        }

        string m_host = null;
        /// <summary>
        /// The server to connect to
        /// </summary>
        [FtpControlConnectionClone]
        public string Host {
            get {
                return m_host;
            }
            set {
                m_host = value;
            }
        }

        int m_port = 0;
        /// <summary>
        /// The port to connect to. If this value is set to 0 (Default) the port used
        /// will be determined by the type of SSL used or if no SSL is to be used it 
        /// will automatically connect to port 21.
        /// </summary>
        [FtpControlConnectionClone]
        public int Port {
            get {
                // automatically determine port
                // when m_port is 0.
                if (m_port == 0) {
                    switch (EncryptionMode) {
                        case FtpEncryptionMode.None:
                        case FtpEncryptionMode.Explicit:
                            return 21;
                        case FtpEncryptionMode.Implicit:
                            return 990;
                    }
                }

                return m_port;
            }
            set {
                m_port = value;
            }
        }

        NetworkCredential m_credentials = null;
        /// <summary>
        /// Credentials used for authentication
        /// </summary>
        [FtpControlConnectionClone]
        public NetworkCredential Credentials {
            get {
                return m_credentials;
            }
            set {
                m_credentials = value;
            }
        }

        X509CertificateCollection m_clientCerts = new X509CertificateCollection();
        /// <summary>
        /// Client certificates to be used in SSL authentication process
        /// </summary>
        [FtpControlConnectionClone]
        public X509CertificateCollection ClientCertificates {
            get {
                return m_clientCerts;
            }
            private set {
                m_clientCerts = value;
            }
        }

        FtpDataConnectionType m_dataConnectionType = FtpDataConnectionType.AutoPassive;
        /// <summary>
        /// Data connection type, default is AutoPassive which tries
        /// a connection with EPSV first and if it fails then tries
        /// PASV before giving up. If you know exactly which kind of
        /// connection you need you can slightly increase performance
        /// by defining a speicific type of passive or active data
        /// connection here.
        /// </summary>
        [FtpControlConnectionClone]
        public FtpDataConnectionType DataConnectionType {
            get {
                return m_dataConnectionType;
            }
            set {
                m_dataConnectionType = value;
            }
        }

        int m_connectTimeout = 15000;
        /// <summary>
        /// Gets or sets the length of time in miliseconds to wait for a connection 
        /// attempt to succeed before giving up. Default is 15000 (15 seconds).
        /// </summary>
        [FtpControlConnectionClone]
        public int ConnectTimeout {
            get {
                return m_connectTimeout;
            }
            set {
                m_connectTimeout = value;
            }
        }

        int m_readTimeout = 15000;
        /// <summary>
        /// Gets or sets the length of time wait in miliseconds for data to be
        /// read from the underlying stream. The default value is 15000 (15 seconds).
        /// </summary>
        [FtpControlConnectionClone]
        public int ReadTimeout {
            get {
                return m_readTimeout;
            }
            set {
                m_readTimeout = value;
            }
        }

        int m_dataConnectionConnectTimeout = 15000;
        /// <summary>
        /// Gets or sets the length of time in miliseconds for a data connection
        /// to be established before giving up. Default is 15000 (15 seconds).
        /// </summary>
        [FtpControlConnectionClone]
        public int DataConnectionConnectTimeout {
            get {
                return m_dataConnectionConnectTimeout;
            }
            set {
                m_dataConnectionConnectTimeout = value;
            }
        }

        int m_dataConnectionReadTimeout = 15000;
        /// <summary>
        /// Gets or sets the length of time in miliseconds the data channel
        /// should wait for the server to send data. Default value is 
        /// 15000 (15 seconds).
        /// </summary>
        [FtpControlConnectionClone]
        public int DataConnectionReadTimeout {
            get {
                return m_dataConnectionReadTimeout;
            }
            set {
                m_dataConnectionReadTimeout = value;
            }
        }

        FtpCapability m_caps = FtpCapability.NONE;
        /// <summary>
        /// Gets the server capabilties represented by flags
        /// </summary>
        public FtpCapability Capabilities {
            get {
                if (m_stream == null || !m_stream.IsConnected) {
                    Connect();
                }

                return m_caps;
            }
            private set {
                m_caps = value;
            }
        }

        FtpEncryptionMode m_encryptionmode = FtpEncryptionMode.None;
        /// <summary>
        /// Type of SSL to use, or none. Default is none. Explicit is TLS, Implicit is SSL.
        /// </summary>
        [FtpControlConnectionClone]
        public FtpEncryptionMode EncryptionMode {
            get {
                return m_encryptionmode;
            }
            set {
                m_encryptionmode = value;
            }
        }

        bool m_dataConnectionEncryption = true;
        /// <summary>
        /// Indicates if data channel transfers should be encrypted. Only valid if EncryptionMode
        /// property is not equal to FtpSslMode.None.
        /// </summary>
        [FtpControlConnectionClone]
        public bool DataConnectionEncryption {
            get {
                return m_dataConnectionEncryption;
            }
            set {
                m_dataConnectionEncryption = value;
            }
        }

        FtpSslValidation m_sslvalidate = null;
        /// <summary>
        /// Event is fired to validate SSL certificates. If this event is
        /// not handled and there are errors validating the certificate
        /// the connection will be aborted.
        /// </summary>
        /// <example><code source="..\Examples\ValidateCertificate.cs" lang="cs" /></example>
        public event FtpSslValidation ValidateCertificate {
            add {
                m_sslvalidate += value;
            }
            remove {
                m_sslvalidate -= value;
            }
        }

        /// <summary>
        /// Gets the type of system/server that we're
        /// connected to.
        /// </summary>
        public string SystemType {
            get {
                FtpReply reply = Execute("SYST");

                if (reply.Success)
                    return reply.Message;

                return null;
            }
        }

        /// <summary>
        /// Fires the SSL validation event
        /// </summary>
        /// <param name="e">Event Args</param>
        void OnValidateCertficate(FtpSslValidationEventArgs e) {
            FtpSslValidation evt;

            evt = m_sslvalidate;
            if (evt != null)
                evt(this, e);
        }

        /// <summary>
        /// Retretieves the delegate for the specified IAsyncResult and removes
        /// it from the m_asyncmethods collection if the operation is successfull
        /// </summary>
        /// <typeparam name="T">Type of delegate to retrieve</typeparam>
        /// <param name="ar">The IAsyncResult to retrieve the delegate for</param>
        /// <returns>The delegate that generated the specified IAsyncResult</returns>
        protected T GetAsyncDelegate<T>(IAsyncResult ar) {
            T func;

            lock (m_asyncmethods) {
                if (!m_asyncmethods.ContainsKey(ar))
                    throw new InvalidOperationException("The specified IAsyncResult could not be located.");

                if (!(m_asyncmethods[ar] is T)) {
                    StackTrace st = new StackTrace(1);

                    throw new InvalidCastException("The AsyncResult cannot be matched to the specified delegate. " +
                        string.Format("Are you sure you meant to call {0} and not another method?",
                        st.GetFrame(0).GetMethod().Name)
                    );
                }

                func = (T)m_asyncmethods[ar];
                m_asyncmethods.Remove(ar);
            }

            return func;
        }

        /// <summary>
        /// Clones the control connection for opening multipe data streams
        /// </summary>
        /// <returns>A new control connection with the same property settings as this one</returns>
        /// <example><code source="..\Examples\CloneConnection.cs" lang="cs" /></example>
        internal FtpClient CloneConnection() {
            FtpClient conn = new FtpClient();

            conn.m_isClone = true;

            foreach (PropertyInfo prop in GetType().GetProperties()) {
                object[] attributes = prop.GetCustomAttributes(typeof(FtpControlConnectionClone), true);

                if (attributes != null && attributes.Length > 0) {
                    prop.SetValue(conn, prop.GetValue(this, null), null);
                }
            }

            // always accept certficate no matter what because if code execution ever
            // gets here it means the certificate on the control connection object being
            // cloned was already accepted.
            conn.ValidateCertificate += new FtpSslValidation(
                delegate(FtpClient obj, FtpSslValidationEventArgs e) {
                    e.Accept = true;
                });

            return conn;
        }

        /// <summary>
        /// Retreives a reply from the server. Do not execute this method
        /// unless you are sure that a reply has been sent, i.e., you
        /// executed a command. Doing so will cause the code to hang
        /// indefinitely waiting for a server reply that is never comming.
        /// </summary>
        /// <returns>FtpReply representing the response from the server</returns>
        /// <example><code source="..\Examples\BeginGetReply.cs" lang="cs" /></example>
        internal FtpReply GetReply() {
            FtpReply reply = new FtpReply();
            string buf;

            try {
                m_lock.WaitOne();

                if (!IsConnected)
                    throw new InvalidOperationException("No connection to the server has been established.");

                m_stream.ReadTimeout = m_readTimeout;
                while ((buf = m_stream.ReadLine(Encoding)) != null) {
                    Match m;

#if DEBUG
                    Debug.WriteLine(buf);
#endif

                    if ((m = Regex.Match(buf, "^(?<code>[0-9]{3}) (?<message>.*)$")).Success) {
                        reply.Code = m.Groups["code"].Value;
                        reply.Message = m.Groups["message"].Value;
                        break;
                    }

                    reply.InfoMessages += string.Format("{0}\n", buf);
                }
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return reply;
        }

        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="command">The command to execute with optional format place holders</param>
        /// <param name="args">Format parameters to the command</param>
        /// <returns>The servers reply to the command</returns>
        /// <example><code source="..\Examples\Execute.cs" lang="cs" /></example>
        public FtpReply Execute(string command, params object[] args) {
            return Execute(string.Format(command, args));
        }

        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The servers reply to the command</returns>
        /// <example><code source="..\Examples\Execute.cs" lang="cs" /></example>
        public FtpReply Execute(string command) {
            FtpReply reply;

            try {
                m_lock.WaitOne();

                if (!IsConnected)
                    Connect();

#if DEBUG
                Debug.WriteLine(command.StartsWith("PASS") ? "PASS <omitted>" : command);
#endif

                m_stream.WriteLine(Encoding, command);
                reply = GetReply();
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return reply;
        }

        delegate FtpReply AsyncExecute(string command);

        /// <summary>
        /// Performs an asynchronouse execution of the specified command
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="callback">The AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginExecute.cs" lang="cs" /></example>
        public IAsyncResult BeginExecute(string command, AsyncCallback callback, object state) {
            AsyncExecute func;
            IAsyncResult ar;

            ar = (func = new AsyncExecute(Execute)).BeginInvoke(command, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous command
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginExecute</param>
        /// <returns>FtpReply object (never null).</returns>
        /// <example><code source="..\Examples\BeginExecute.cs" lang="cs" /></example>
        public FtpReply EndExecute(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncExecute>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <example><code source="..\Examples\Connect.cs" lang="cs" /></example>
        public virtual void Connect() {
            FtpReply reply;

            try {
                m_lock.WaitOne();

                if (m_stream == null) {
                    m_stream = new FtpSocketStream();
                    m_stream.ValidateCertificate += new FtpSocketStreamSslValidation(FireValidateCertficate);
                }
                else
                    if (IsConnected)
                        Disconnect();

                if (Host == null)
                    throw new FtpException("No host has been specified");

                if (Credentials == null)
                    throw new FtpException("No credentials have been specified");

                m_textEncoding = Encoding.Default;
                m_caps = FtpCapability.NONE;
                m_stream.ConnectTimeout = m_connectTimeout;
                m_stream.Connect(Host, Port);

                if (EncryptionMode == FtpEncryptionMode.Implicit)
                    m_stream.ActivateEncryption(Host, 
                        m_clientCerts.Count > 0 ? m_clientCerts : null);

                if (!(reply = GetReply()).Success)
                    throw new FtpCommandException(reply);

                if (EncryptionMode == FtpEncryptionMode.Explicit) {
                    if (!(reply = Execute("AUTH TLS")).Success)
                        throw new FtpSecrutiyNotAvailableException("AUTH TLS command failed.");
                    m_stream.ActivateEncryption(Host, 
                        m_clientCerts.Count > 0 ? m_clientCerts : null);
                }

                if (m_stream.IsEncrypted && DataConnectionEncryption) {
                    if (!(reply = Execute("PBSZ 0")).Success)
                        throw new FtpCommandException(reply);
                    if (!(reply = Execute("PROT P")).Success)
                        throw new FtpCommandException(reply);
                }

                if (m_credentials != null) {
                    Authenticate();
                }

                if ((reply = Execute("FEAT")).Success && reply.InfoMessages != null) {
                    GetFeatures(reply);
                }

                // Enable UTF8 if it's available
                if (m_caps.HasFlag(FtpCapability.UTF8)) {
                    // If the server supports UTF8 it should already be enabled and this
                    // command should not matter however there are conflicting drafts
                    // about this so we'll just execute it to be safe. 
                    Execute("OPTS UTF8 ON");
                    m_textEncoding = Encoding.UTF8;
                }
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Performs a login on the server. This method is overridable so
        /// that the login procedure can be changed to support, for example,
        /// a FTP proxy.
        /// </summary>
        protected virtual void Authenticate() {
            FtpReply reply;

            if (!(reply = Execute("USER {0}", Credentials.UserName)).Success)
                throw new FtpCommandException(reply);

            if (reply.Type == FtpResponseType.PositiveIntermediate
                && !(reply = Execute("PASS {0}", Credentials.Password)).Success)
                throw new FtpCommandException(reply);
        }

        /// <summary>
        /// Populates the capabilities flags based on capabilities
        /// supported by this server. This method is overridable
        /// so that new features can be supported
        /// </summary>
        /// <param name="reply">The reply object from the FEAT command. The InfoMessages property will
        /// contain a list of the features the server supported delimited by a new line '\n' character.</param>
        protected virtual void GetFeatures(FtpReply reply) {
            foreach (string feat in reply.InfoMessages.Split('\n')) {
                if (feat.ToUpper().Contains("MLST") || feat.ToUpper().Contains("MLSD"))
                    m_caps |= FtpCapability.MLSD;
                else if (feat.ToUpper().Contains("MDTM"))
                    m_caps |= FtpCapability.MDTM;
                else if (feat.ToUpper().Contains("REST STREAM"))
                    m_caps |= FtpCapability.REST;
                else if (feat.ToUpper().Contains("SIZE"))
                    m_caps |= FtpCapability.SIZE;
                else if (feat.ToUpper().Contains("UTF8"))
                    m_caps |= FtpCapability.UTF8;
                else if (feat.ToUpper().Contains("PRET"))
                    m_caps |= FtpCapability.PRET;
                else if (feat.ToUpper().Contains("MFMT"))
                    m_caps |= FtpCapability.MFMT;
                else if (feat.ToUpper().Contains("MFCT"))
                    m_caps |= FtpCapability.MFCT;
                else if (feat.ToUpper().Contains("MFF"))
                    m_caps |= FtpCapability.MFF;
            }
        }

        delegate void AsyncConnect();

        /// <summary>
        /// Initiates a connection to the server
        /// </summary>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
        public IAsyncResult BeginConnect(AsyncCallback callback, object state) {
            AsyncConnect func;
            IAsyncResult ar;

            ar = (func = new AsyncConnect(Connect)).BeginInvoke(callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous connection attempt to the server
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginConnect()</param>
        /// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
        public void EndConnect(IAsyncResult ar) {
            GetAsyncDelegate<AsyncConnect>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Catches the socket stream ssl validation event and fires the event handlers
        /// attached to this object for validating SSL certificates
        /// </summary>
        /// <param name="stream">The stream that fired the event</param>
        /// <param name="e">The event args used to validate the certficate</param>
        void FireValidateCertficate(FtpSocketStream stream, FtpSslValidationEventArgs e) {
            OnValidateCertficate(e);
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public virtual void Disconnect() {
            try {
                m_lock.WaitOne();

                if (m_stream != null && m_stream.IsConnected) {
                    try {
                        Execute("QUIT");
                    }
                    finally {
                        m_stream.Close();
                    }
                }
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        delegate void AsyncDisconnect();

        /// <summary>
        /// Initiates a disconnection on the server
        /// </summary>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDisconnect.cs" lang="cs" /></example>
        public IAsyncResult BeginDisconnect(AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncDisconnect func;

            ar = (func = new AsyncDisconnect(Disconnect)).BeginInvoke(callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginDisconnect
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDisconnect</param>
        /// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
        public void EndDisconnect(IAsyncResult ar) {
            GetAsyncDelegate<AsyncDisconnect>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Opens the specified type of passive data stream
        /// </summary>
        /// <param name="type">Type of passive data stream to open</param>
        /// <param name="command">The command to execute that requires a data stream</param>
        /// <param name="args">Arguments to the command</param>
        /// <returns>A data stream ready to be used</returns>
        FtpDataStream OpenPassiveDataStream(FtpDataConnectionType type, string command, params object[] args) {
            FtpDataStream stream = null;
            FtpReply reply;
            Match m;
            string host = null;
            int port = 0;

            if (m_stream == null)
                throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open a passive data stream.");

            if (type == FtpDataConnectionType.EPSV || type == FtpDataConnectionType.AutoPassive) {
                if (!(reply = Execute("EPSV")).Success) {
                    // if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
                    if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoPassive && m_stream.LocalEndPoint.AddressFamily == Sockets.AddressFamily.InterNetwork)
                        return OpenPassiveDataStream(FtpDataConnectionType.PASV, command, args);
                    throw new FtpCommandException(reply);
                }

                m = Regex.Match(reply.Message, @"\(\|\|\|(?<port>\d+)\|\)");
                if (!m.Success) {
                    throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
                }

                host = m_host;
                port = int.Parse(m.Groups["port"].Value);
            }
            else {
                if (m_stream.LocalEndPoint.AddressFamily != Sockets.AddressFamily.InterNetwork)
                    throw new FtpException("Only IPv4 is supported by the PASV command. Use EPSV instead.");

                if (!(reply = Execute("PASV")).Success)
                    throw new FtpCommandException(reply);

                m = Regex.Match(reply.Message,
                    @"(?<quad1>\d+)," +
                    @"(?<quad2>\d+)," +
                    @"(?<quad3>\d+)," +
                    @"(?<quad4>\d+)," +
                    @"(?<port1>\d+)," +
                    @"(?<port2>\d+)"
                );

                if (!m.Success || m.Groups.Count != 7)
                    throw new FtpException(string.Format("Malformed PASV response: {0}", reply.Message));

                // PASVEX mode ignores the host supplied in the PASV response
                if (type == FtpDataConnectionType.PASVEX)
                    host = m_host;
                else
                    host = string.Format("{0}.{1}.{2}.{3}",
                        m.Groups["quad1"].Value,
                        m.Groups["quad2"].Value,
                        m.Groups["quad3"].Value,
                        m.Groups["quad4"].Value);

                port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);
            }

            stream = new FtpDataStream(this);
            stream.ConnectTimeout = DataConnectionConnectTimeout;
            stream.ReadTimeout = DataConnectionReadTimeout;
            stream.Connect(host, port);

            // and finally execute the command that needed the data stream
            if (!(reply = Execute(command, args)).Success) {
                stream.Close();
                throw new FtpCommandException(reply);
            }

            // this needs to take place after the command is executed
            if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None)
                stream.ActivateEncryption(m_host, 
                    this.ClientCertificates.Count > 0 ? this.ClientCertificates : null);

            // the command status is used to determine
            // if a reply needs to be read from the server
            // when the stream is closed so always set it
            // otherwise things can get out of sync.
            stream.CommandStatus = reply;

            return stream;
        }

        /// <summary>
        /// Opens the specified type of active data stream
        /// </summary>
        /// <param name="type">Type of passive data stream to open</param>
        /// <param name="command">The command to execute that requires a data stream</param>
        /// <param name="args">Arguments to the command</param>
        /// <returns>A data stream ready to be used</returns>
        FtpDataStream OpenActiveDataStream(FtpDataConnectionType type, string command, params object[] args) {
            FtpDataStream stream = new FtpDataStream(this);
            FtpReply reply;
            IAsyncResult ar;

            if (m_stream == null)
                throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open an active data stream.");

            stream.Listen(m_stream.LocalEndPoint.Address, 0);
            ar = stream.BeginAccept(null, null);

            if (type == FtpDataConnectionType.EPRT || type == FtpDataConnectionType.AutoActive) {
                int ipver = 0;

                switch (stream.LocalEndPoint.AddressFamily) {
                    case Sockets.AddressFamily.InterNetwork:
                        ipver = 1; // IPv4
                        break;
                    case Sockets.AddressFamily.InterNetworkV6:
                        ipver = 2; // IPv6
                        break;
                    default:
                        throw new InvalidOperationException("The IP protocol being used is not supported.");
                }

                if (!(reply = Execute("EPRT |{0}|{1}|{2}|", ipver,
                    stream.LocalEndPoint.Address.ToString(), stream.LocalEndPoint.Port)).Success) {
                    stream.Close();
                    // if we're connected with IPv4 and the data channel type is AutoActive then try to fall back to the PORT command
                    if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoActive && m_stream.LocalEndPoint.AddressFamily == Sockets.AddressFamily.InterNetwork)
                        return OpenActiveDataStream(FtpDataConnectionType.PORT, command, args);
                    throw new FtpCommandException(reply);
                }
            }
            else {
                if (m_stream.LocalEndPoint.AddressFamily != Sockets.AddressFamily.InterNetwork)
                    throw new FtpException("Only IPv4 is supported by the PORT command. Use EPRT instead.");

                if (!(reply = Execute("PORT {0},{1},{2}",
                        stream.LocalEndPoint.Address.ToString().Replace('.', ','),
                        stream.LocalEndPoint.Port / 256,
                        stream.LocalEndPoint.Port % 256)).Success) {
                    stream.Close();
                    throw new FtpCommandException(reply);
                }
            }

            if (!(reply = Execute(command, args)).Success) {
                stream.Close();
                throw new FtpCommandException(reply);
            }

            ar.AsyncWaitHandle.WaitOne(m_dataConnectionConnectTimeout);
            if (!ar.IsCompleted) {
                stream.Close();
                throw new TimeoutException("Timed out waiting for the server to connect to the active data socket.");
            }

            stream.EndAccept(ar);

            if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None)
                stream.ActivateEncryption(m_host,
                    this.ClientCertificates.Count > 0 ? this.ClientCertificates : null);

            stream.ReadTimeout = m_dataConnectionReadTimeout;
            stream.CommandStatus = reply;

            return stream;
        }

        /// <summary>
        /// Opens a data stream.
        /// </summary>
        /// <param name='command'>The command to execute that requires a data stream</param>
        /// <param name='args'>Format arguments.</param>
        /// <returns>The data stream.</returns>
        FtpDataStream OpenDataStream(string command, params object[] args) {
            FtpDataConnectionType type = m_dataConnectionType;
            FtpDataStream stream = null;
            FtpClient client = null;

            try {
                m_lock.WaitOne();

                if (!IsConnected)
                    Connect();

                if (m_threadSafeDataChannels) {
                    client = CloneConnection();
                    client.Connect();
                }
                else {
                    client = this;
                }

                // The PORT and PASV commands do not work with IPv6 so
                // if either one of those types are set change them
                // to EPSV or EPRT appropriately.

                if (client.m_stream.LocalEndPoint.AddressFamily == Sockets.AddressFamily.InterNetworkV6) {
                    switch (type) {
                        case FtpDataConnectionType.PORT:
                            type = FtpDataConnectionType.EPRT;
#if DEBUG
                            Debug.WriteLine("Changed data connection type to EPRT because we are connected with IPv6.");
#endif
                            break;
                        case FtpDataConnectionType.PASV:
                        case FtpDataConnectionType.PASVEX:
                            type = FtpDataConnectionType.EPSV;
#if DEBUG
                            Debug.WriteLine("Changed data connection type to EPSV because we are connected with IPv6.");
#endif
                            break;
                    }
                }

                switch (type) {
                    case FtpDataConnectionType.AutoPassive:
                    case FtpDataConnectionType.EPSV:
                    case FtpDataConnectionType.PASV:
                    case FtpDataConnectionType.PASVEX:
                        stream = client.OpenPassiveDataStream(type, command, args);
                        break;
                    case FtpDataConnectionType.AutoActive:
                    case FtpDataConnectionType.EPRT:
                    case FtpDataConnectionType.PORT:
                        stream = client.OpenActiveDataStream(type, command, args);
                        break;
                }

                if (stream == null)
                    throw new InvalidOperationException("The specified data channel type is not implemented.");
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return stream;
        }

        /// <summary>
        /// Disconnects a data stream
        /// </summary>
        /// <param name="stream">The data stream to close</param>
        internal void CloseDataStream(FtpDataStream stream) {
            if (stream == null)
                throw new ArgumentException("The data stream parameter was null");

            try {
                m_lock.WaitOne();

                if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
                    FtpReply reply;

                    if (!(reply = GetReply()).Success) {
                        throw new FtpCommandException(reply);
                    }
                }

                // if this is a clone of the original control
                // connection close this object out
                if (IsClone) {
                    Disconnect();
                    Dispose();
                }
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        public Stream OpenRead(string path) {
            return OpenRead(path, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        public Stream OpenRead(string path, FtpDataType type) {
            return OpenRead(path, type, 0);
        }

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="restart">Resume location</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        public Stream OpenRead(string path, long restart) {
            return OpenRead(path, FtpDataType.Binary, restart);
        }

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="restart">Resume location</param>
        /// <returns>A stream for reading the file on the server</returns>
        /// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
        public virtual Stream OpenRead(string path, FtpDataType type, long restart) {
            FtpDataStream stream = null;
            FtpReply reply;
            long length = 0;

            try {
                m_lock.WaitOne();

                SetDataType(type);
                length = GetFileSize(path);

                if (restart > 0) {
                    if (!(reply = Execute("REST {0}", restart)).Success)
                        throw new FtpCommandException(reply);
                }

                stream = OpenDataStream("RETR {0}", path.GetFtpPath());
            }
            finally {
                m_lock.ReleaseMutex();
            }

            if (stream != null) {
                if (length > 0)
                    stream.SetLength(length);

                if (restart > 0)
                    stream.SetPosition(restart);
            }

            return stream;
        }

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        public IAsyncResult BeginOpenRead(string path, AsyncCallback callback, object state) {
            return BeginOpenRead(path, FtpDataType.Binary, 0, callback, state);
        }

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        public IAsyncResult BeginOpenRead(string path, FtpDataType type, AsyncCallback callback, object state) {
            return BeginOpenRead(path, type, 0, callback, state);
        }

        /// <summary>
        /// Opens the specified file for reading
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="restart">Resume location</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        public IAsyncResult BeginOpenRead(string path, long restart, AsyncCallback callback, object state) {
            return BeginOpenRead(path, FtpDataType.Binary, restart, callback, state);
        }

        delegate Stream AsyncOpenRead(string path, FtpDataType type, long restart);

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
        public IAsyncResult BeginOpenRead(string path, FtpDataType type, long restart, AsyncCallback callback, object state) {
            AsyncOpenRead func;
            IAsyncResult ar;

            ar = (func = new AsyncOpenRead(OpenRead)).BeginInvoke(path, type, restart, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginOpenRead()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginOpenRead()</param>
        /// <returns>A readable stream</returns>
        /// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
        public Stream EndOpenRead(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncOpenRead>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
        public Stream OpenWrite(string path) {
            return OpenWrite(path, FtpDataType.Binary);
        }

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
        public virtual Stream OpenWrite(string path, FtpDataType type) {
            FtpDataStream stream = null;
            long length = 0;

            try {
                m_lock.WaitOne();

                SetDataType(type);
                length = GetFileSize(path);
                stream = OpenDataStream("STOR {0}", path.GetFtpPath());

                if (length > 0 && stream != null)
                    stream.SetLength(length);
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return stream;
        }

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
        public IAsyncResult BeginOpenWrite(string path, AsyncCallback callback, object state) {
            return BeginOpenWrite(path, FtpDataType.Binary, callback, state);
        }

        delegate Stream AsyncOpenWrite(string path, FtpDataType type);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
        public IAsyncResult BeginOpenWrite(string path, FtpDataType type, AsyncCallback callback, object state) {
            AsyncOpenWrite func;
            IAsyncResult ar;

            ar = (func = new AsyncOpenWrite(OpenWrite)).BeginInvoke(path, type, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginOpenWrite()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginOpenWrite()</param>
        /// <returns>A writable stream</returns>
        /// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
        public Stream EndOpenWrite(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncOpenWrite>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Opens the specified file to be appended to
        /// </summary>
        /// <param name="path">The full or relative path to the file to be opened</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
        public Stream OpenAppend(string path) {
            return OpenAppend(path, FtpDataType.Binary);
        }

        /// <summary>
        /// Opens the specified file to be appended to
        /// </summary>
        /// <param name="path">The full or relative path to the file to be opened</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for writing to the file on the server</returns>
        /// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
        public virtual Stream OpenAppend(string path, FtpDataType type) {
            FtpDataStream stream = null;
            long length = 0;

            try {
                m_lock.WaitOne();

                SetDataType(type);
                length = GetFileSize(path);
                stream = OpenDataStream("APPE {0}", path.GetFtpPath());

                if (length > 0 && stream != null) {
                    stream.SetLength(length);
                    stream.SetPosition(length);
                }
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return stream;
        }

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
        public IAsyncResult BeginOpenAppend(string path, AsyncCallback callback, object state) {
            return BeginOpenAppend(path, FtpDataType.Binary, callback, state);
        }

        delegate Stream AsyncOpenAppend(string path, FtpDataType type);

        /// <summary>
        /// Opens the specified file for writing
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
        public IAsyncResult BeginOpenAppend(string path, FtpDataType type, AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncOpenAppend func;

            ar = (func = new AsyncOpenAppend(OpenAppend)).BeginInvoke(path, type, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginOpenAppend()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginOpenWrite()</param>
        /// <returns>A writable stream</returns>
        /// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
        public Stream EndOpenAppend(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncOpenAppend>(ar).EndInvoke(ar);
        }

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
        public FtpListItem[] GetListing() {
            return GetListing(null);
        }

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
        public FtpListItem[] GetListing(string path) {
            return GetListing(path, FtpListOption.Modify | FtpListOption.Size);
        }

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
        public FtpListItem[] GetListing(string path, FtpListOption options) {
            List<FtpListItem> lst = new List<FtpListItem>();
            string listcmd = null;

            try {
                m_lock.WaitOne();

                if (path == null || path.Trim().Length == 0)
                    path = GetWorkingDirectory();

                // always get the file listing in binary
                // to avoid any potential character translation
                // problems that would happen if in ASCII.
                Execute("TYPE I");

                // MLSD provides a machine parsable format with more
                // accurate information than most of the UNIX long list
                // formats which translates to more effcient file listings
                // so always prefer MLSD over LIST unless the caller of this
                // method overrides it with the ForceList option
                if (!options.HasFlag(FtpListOption.ForceList) && m_caps.HasFlag(FtpCapability.MLSD))
                    listcmd = "MLSD";
                else {
                    if (options.HasFlag(FtpListOption.AllFiles))
                        listcmd = "LIST -a";
                    else
                        listcmd = "LIST";
                }


                using (FtpDataStream stream = OpenDataStream(string.Format("{0} {1}", listcmd, path.GetFtpPath()))) {
                    string buf;

                    try {
                        while ((buf = stream.ReadLine(Encoding)) != null) {
                            FtpListItem item = null;

#if DEBUG
                            Debug.WriteLine(buf);
#endif

                            item = FtpListItem.Parse(path, buf, Capabilities);
                            // FtpListItem.Parse() returns null if the line
                            // could not be parsed
                            if (item != null)
                                lst.Add(item);
                        }
                    }
                    finally {
                        stream.Close();
                    }

                    // don't loop through the files to load extended information
                    // unless an option pertaining to this operation has bee set
                    // by the caller of this method
                    if (options.HasFlag(FtpListOption.Modify) || options.HasFlag(FtpListOption.Size)) {
                        // do not try to load extended file system information until
                        // after the list retrieval is done because you cannot execute
                        // commands on the control connection while the data stream
                        // is open without triggering an error on the server
                        foreach (FtpListItem item in lst) {
                            if (options.HasFlag(FtpListOption.Modify)) {
                                // if the modified date was not loaded and the server supports
                                // the MDTM command and this is not a directory, load the modified date.
                                // most if not all servers do not support retrieving the modified date
                                // from a directory
                                if (item.Modified == DateTime.MinValue && item.Type != FtpFileSystemObjectType.Directory && m_caps.HasFlag(FtpCapability.MDTM))
                                    item.Modified = GetModifiedTime(item.FullName);
                            }

                            if (options.HasFlag(FtpListOption.Size)) {
                                // if no size was parsed, the object is a file and the server
                                // supports the SIZE command, then load the file size
                                if (item.Size == -1) {
                                    long size;

                                    if (item.Type == FtpFileSystemObjectType.File && m_caps.HasFlag(FtpCapability.SIZE) && (size = GetFileSize(item.FullName)) >= 0) {
                                        item.Size = size;
                                    }
                                    else {
                                        item.Size = 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return lst.ToArray();
        }

        /// <summary>
        /// Gets a file listing from the server asynchronously
        /// </summary>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        public IAsyncResult BeginGetListing(AsyncCallback callback, Object state) {
            return BeginGetListing(null, callback, state);
        }

        /// <summary>
        /// Gets a file listing from the server asynchronously
        /// </summary>
        /// <param name="path">The path to list</param>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        public IAsyncResult BeginGetListing(string path, AsyncCallback callback, Object state) {
            return BeginGetListing(path, FtpListOption.Modify | FtpListOption.Size, callback, state);
        }


        delegate FtpListItem[] AsyncGetListing(string path, FtpListOption options);

        /// <summary>
        /// Gets a file listing from the server asynchronously
        /// </summary>
        /// <param name="path">The path to list</param>
        /// <param name="options">Options that dictate how the list operation is performed</param>
        /// <param name="callback">AsyncCallback method</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        public IAsyncResult BeginGetListing(string path, FtpListOption options, AsyncCallback callback, Object state) {
            IAsyncResult ar;
            AsyncGetListing func;

            ar = (func = new AsyncGetListing(GetListing)).BeginInvoke(path, options, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous file listing
        /// </summary>
        /// <param name="ar">IAsyncResult return from BeginGetListing()</param>
        /// <returns>An array of items retrieved in the listing</returns>
        /// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
        public FtpListItem[] EndGetListing(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncGetListing>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Sets the data type of information sent over the data stream
        /// </summary>
        /// <param name="type">ASCII/Binary</param>
        public void SetDataType(FtpDataType type) {
            FtpReply reply;

            try {
                m_lock.WaitOne();

                switch (type) {
                    case FtpDataType.ASCII:
                        reply = Execute("TYPE A");
                        break;
                    case FtpDataType.Binary:
                        reply = Execute("TYPE I");
                        break;
                    default:
                        throw new FtpException("Unsupported data type: " + type.ToString());
                }

                if (!reply.Success)
                    throw new FtpCommandException(reply);
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        delegate void AsyncSetDataType(FtpDataType type);

        /// <summary>
        /// Asynchronously sets the data type on the server
        /// </summary>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginSetDataType.cs" lang="cs" /></example>
        public IAsyncResult BeginSetDataType(FtpDataType type, AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncSetDataType func;

            ar = (func = new AsyncSetDataType(SetDataType)).BeginInvoke(type, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginSetDataType()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginSetDataType()</param>
        /// <example><code source="..\Examples\BeginSetDataType.cs" lang="cs" /></example>
        public void EndSetDataType(IAsyncResult ar) {
            GetAsyncDelegate<AsyncSetDataType>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Sets the work directory on the server
        /// </summary>
        /// <param name="path">The path of the directory to change to</param>
        /// <example><code source="..\Examples\SetWorkingDirectory.cs" lang="cs" /></example>
        public void SetWorkingDirectory(string path) {
            FtpReply reply;

            try {
                m_lock.WaitOne();

                if (!(reply = Execute("CWD {0}", path.GetFtpPath())).Success)
                    throw new FtpCommandException(reply);
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        delegate void AsyncSetWorkingDirectory(string path);

        /// <summary>
        /// Asynchronously changes the working directory on the server
        /// </summary>
        /// <param name="path">The directory to change to</param>
        /// <param name="callback">Async Callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
        public IAsyncResult BeginSetWorkingDirectory(string path, AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncSetWorkingDirectory func;

            ar = (func = new AsyncSetWorkingDirectory(SetWorkingDirectory)).BeginInvoke(path, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends asynchronous directory change
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginSetWorkingDirectory</param>
        /// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
        public void EndSetWorkingDirectory(IAsyncResult ar) {
            GetAsyncDelegate<AsyncSetWorkingDirectory>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Gets the current working directory
        /// </summary>
        /// <returns>The current working directory</returns>
        /// <example><code source="..\Examples\GetWorkingDirectory.cs" lang="cs" /></example>
        public string GetWorkingDirectory() {
            FtpReply reply;
            Match m;

            try {
                m_lock.WaitOne();

                if (!(reply = Execute("PWD")).Success)
                    throw new FtpCommandException(reply);
            }
            finally {
                m_lock.ReleaseMutex();
            }

            if (!(m = Regex.Match(reply.Message, "\"(?<pwd>.*)\"")).Success)
                throw new FtpException("Failed to parse working directory from: " + reply.Message);

            return m.Groups["pwd"].Value;
        }

        delegate string AsyncGetWorkingDirectory();

        /// <summary>
        /// Asynchronously retrieves the working directory
        /// </summary>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
        public IAsyncResult BeginGetWorkingDirectory(AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncGetWorkingDirectory func;

            ar = (func = new AsyncGetWorkingDirectory(GetWorkingDirectory)).BeginInvoke(callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous call to retrieve the working directory
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetWorkingDirectory</param>
        /// <returns>The current working directory</returns>
        /// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
        public string EndGetWorkingDirectory(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncGetWorkingDirectory>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Gets the size of the file
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <returns>-1 if the command fails, otherwise the file size</returns>
        /// <example><code source="..\Examples\GetFileSize.cs" lang="cs" /></example>
        public virtual long GetFileSize(string path) {
            FtpReply reply;
            long length = 0;

            try {
                m_lock.WaitOne();

                if ((reply = Execute("SIZE {0}", path.GetFtpPath())).Success && !long.TryParse(reply.Message, out length))
                    length = -1;
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return length;
        }

        delegate long AsyncGetFileSize(string path);

        /// <summary>
        /// Asynchronously retrieve the size of the specified file
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
        public IAsyncResult BeginGetFileSize(string path, AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncGetFileSize func;

            ar = (func = new AsyncGetFileSize(GetFileSize)).BeginInvoke(path, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginGetFileSize()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetFileSize</param>
        /// <returns>The size of the file, -1 if there was a problem.</returns>
        /// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
        public long EndGetFileSize(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncGetFileSize>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Gets the modified time of the file
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <returns>The modified time, DateTime.MinValue if there was a problem</returns>
        /// <example><code source="..\Examples\GetModifiedTime.cs" lang="cs" /></example>
        public virtual DateTime GetModifiedTime(string path) {
            DateTime modify = DateTime.MinValue;
            FtpReply reply;

            try {
                m_lock.WaitOne();

                if ((reply = Execute("MDTM {0}", path.GetFtpPath())).Success)
                    modify = reply.Message.GetFtpDate();
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return modify;
        }

        delegate DateTime AsyncGetModifiedTime(string path);

        /// <summary>
        /// Gets the modified time of the file
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
        public IAsyncResult BeginGetModifiedTime(string path, AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncGetModifiedTime func;

            ar = (func = new AsyncGetModifiedTime(GetModifiedTime)).BeginInvoke(path, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginGetModifiedTime()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetModifiedTime()</param>
        /// <returns>The modified time, DateTime.MinValue if there was a problem</returns>
        /// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
        public DateTime EndGetModifiedTime(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncGetModifiedTime>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Deletes a file on the server
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <example><code source="..\Examples\DeleteFile.cs" lang="cs" /></example>
        public void DeleteFile(string path) {
            FtpReply reply;

            try {
                m_lock.WaitOne();

                if (!(reply = Execute("DELE {0}", path.GetFtpPath())).Success)
                    throw new FtpCommandException(reply);
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        delegate void AsyncDeleteFile(string path);

        /// <summary>
        /// Asynchronously deletes a file from the server
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
        public IAsyncResult BeginDeleteFile(string path, AsyncCallback callback, object state) {
            IAsyncResult ar;
            AsyncDeleteFile func;

            ar = (func = new AsyncDeleteFile(DeleteFile)).BeginInvoke(path, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginDeleteFile
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDeleteFile</param>
        /// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
        public void EndDeleteFile(IAsyncResult ar) {
            GetAsyncDelegate<AsyncDeleteFile>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Deletes the specified directory on the server.
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        public void DeleteDirectory(string path) {
            DeleteDirectory(path, false, FtpListOption.AllFiles | FtpListOption.ForceList);
        }

        /// <summary>
        /// Delets the specified directory on the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        public void DeleteDirectory(string path, bool force) {
            DeleteDirectory(path, force, FtpListOption.AllFiles | FtpListOption.ForceList);
        }

        /// <summary>
        /// Deletes the specified directory on the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <param name="options">FtpListOptions for controlling how the directory
        /// contents are retrieved with the force option is true. The recommended value
        /// here is FtpListOption.AllFiles | FtpListOption.ForceList however these
        /// parameters may not work on all servers. If you experience problems
        /// the file listing can be fine tuned through this parameter.</param>
        /// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
        public void DeleteDirectory(string path, bool force, FtpListOption options) {
            FtpReply reply;

            try {
                m_lock.WaitOne();

                if (force) {
                    // force the LIST -a command so hidden files
                    // and folders get removed too
                    foreach (FtpListItem item in GetListing(path, options)) {
                        switch (item.Type) {
                            case FtpFileSystemObjectType.File:
                                DeleteFile(item.FullName);
                                break;
                            case FtpFileSystemObjectType.Directory:
                                DeleteDirectory(item.FullName, true, options);
                                break;
                            default:
                                throw new FtpException("Don't know how to delete object type: " + item.Type);
                        }
                    }
                }

                if (!(reply = Execute("RMD {0}", path.GetFtpPath())).Success)
                    throw new FtpCommandException(reply);
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        delegate void AsyncDeleteDirectory(string path, bool force, FtpListOption options);

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        public IAsyncResult BeginDeleteDirectory(string path, AsyncCallback callback, object state) {
            return BeginDeleteDirectory(path, true, FtpListOption.ForceList | FtpListOption.AllFiles, callback, state);
        }

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        public IAsyncResult BeginDeleteDirectory(string path, bool force, AsyncCallback callback, object state) {
            return BeginDeleteDirectory(path, force, FtpListOption.ForceList | FtpListOption.AllFiles, callback, state);
        }

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <param name="options">FtpListOptions for controlling how the directory
        /// contents are retrieved with the force option is true. The recommended value
        /// here is FtpListOption.AllFiles | FtpListOption.ForceList however these
        /// parameters may not work on all servers. If you experience problems
        /// the file listing can be fine tuned through this parameter.</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        public IAsyncResult BeginDeleteDirectory(string path, bool force, FtpListOption options, AsyncCallback callback, object state) {
            AsyncDeleteDirectory func;
            IAsyncResult ar;

            ar = (func = new AsyncDeleteDirectory(DeleteDirectory)).BeginInvoke(path, force, options, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginDeleteDirectory()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDeleteDirectory</param>
        /// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
        public void EndDeleteDirectory(IAsyncResult ar) {
            GetAsyncDelegate<AsyncDeleteDirectory>(ar).EndInvoke(ar);
        }

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
        public bool DirectoryExists(string path) {
            string pwd;

            if (path.GetFtpPath() == "/")
                return true;

            try {
                m_lock.WaitOne();
                pwd = GetWorkingDirectory();

                if (Execute("CWD {0}", path.GetFtpPath()).Success) {
                    FtpReply reply = Execute("CWD {0}", pwd.GetFtpPath());

                    if (!reply.Success)
                        throw new FtpException("DirectoryExists(): Failed to restore the working directory.");

                    return true;
                }
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return false;
        }

        delegate bool AsyncDirectoryExists(string path);

        /// <summary>
        /// Checks if a directory exists on the server asynchronously.
        /// </summary>
        /// <returns>IAsyncResult</returns>
        /// <param name='path'>The full or relative path of the directory to check for</param>
        /// <param name='callback'>Async callback</param>
        /// <param name='state'>State object</param>
        /// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
        public IAsyncResult BeginDirectoryExists(string path, AsyncCallback callback, object state) {
            AsyncDirectoryExists func;
            IAsyncResult ar;

            ar = (func = new AsyncDirectoryExists(DirectoryExists)).BeginInvoke(path, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginDirectoryExists
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginDirectoryExists</param>
        /// <returns>True if the directory exists. False otherwise.</returns>
        /// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
        public bool EndDirectoryExists(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncDirectoryExists>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Checks if a file exsts on the server by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <returns>True if the file exists</returns>
        /// <example><code source="..\Examples\FileExists.cs" lang="cs" /></example>
        public bool FileExists(string path) {
            return FileExists(path, FtpListOption.AllFiles | FtpListOption.ForceList);
        }

        /// <summary>
        /// Checks if a file exsts on the server by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <param name="options">Options for controling the file listing used to
        /// determine if the file exists. The recommended value here is
        /// FtpListOption.AllFiles | FtpListOption.ForceList to ensure that
        /// hidden files are included in the listing however some servers
        /// may not support this feature.</param>
        /// <returns>True if the file exists</returns>
        /// <example><code source="..\Examples\FileExists.cs" lang="cs" /></example>
        public bool FileExists(string path, FtpListOption options) {
            string dirname = path.GetFtpDirectoryName();

            try {
                m_lock.WaitOne();

                if (!DirectoryExists(dirname))
                    return false;

                foreach (FtpListItem item in GetListing(dirname, options))
                    if (item.Type == FtpFileSystemObjectType.File && item.Name == path.GetFtpFileName())
                        return true;
            }
            finally {
                m_lock.ReleaseMutex();
            }

            return false;
        }

        delegate bool AsyncFileExists(string path, FtpListOption options);

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
        public IAsyncResult BeginFileExists(string path, AsyncCallback callback, object state) {
            return BeginFileExists(path, FtpListOption.AllFiles | FtpListOption.ForceList, callback, state);
        }

        /// <summary>
        /// Checks if a file exsts on the server by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <param name="options">Options for controling the file listing used to
        /// determine if the file exists. The recommended value here is
        /// FtpListOption.AllFiles | FtpListOption.ForceList to ensure that
        /// hidden files are included in the listing however some servers
        /// may not support this feature.</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
        public IAsyncResult BeginFileExists(string path, FtpListOption options, AsyncCallback callback, object state) {
            AsyncFileExists func;
            IAsyncResult ar;

            ar = (func = new AsyncFileExists(FileExists)).BeginInvoke(path, options, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginFileExists
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginFileExists</param>
        /// <returns>True if the file exists</returns>
        /// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
        public bool EndFileExists(IAsyncResult ar) {
            return GetAsyncDelegate<AsyncFileExists>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Creates a directory on the server. If the preceding
        /// directories do not exist they are created.
        /// </summary>
        /// <param name="path">The full or relative path to the new directory</param>
        /// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
        public void CreateDirectory(string path) {
            CreateDirectory(path, true);
        }

        /// <summary>
        /// Creates a directory on the server
        /// </summary>
        /// <param name="path">The full or relative path to the directory to create</param>
        /// <param name="force">Try to force all non-existant pieces of the path to be created</param>
        /// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
        public void CreateDirectory(string path, bool force) {
            FtpReply reply;

            if (path.GetFtpPath() == "/")
                return;

            try {
                m_lock.WaitOne();

                if (force && !DirectoryExists(path.GetFtpDirectoryName()))
                    CreateDirectory(path.GetFtpDirectoryName(), true);
                else if (DirectoryExists(path))
                    return;

                if (!(reply = Execute("MKD {0}", path.GetFtpPath())).Success)
                    throw new FtpCommandException(reply);
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        delegate void AsyncCreateDirectory(string path, bool force);

        /// <summary>
        /// Creates a directory asynchronously
        /// </summary>
        /// <param name="path">The full or relative path to the directory to create</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
        public IAsyncResult BeginCreateDirectory(string path, AsyncCallback callback, object state) {
            return BeginCreateDirectory(path, true, callback, state);
        }

        /// <summary>
        /// Creates a directory asynchronously
        /// </summary>
        /// <param name="path">The full or relative path to the directory to create</param>
        /// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
        public IAsyncResult BeginCreateDirectory(string path, bool force, AsyncCallback callback, object state) {
            AsyncCreateDirectory func;
            IAsyncResult ar;

            ar = (func = new AsyncCreateDirectory(CreateDirectory)).BeginInvoke(path, force, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginCreateDirectory
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginCreateDirectory</param>
        /// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
        public void EndCreateDirectory(IAsyncResult ar) {
            GetAsyncDelegate<AsyncCreateDirectory>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Renames an object on the remote file system.
        /// </summary>
        /// <param name="path">The full or relative path to the object</param>
        /// <param name="dest">The old or new full or relative path including the new name of the object</param>
        /// <example><code source="..\Examples\Rename.cs" lang="cs" /></example>
        public void Rename(string path, string dest) {
            FtpReply reply;

            try {
                m_lock.WaitOne();

                if (!(reply = Execute("RNFR {0}", path.GetFtpPath())).Success)
                    throw new FtpCommandException(reply);

                if (!(reply = Execute("RNTO {0}", dest.GetFtpPath())).Success)
                    throw new FtpCommandException(reply);
            }
            finally {
                m_lock.ReleaseMutex();
            }
        }

        delegate void AsyncRename(string path, string dest);

        /// <summary>
        /// Asynchronously renames an object on the server
        /// </summary>
        /// <param name="path">The full or relative path to the object</param>
        /// <param name="dest">The old or new full or relative path including the new name of the object</param>
        /// <param name="callback">Async callback</param>
        /// <param name="state">State object</param>
        /// <returns>IAsyncResult</returns>
        /// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
        public IAsyncResult BeginRename(string path, string dest, AsyncCallback callback, object state) {
            AsyncRename func;
            IAsyncResult ar;

            ar = (func = new AsyncRename(Rename)).BeginInvoke(path, dest, callback, state);
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends a call to BeginRename
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginRename</param>
        /// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
        public void EndRename(IAsyncResult ar) {
            GetAsyncDelegate<AsyncRename>(ar).EndInvoke(ar);
        }

        /// <summary>
        /// Disconnects from the server, releases resources held by this
        /// object.
        /// </summary>
        public void Dispose() {
            if (m_stream != null) {
                if (m_stream.IsConnected)
                    m_stream.Close();
                m_stream.Dispose();
                m_stream = null;
            }

            m_credentials = null;
            m_textEncoding = null;
            m_host = null;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~FtpClient() {
            Dispose();
        }

        /// <summary>
        /// Creates a new isntance of FtpClient
        /// </summary>
        public FtpClient() { }

        /// <summary>
        /// Connects to the specified URI. If the path specified by the URI ends with a
        /// / then the working directory is changed to the path specified.
        /// </summary>
        /// <param name="uri">The URI to parse</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <returns>FtpClient object</returns>
        public static FtpClient Connect(Uri uri, bool checkcertificate) {
            FtpClient cl = new FtpClient();

            if (uri == null)
                throw new ArgumentException("Invalid URI object");

            switch (uri.Scheme.ToLower()) {
                case "ftp":
                case "ftps":
                    break;
                default:
                    throw new UriFormatException("The specified URI scheme is not support. Please use ftp:// or ftps://");
            }

            cl.Host = uri.Host;
            cl.Port = uri.Port;

            if (uri.UserInfo != null && uri.UserInfo.Length > 0) {
                if (uri.UserInfo.Contains(":")) {
                    string[] parts = uri.UserInfo.Split(':');

                    if (parts.Length != 2)
                        throw new UriFormatException("The user info portion of the URI contains more than 1 colon. The username and password portion of the URI should be URL encoded.");

                    cl.Credentials = new NetworkCredential(HttpUtility.UrlDecode(parts[0]), HttpUtility.UrlDecode(parts[1]));
                }
                else
                    cl.Credentials = new NetworkCredential(HttpUtility.UrlDecode(uri.UserInfo), "");
            }
            else {
                // if no credentials were supplied just make up
                // some for anonymous authentication.
                cl.Credentials = new NetworkCredential("ftp", "ftp");
            }

            cl.ValidateCertificate += new FtpSslValidation(delegate(FtpClient control, FtpSslValidationEventArgs e) {
                if (e.PolicyErrors != Security.SslPolicyErrors.None && checkcertificate)
                    e.Accept = false;
                else
                    e.Accept = true;
            });

            cl.Connect();

            if (uri.PathAndQuery != null && uri.PathAndQuery.EndsWith("/"))
                cl.SetWorkingDirectory(uri.PathAndQuery);

            return cl;
        }

        /// <summary>
        /// Connects to the specified URI. If the path specified by the URI ends with a
        /// / then the working directory is changed to the path specified.
        /// </summary>
        /// <param name="uri">The URI to parse</param>
        /// <returns>FtpClient object</returns>
        public static FtpClient Connect(Uri uri) {
            return Connect(uri, true);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <param name="datatype">ASCII/Binary mode</param>
        /// <param name="restart">Restart location</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
        public static Stream OpenRead(Uri uri, bool checkcertificate, FtpDataType datatype, long restart) {
            FtpClient cl = null;

            if (uri.PathAndQuery == null || uri.PathAndQuery.Length == 0)
                throw new UriFormatException("The supplied URI does not contain a valid path.");

            if (uri.PathAndQuery.EndsWith("/"))
                throw new UriFormatException("The supplied URI points at a directory.");

            cl = Connect(uri, checkcertificate);
            cl.EnableThreadSafeDataConnections = false;

            return cl.OpenRead(uri.PathAndQuery, datatype, restart);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <param name="datatype">ASCII/Binary mode</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
        public static Stream OpenRead(Uri uri, bool checkcertificate, FtpDataType datatype) {
            return OpenRead(uri, checkcertificate, datatype, 0);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
        public static Stream OpenRead(Uri uri, bool checkcertificate) {
            return OpenRead(uri, checkcertificate, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
        public static Stream OpenRead(Uri uri) {
            return OpenRead(uri, true, FtpDataType.Binary, 0);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <param name="datatype">ASCII/Binary mode</param> 
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenWriteURI.cs" lang="cs" /></example>
        public static Stream OpenWrite(Uri uri, bool checkcertificate, FtpDataType datatype) {
            FtpClient cl = null;

            if (uri.PathAndQuery == null || uri.PathAndQuery.Length == 0)
                throw new UriFormatException("The supplied URI does not contain a valid path.");

            if (uri.PathAndQuery.EndsWith("/"))
                throw new UriFormatException("The supplied URI points at a directory.");

            cl = Connect(uri, checkcertificate);
            cl.EnableThreadSafeDataConnections = false;

            return cl.OpenWrite(uri.PathAndQuery, datatype);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenWriteURI.cs" lang="cs" /></example>
        public static Stream OpenWrite(Uri uri, bool checkcertificate) {
            return OpenWrite(uri, checkcertificate, FtpDataType.Binary);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenWriteURI.cs" lang="cs" /></example>
        public static Stream OpenWrite(Uri uri) {
            return OpenWrite(uri, true, FtpDataType.Binary);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <param name="datatype">ASCII/Binary mode</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenAppendURI.cs" lang="cs" /></example>
        public static Stream OpenAppend(Uri uri, bool checkcertificate, FtpDataType datatype) {
            FtpClient cl = null;

            if (uri.PathAndQuery == null || uri.PathAndQuery.Length == 0)
                throw new UriFormatException("The supplied URI does not contain a valid path.");

            if (uri.PathAndQuery.EndsWith("/"))
                throw new UriFormatException("The supplied URI points at a directory.");

            cl = Connect(uri, checkcertificate);
            cl.EnableThreadSafeDataConnections = false;

            return cl.OpenAppend(uri.PathAndQuery, datatype);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenAppendURI.cs" lang="cs" /></example>
        public static Stream OpenAppend(Uri uri, bool checkcertificate) {
            return OpenAppend(uri, checkcertificate, FtpDataType.Binary);
        }

        /// <summary>
        /// Opens a stream to the file specified by the URI
        /// </summary>
        /// <param name="uri">FTP/FTPS URI pointing at a file</param>
        /// <returns>Stream object</returns>
        /// <example><code source="..\Examples\OpenAppendURI.cs" lang="cs" /></example>
        public static Stream OpenAppend(Uri uri) {
            return OpenAppend(uri, true, FtpDataType.Binary);
        }

        /// <summary>
        /// Used internally to mark properties in the control connection that
        /// should be cloned when opening a data connection.
        /// </summary>
        sealed class FtpControlConnectionClone : Attribute {
        }
    }
}
