using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.FtpClient {
    /// <summary>
    /// 
    /// </summary>
    public abstract class FtpDataStream : Stream, IDisposable {
        FtpCommandChannel _channel = null;
        /// <summary>
        /// Command channel this data stream is associated with
        /// </summary>
        public FtpCommandChannel CommandChannel {
            get { return _channel; }
            protected set { _channel = value; }
        }

        FtpDataMode _mode = FtpDataMode.Stream;
        /// <summary>
        /// Gets the data mode being used to transfer the file: stream / block
        /// </summary>
        public FtpDataMode DataMode {
            get { return _mode; }
            protected set { _mode = value; }
        }

        bool _started = false;
        /// <summary>
        /// Gets a value indicating if the transfer has started.
        /// </summary>
        public bool TransferStarted {
            get { return _started; }
            private set { _started = value; }
        }

        Socket _socket = null;
        /// <summary>
        /// Socket used for communication
        /// </summary>
        protected Socket Socket {
            get {
                if (this._socket == null) {
                    this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }

                return this._socket;
            }

            set {
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

                    this._socket.Dispose();
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
                if (this.CommandChannel.SslEnabled && this.CommandChannel.DataChannelEncryption) {
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

        long _length = 0;
        /// <summary>
        /// 
        /// </summary>
        public override long Length {
            get { return _length; }
        }

        long _position = 0;
        /// <summary>
        /// 
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
        /// 
        /// </summary>
        public override bool CanSeek {
            get {
                return this.TransferStarted ? false : true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool CanRead {
            get { return this.BaseStream != null ? this.BaseStream.CanRead : false; }
        }

        /// <summary>
        /// 
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
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        void Authenticate() {
            if (this.Socket.Connected && !this.SslEnabled) {
                //this.SecurteStream.AuthenticateAsClient(((IPEndPoint)this.RemoteEndPoint).Address.ToString());
                this.SecureStream.AuthenticateAsClient(this.CommandChannel.Server);
            }
        }

        /// <summary>
        /// 
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

            this.BaseStream.Write(buffer, offset, count);
            this._position += count;
        }

        protected FtpBlockHeader BlockHeader = null;

        private void GetBlockHeader() {
            byte[] buf = new byte[3];
            int read = 0;

            do {
                read += this.BaseStream.Read(buf, read, buf.Length);
            } while(read < buf.Length && read > 0) ;

            if (read == 0) {
                // something aint right, we should have at least got a header.
                // check the command channel for an
                // error response from the server.
                if (!this.CommandChannel.ReadResponse()) {
                    throw new FtpException(this.CommandChannel.ResponseMessage);
                }
            }

            this.BlockHeader = new FtpBlockHeader(buf);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count) {
            int read = 0;

            if (!this.CanRead) {
                throw new IOException("This stream is not readable!");
            }

            if (this.BaseStream == null) {
                throw new IOException("The base stream is null. Has a socket connection been opened yet?");
            }

            if (this.DataMode == FtpDataMode.Block) {
                if (this.BlockHeader == null || (this.BlockHeader.IsBlockFinished && !this.BlockHeader.IsEndOfFile)) {
                    this.GetBlockHeader();
                }
                
                if (this.BlockHeader.IsBlockFinished && this.BlockHeader.IsEndOfFile) {
                    // we're done! we got the EOF marker and we're at the end 
                    // of the file
                    if (!this.CommandChannel.ReadResponse()) {
                        throw new FtpException(this.CommandChannel.ResponseMessage);
                    }

                    this.BlockHeader = null;
                    return 0;
                }

                if (count > (this.BlockHeader.Length - this.BlockHeader.TotalRead)) {
                    count = this.BlockHeader.Length - this.BlockHeader.TotalRead;
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine(this.BlockHeader.ToString());
#endif
            }

            read = this.BaseStream.Read(buffer, offset, count);
            this._position += read;

            if (this.DataMode == FtpDataMode.Block) {
                this.BlockHeader.TotalRead += read;
            }

            return read;
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) {
            if (!this.CommandChannel.HasCapability(FtpCapability.REST)) {
                throw new FtpException("The FTP server does not support stream seeking.");
            }

            if (this.TransferStarted) {
                throw new IOException("FTP stream seeking cannot be done after the transfer has started.");
            }

            if (origin != SeekOrigin.Begin) {
                throw new IOException("The only seek origin that is supported is SeekOrigin.Begin");
            }

            if (!this.CommandChannel.Execute("REST {0}", offset)) {
                throw new FtpException(this.CommandChannel.ResponseMessage);
            }

            this._position = offset;
            return offset;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Flush() {
            if (this.BaseStream == null) {
                throw new IOException("The base stream is null. Has a socket connection been opened yet?");
            }

            this.BaseStream.Flush();
        }

        public override void Close() {
            if (this._socket != null) {
                this.Socket = null;

                if (this.DataMode == FtpDataMode.Stream && this.CommandChannel.Connected && !this.CommandChannel.ReadResponse()) {
                    throw new FtpException(this.CommandChannel.ResponseMessage);
                }
            }

            this.SslPolicyErrors = Security.SslPolicyErrors.None;
        }

        public new void Dispose() {
            this.Close();
            base.Dispose();
        }

        protected void Open() {
            if (!this.Socket.Connected) {
                this.Open(this.CommandChannel.DataChannelType);
                this.TransferStarted = true;
            }
        }

        protected abstract void Open(FtpDataChannelType type);

        public bool Execute(string command, params object[] args) {
            return this.Execute(string.Format(command, args));
        }

        public abstract bool Execute(string command);

        /// <summary>
        /// 
        /// </summary>
        public FtpDataStream() { }
    }
}
