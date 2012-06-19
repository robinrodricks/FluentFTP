using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Net.FtpClient.Proxy;

namespace System.Net.FtpClient {
    /// <summary>
    /// Stream for reading and writing FTP data channels
    /// </summary>
    public abstract class FtpDataStream : Stream, IDisposable {
        FtpControlConnection _channel = null;
        /// <summary>
        /// Command channel this data stream is associated with
        /// </summary>
        public FtpControlConnection ControlConnection {
            get { return _channel; }
            protected set { _channel = value; }
        }

        bool _started = false;
        /// <summary>
        /// Gets a value indicating if the transfer has started.
        /// </summary>
        public bool TransferStarted {
            get { return _started; }
            private set { _started = value; }
        }

        DateTime _lastNoOp = DateTime.Now;
        /// <summary>
        /// Gets or sets the last time the NOOP command was
        /// executed on the server
        /// </summary>
        protected DateTime LastNoOp {
            get { return _lastNoOp; }
            set { _lastNoOp = value; }
        }

        /// <summary>
        /// Gets the receive buffer size of the underlying socket
        /// </summary>
        public int ReceiveBufferSize {
            get {
                if (this._socket != null) {
                    return this._socket.ReceiveBufferSize;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the send buffer size of the underlying socket
        /// </summary>
        public int SendBufferSize {
            get {
                if (this._socket != null) {
                    return this._socket.SendBufferSize;
                }

                return 0;
            }
        }

        Socket _socket = null;
        /// <summary>
        /// Socket used for communication
        /// </summary>
        protected Socket Socket {
            get {
                if (this._socket == null) {
                    this._socket = new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    (this._socket as ProxySocket).ProxyType = ProxyType.None;
                    if (this.ControlConnection.ProxyType != ProxyType.None) {
                        (this._socket as ProxySocket).ProxyType = this.ControlConnection.ProxyType;
                        (this._socket as ProxySocket).ProxyEndPoint = new IPEndPoint(IPAddress.Parse(this.ControlConnection.ProxyHost), this.ControlConnection.ProxyPort);
                        (this._socket as ProxySocket).ProxyUsername = this.ControlConnection.ProxyUsername;
                        (this._socket as ProxySocket).ProxyPassword = this.ControlConnection.ProxyPassword;
                    }
                }

                return this._socket;
            }

            set {
                if (this._reader != null) {
                    //this._reader.Dispose();
                    this._reader = null;
                }

                if (this._sslstream != null) {
                    this._sslstream.Dispose();
                    this._sslstream = null;
                }

                if (this._netstream != null) {
                    this._netstream.Dispose();
                    this._netstream = null;
                }

                if (this._socket != null) {
                    if (this._socket.Connected) {
                        this._socket.Shutdown(SocketShutdown.Both);
                        this._socket.Disconnect(false);
                    }

                    // doesn't work in .net 2
                    //this._socket.Dispose();
                }

                this._socket = value;
            }
        }

        NetworkStream _netstream = null;
        /// <summary>
        /// Base unencrypted network stream
        /// </summary>
        private NetworkStream NetworkStream {
            get {
                if (this._netstream == null && this._socket != null) {
                    this._netstream = new NetworkStream(this.Socket);
                }

                return this._netstream;
            }

            set {
                this._netstream = value;
            }
        }

        SslStream _sslstream = null;
        /// <summary>
        /// SSL Encrypted stream for ssl use
        /// </summary>
        private SslStream SecureStream {
            get {
                if (_sslstream == null && this.NetworkStream != null) {
                    this._sslstream = new SslStream(this.NetworkStream, true,
                        new RemoteCertificateValidationCallback(CheckCertificate));
                }

                return this._sslstream;
            }
        }

        /// <summary>
        /// Base stream
        /// </summary>
        private Stream BaseStream {
            get {
                if (this.ControlConnection.SslEnabled && this.ControlConnection.DataChannelEncryption) {
                    if (!this.SslEnabled) {
                        this.Authenticate();
                    }

                    return this.SecureStream;
                }
                else {
                    return this.NetworkStream;
                }
            }
        }

        StreamReader _reader = null;
        /// <summary>
        /// Gets a stream reader object
        /// </summary>
        protected StreamReader StreamReader {
            get {
                if (_reader == null && this._netstream != null) {

                    if (_channel.HasCapability(FtpCapability.UTF8)) {
                        _reader = new StreamReader(this, System.Text.Encoding.UTF8);
                    }
                    else {
                        _reader = new StreamReader(this, System.Text.Encoding.Default);
                    }
                }

                return _reader;
            }
        }

        int _readTimeout = -1;
        /// <summary>
        /// Gets or sets the length of time in miliseconds that this stream will
        /// attempt to read from the underlying socket before giving up. The default
        /// value of -1 means wait indefinitely.
        /// </summary>
        public override int ReadTimeout {
            get {
                return this._readTimeout;
            }
            set {
                this._readTimeout = value;
            }
        }

        long _length = 0;
        /// <summary>
        /// Gets the length of the stream
        /// </summary>
        public override long Length {
            get { return _length; }
        }

        long _position = 0;
        /// <summary>
        /// Gets or sets the position in the stream. Once a transfer
        /// has started, the position cannot be modified. If an attempt
        /// is made an exception will be thrown.
        /// </summary>
        public override long Position {
            get {
                return this._position;
            }
            set {
                if (this.TransferStarted) {
                    throw new IOException("FTP stream seeking cannot be done after the transfer has started.");
                }

                this.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Gets a value indicating if this stream is seekable. 
        /// If you are doing a stream mode transfer and the transfer has not
        /// yet started you can use seeking to resume your transfer.
        /// </summary>
        public override bool CanSeek {
            get {
                return this.TransferStarted ? false : true;
            }
        }

        /// <summary>
        /// Gets a value indicating if this stream can be read
        /// </summary>
        public override bool CanRead {
            get { return this.BaseStream != null ? this.BaseStream.CanRead : false; }
        }

        /// <summary>
        /// Gets a value indicating if this stream can be
        /// written to.
        /// </summary>
        public override bool CanWrite {
            get { return this.BaseStream != null ? this.BaseStream.CanWrite : false; }
        }

        /// <summary>
        /// Gets a value indicating if encryption is in use
        /// </summary>
        public bool SslEnabled {
            get {
                if (this.Socket.Connected) {
                    return this._sslstream != null && this._sslstream.IsEncrypted;
                }

                return false;
            }
        }

        SslPolicyErrors _policyErrors = SslPolicyErrors.None;
        /// <summary>
        /// Gets the SSL errors if there were any
        /// </summary>
        public SslPolicyErrors SslPolicyErrors {
            get { return _policyErrors; }
            private set { _policyErrors = value; }
        }

        X509Certificate _sslCertificate = null;
        /// <summary>
        /// Gets the certificate associated with the current connection
        /// </summary>
        public X509Certificate SslCertificate {
            get { return _sslCertificate; }
            private set { _sslCertificate = value; }
        }

        /// <summary>
        /// Checks if a certificate is valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        bool CheckCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            this.SslPolicyErrors = sslPolicyErrors;
            this.SslCertificate = certificate;
            // we don't care if there are policy errors on
            // this connection because if we're at the point
            // that a data stream is being opened then it means
            // the ssl certificate on the control connection
            // was already accepted.
            return true;
        }

        /// <summary>
        /// Validates the SSL certificate if security is in use
        /// </summary>
        void Authenticate() {
            if (this.Socket.Connected && !this.SslEnabled) {
                //this.SecurteStream.AuthenticateAsClient(((IPEndPoint)this.RemoteEndPoint).Address.ToString());
                this.SecureStream.AuthenticateAsClient(this.ControlConnection.Server);
            }
        }

        /// <summary>
        /// Writes bytes to the stream. If the transfer mode is block
        /// a block header is encoded and sent with the data.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count) {
            if (!this.CanWrite) {
                throw new IOException("This stream is not writeable!");
            }

            if (this.BaseStream == null) {
                throw new IOException("The base stream is null. Has a socket connection been opened yet?");
            }

            // keep-alive hack
            if (this.ControlConnection.KeepAliveInterval > 0 && DateTime.Now.Subtract(this.LastNoOp).Seconds >= this.ControlConnection.KeepAliveInterval) {
                this.ControlConnection.Execute("NOOP");
                this.LastNoOp = DateTime.Now;
            }

            this.BaseStream.Write(buffer, offset, count);
            this._position += count;
        }

        /// <summary>
        /// Read bytes off the data stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count) {
            IAsyncResult res;
            int read = 0;

            if (!this.CanRead) {
                //throw new IOException("This stream is not readable!");
                return 0;
            }

            if (this.BaseStream == null) {
                //throw new IOException("The base stream is null. Has a socket connection been opened yet?");
                return 0;
            }

            if (this._socket == null || !this._socket.Connected) {
                return 0;
            }

            // old blocking read
            //read = this.BaseStream.Read(buffer, offset, count);
            //this._position += read;

            // new read code that supports read timeout
            res = this.BaseStream.BeginRead(buffer, offset, count, null, null);
            res.AsyncWaitHandle.WaitOne(this.ReadTimeout);
            if (!res.IsCompleted) {
                // can just set this.Socket = null due to strange bug
                // where _socket gets reset and causes another exception
                // in the Close method of this class.
                this._socket.Close();
                this._socket = null;
                this._netstream = null;
                this._reader = null;
                throw new IOException("Timed out waiting for a response on the data channel.");
            }

            read = this.BaseStream.EndRead(res);
            this._position += read;
            // end new read code

            // if EOF close stream
            if (read == 0) {
                this.Close();
            }
            else {
                // keep-alive hack
                if (this.ControlConnection.KeepAliveInterval > 0 && DateTime.Now.Subtract(this.LastNoOp).Seconds >= this.ControlConnection.KeepAliveInterval) {
                    this.ControlConnection.Execute("NOOP");
                    this.LastNoOp = DateTime.Now;
                }
            }

            return read;
        }

        /// <summary>
        /// Reads a line off the data stream
        /// </summary>
        /// <returns></returns>
        public string ReadLine() {
            string data = null;

            if (!this.CanRead) {
                throw new IOException("This stream is not readable!");
            }

            if (this.BaseStream == null) {
                throw new IOException("The base stream is null. Has a socket connection been opened yet?");
            }

            data = this.StreamReader.ReadLine();
            ControlConnection.WriteLineToLogStream("> " + data);

            return data;
        }

        /// <summary>
        /// Sets the length of the stream.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value) {
            this._length = value;
        }

        /// <summary>
        /// Seek the stream to the specified position
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public long Seek(long offset) {
            return this.Seek(offset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Uses the REST command to set the stream position in stream
        /// mode transfers. If the transfer has started an exception
        /// will be triggered because REST cannot be executed once data
        /// has been exchanged.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) {
            /*if (!this.ControlConnection.HasCapability(FtpCapability.REST)) {
                throw new IOException("The FTP server does not support stream seeking.");
            }*/

            if (this.TransferStarted) {
                throw new IOException("FTP stream seeking cannot be done after the transfer has started.");
            }

            if (origin != SeekOrigin.Begin) {
                throw new IOException("The only seek origin that is supported is SeekOrigin.Begin");
            }

            try {
                this.ControlConnection.LockControlConnection();
                if (!this.ControlConnection.Execute("REST {0}", offset)) {
                    throw new FtpCommandException(this.ControlConnection);
                }
            }
            finally {
                this.ControlConnection.UnlockControlConnection();
            }

            this._position = offset;
            return offset;
        }

        /// <summary>
        /// Flushes the base stream
        /// </summary>
        public override void Flush() {
            if (this.BaseStream == null) {
                throw new IOException("The base stream is null. Has a socket connection been opened yet?");
            }

            this.BaseStream.Flush();
        }

        /// <summary>
        /// Closes the base stream and reads the response
        /// status of the transfer if necessary.
        /// </summary>
        public override void Close() {
            if (this._socket != null) {
                //this._socket = null;
                this.Socket = null;

                if (this.ControlConnection.Connected) {
                    try {
                        this.ControlConnection.LockControlConnection();

                        if (this.ControlConnection.ResponseStatus && !this.ControlConnection.ReadResponse()) {
                            throw new FtpCommandException(this.ControlConnection);
                        }
                    }
                    finally {
                        this.ControlConnection.UnlockControlConnection();
                    }
                }
            }

            this._length = 0;
            this._position = 0;
            this._started = false;
            this.SslPolicyErrors = Security.SslPolicyErrors.None;
        }

        /// <summary>
        /// Cleans up the resources this stream is using.
        /// </summary>
        public new void Dispose() {
            this.Close();
            base.Dispose();
        }

        /// <summary>
        /// Opens the stream
        /// </summary>
        protected void Open() {
            if (!this.Socket.Connected) {
                this.Open(this.ControlConnection.DataChannelType);
                this.TransferStarted = true;
            }
        }

        /// <summary>
        /// Sets up sockets and executes necessary commands to initalize
        /// a data transfer.
        /// </summary>
        /// <param name="type"></param>
        protected abstract void Open(FtpDataChannelType type);

        /// <summary>
        /// Executes a command on the control channel
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Execute(string command, params object[] args) {
            return this.Execute(string.Format(command, args));
        }

        /// <summary>
        /// Executes a command on the control channel
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public abstract bool Execute(string command);

        /// <summary>
        /// Stream for reading and writing to ftp data channels
        /// </summary>
        public FtpDataStream() { }
    }
}
