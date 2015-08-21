using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.FtpClient {
    /// <summary>
    /// Event fired if a bad SSL certificate is encountered. This even is used internally; if you
    /// don't have a specific reason for using it you are probably looking for FtpSslValidation.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="e"></param>
    public delegate void FtpSocketStreamSslValidation(FtpSocketStream stream, FtpSslValidationEventArgs e);

    /// <summary>
    /// Event args for the FtpSslValidationError delegate
    /// </summary>
    public class FtpSslValidationEventArgs : EventArgs {
        X509Certificate m_certificate = null;
        /// <summary>
        /// The certificate to be validated
        /// </summary>
        public X509Certificate Certificate {
            get {
                return m_certificate;
            }
            set {
                m_certificate = value;
            }
        }

        X509Chain m_chain = null;
        /// <summary>
        /// The certificate chain
        /// </summary>
        public X509Chain Chain {
            get {
                return m_chain;
            }
            set {
                m_chain = value;
            }
        }

        SslPolicyErrors m_policyErrors = SslPolicyErrors.None;
        /// <summary>
        /// Validation errors, if any.
        /// </summary>
        public SslPolicyErrors PolicyErrors {
            get {
                return m_policyErrors;
            }
            set {
                m_policyErrors = value;
            }
        }

        bool m_accept = false;
        /// <summary>
        /// Gets or sets a value indicating if this certificate should be accepted. The default
        /// value is false. If the certificate is not accepted, an AuthenticationException will
        /// be thrown.
        /// </summary>
        public bool Accept {
            get {
                return m_accept;
            }
            set {
                m_accept = value;
            }
        }
    }

    /// <summary>
    /// Stream class used for talking. Used by FtpClient, extended by FtpDataStream
    /// </summary>
    public class FtpSocketStream : Stream, IDisposable {
        /// <summary>
        /// Used for tacking read/write activity on the socket
        /// to determine if Poll() should be used to test for
        /// socket conenctivity. The socket in this class will
        /// not know it has been disconnected if the remote host
        /// closes the connection first. Using Poll() avoids 
        /// the exception that would be thrown when trying to
        /// read or write to the disconnected socket.
        /// </summary>
        private DateTime m_lastActivity = DateTime.Now;

        private Socket m_socket = null;
        /// <summary>
        /// The socket used for talking
        /// </summary>
        protected Socket Socket {
            get {
                return m_socket;
            }
            private set {
                m_socket = value;
            }
        }

        int m_socketPollInterval = 15000;
        /// <summary>
        /// Gets or sets the length of time in miliseconds
        /// that must pass since the last socket activity
        /// before calling Poll() on the socket to test for
        /// connectivity. Setting this interval too low will
        /// have a negative impact on perfomance. Setting this
        /// interval to 0 disables Poll()'ing all together.
        /// The default value is 15 seconds.
        /// </summary>
        public int SocketPollInterval {
            get { return m_socketPollInterval; }
            set { m_socketPollInterval = value; }
        }

        /// <summary>
        /// Gets the number of available bytes on the socket, 0 if the
        /// socket has not been initalized. This property is used internally
        /// by FtpClient in an effort to detect disconnections and gracefully
        /// reconnect the control connection.
        /// </summary>
        internal int SocketDataAvailable {
            get {
                if (m_socket != null)
                    return m_socket.Available;
                return 0;
            }
        }

        /// <summary>
        /// Gets a value indicating if this socket stream is connected
        /// </summary>
        public bool IsConnected {
            get {
                try {
                    if (m_socket == null)
                        return false;

                    if (!m_socket.Connected) {
                        Close();
                        return false;
                    }

                    if (!CanRead || !CanWrite) {
                        Close();
                        return false;
                    }

                    if (m_socketPollInterval > 0 && DateTime.Now.Subtract(m_lastActivity).TotalMilliseconds > m_socketPollInterval) {
                        FtpTrace.WriteLine("Testing connectivity using Socket.Poll()...");
                        if (m_socket.Poll(500000, SelectMode.SelectRead) && m_socket.Available == 0) {
                            Close();
                            return false;
                        }
                    }
                }
                catch (SocketException sockex) {
                    Close();
                    FtpTrace.WriteLine("FtpSocketStream.IsConnected: Caught and discarded SocketException while testing for connectivity: {0}", sockex.ToString());
                    return false;
                }
                catch (IOException ioex) {
                    Close();
                    FtpTrace.WriteLine("FtpSocketStream.IsConnected: Caught and discarded IOException while testing for connectivity: {0}", ioex.ToString());
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating if encryption is being used
        /// </summary>
        public bool IsEncrypted {
            get {
                return m_sslStream != null;
            }
        }

        NetworkStream m_netStream = null;
        /// <summary>
        /// The non-encrypted stream
        /// </summary>
        private NetworkStream NetworkStream {
            get {
                return m_netStream;
            }
            set {
                m_netStream = value;
            }
        }

        SslStream m_sslStream = null;
        /// <summary>
        /// The encrypted stream
        /// </summary>
        private SslStream SslStream {
            get {
                return m_sslStream;
            }
            set {
                m_sslStream = value;
            }
        }

        /// <summary>
        /// Underlying stream, could be a NetworkStream or SslStream
        /// </summary>
        protected Stream BaseStream {
            get {
                if (m_sslStream != null)
                    return m_sslStream;
                else if (m_netStream != null)
                    return m_netStream;

                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating if this stream can be read
        /// </summary>
        public override bool CanRead {
            get {
                if (m_netStream != null)
                    return m_netStream.CanRead;
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating if this stream if seekable
        /// </summary>
        public override bool CanSeek {
            get {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating if this stream can be written to
        /// </summary>
        public override bool CanWrite {
            get {
                if (m_netStream != null)
                    return m_netStream.CanWrite;

                return false;
            }
        }

        /// <summary>
        /// Gets the length of the stream
        /// </summary>
        public override long Length {
            get {
                return 0;
            }
        }

        /// <summary>
        /// Gets the current position of the stream. Trying to
        /// set this property throws an InvalidOperationException()
        /// </summary>
        public override long Position {
            get {
                if (BaseStream != null)
                    return BaseStream.Position;
                return 0;
            }
            set {
                throw new InvalidOperationException();
            }
        }

        event FtpSocketStreamSslValidation m_sslvalidate = null;
        /// <summary>
        /// Event is fired when a SSL certificate needs to be validated
        /// </summary>
        public event FtpSocketStreamSslValidation ValidateCertificate {
            add {
                m_sslvalidate += value;
            }
            remove {
                m_sslvalidate -= value;
            }
        }

        int m_readTimeout = Timeout.Infinite;
        /// <summary>
        /// Gets or sets the amount of time to wait for a read operation to complete. Default
        /// value is Timeout.Infinite.
        /// </summary>
        public override int ReadTimeout {
            get {
                return m_readTimeout;
            }
            set {
                m_readTimeout = value;
            }
        }

        int m_connectTimeout = 30000;
        /// <summary>
        /// Gets or sets the length of time miliseconds to wait
        /// for a connection succeed before giving up. The default
        /// is 30000 (30 seconds).
        /// </summary>
        public int ConnectTimeout {
            get {
                return m_connectTimeout;
            }
            set {
                m_connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets the local end point of the socket
        /// </summary>
        public IPEndPoint LocalEndPoint {
            get {
                if (m_socket == null)
                    return null;
                return (IPEndPoint)m_socket.LocalEndPoint;
            }
        }

        /// <summary>
        /// Gets the remote end point of the socket
        /// </summary>
        public IPEndPoint RemoteEndPoint {
            get {
                if (m_socket == null)
                    return null;
                return (IPEndPoint)m_socket.RemoteEndPoint;
            }
        }

        /// <summary>
        /// Fires the SSL certificate validation event
        /// </summary>
        /// <param name="certificate">Certificate being validated</param>
        /// <param name="chain">Certificate chain</param>
        /// <param name="errors">Policy errors if any</param>
        /// <returns>True if it was accepted, false otherwise</returns>
        protected bool OnValidateCertificate(X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
            FtpSocketStreamSslValidation evt = m_sslvalidate;

            if (evt != null) {
                FtpSslValidationEventArgs e = new FtpSslValidationEventArgs() {
                    Certificate = certificate,
                    Chain = chain,
                    PolicyErrors = errors,
                    Accept = (errors == SslPolicyErrors.None)
                };

                evt(this, e);
                return e.Accept;
            }

            // if the event was not handled then only accept
            // the certificate if there were no validation errors
            return (errors == SslPolicyErrors.None);
        }

        /// <summary>
        /// Throws an InvalidOperationException
        /// </summary>
        /// <param name="offset">Ignored</param>
        /// <param name="origin">Ignored</param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Throws an InvalidOperationException
        /// </summary>
        /// <param name="value">Ignored</param>
        public override void SetLength(long value) {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Flushes the stream
        /// </summary>
        public override void Flush() {
            if (!IsConnected)
                throw new InvalidOperationException("The FtpSocketStream object is not connected.");

            if (BaseStream == null)
                throw new InvalidOperationException("The base stream of the FtpSocketStream object is null.");

            BaseStream.Flush();
        }

        /// <summary>
        /// Bypass the stream and read directly off the socket.
        /// </summary>
        /// <param name="buffer">The buffer to read into</param>
        /// <returns>The number of bytes read</returns>
        internal int RawSocketRead(byte[] buffer) {
            int read = 0;

            if (m_socket != null && m_socket.Connected) {
                read = m_socket.Receive(buffer, buffer.Length, 0);
            }

            return read;
        }

        /// <summary>
        /// Reads data from the stream
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Where in the buffer to start</param>
        /// <param name="count">Number of bytes to be read</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count) {
            IAsyncResult ar = null;

            if (BaseStream == null)
                return 0;

            m_lastActivity = DateTime.Now;
            ar = BaseStream.BeginRead(buffer, offset, count, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(m_readTimeout, true)) {
                Close();
                throw new TimeoutException("Timed out trying to read data from the socket stream!");
            }

            return BaseStream.EndRead(ar);
        }

        /// <summary>
        /// Reads a line from the socket
        /// </summary>
        /// <returns>A line from the stream, null if there is nothing to read</returns>
        public string ReadLine(System.Text.Encoding encoding) {
            List<byte> data = new List<byte>();
            byte[] buf = new byte[1];
            string line = null;

            while (Read(buf, 0, buf.Length) > 0) {
                data.Add(buf[0]);
                if ((char)buf[0] == '\n') {
                    line = encoding.GetString(data.ToArray()).Trim('\r', '\n');
                    break;
                }
            }

            return line;
        }

        /// <summary>
        /// Writes data to the stream
        /// </summary>
        /// <param name="buffer">Buffer to write to stream</param>
        /// <param name="offset">Where in the buffer to start</param>
        /// <param name="count">Number of bytes to be read</param>
        public override void Write(byte[] buffer, int offset, int count) {
            if (BaseStream == null)
                return;

            BaseStream.Write(buffer, offset, count);
            m_lastActivity = DateTime.Now;
        }

        /// <summary>
        /// Writes a line to the stream using the specified encoding
        /// </summary>
        /// <param name="encoding">Encoding used for writing the line</param>
        /// <param name="buf">The data to write</param>
        public void WriteLine(System.Text.Encoding encoding, string buf) {
            byte[] data;
            data = encoding.GetBytes(string.Format("{0}\r\n", buf));
            Write(data, 0, data.Length);
        }

        /// <summary>
        /// Disposes the stream
        /// </summary>
        public new void Dispose() {
            FtpTrace.WriteLine("Disposing FtpSocketStream...");
            Close();
        }

        /// <summary>
        /// Disconnects from server
        /// </summary>
        public override void Close() {
            if (m_socket != null) {
                try {
                    if (m_socket.Connected) {
                        ////
                        // Calling Shutdown() with mono causes an
                        // exception if the remote host closed first
                        //m_socket.Shutdown(SocketShutdown.Both);
                        m_socket.Close();
                    }

#if !NET2
                    m_socket.Dispose();
#endif
                }
                catch (SocketException ex) {
                    FtpTrace.WriteLine("Caught and discarded a SocketException while cleaning up the Socket: {0}", ex.ToString());
                }
                finally {
                    m_socket = null;
                }
            }

            if (m_netStream != null) {
                try {
                    m_netStream.Dispose();
                }
                catch (IOException ex) {
                    FtpTrace.WriteLine("Caught and discarded an IOException while cleaning up the NetworkStream: {0}", ex.ToString());
                }
                finally {
                    m_netStream = null;
                }
            }

            if (m_sslStream != null) {
                try {
                    m_sslStream.Dispose();
                }
                catch (IOException ex) {
                    FtpTrace.WriteLine("Caught and discarded an IOException while cleaning up the SslStream: {0}", ex.ToString());
                }
                finally {
                    m_sslStream = null;
                }
            }
        }

        /// <summary>
        /// Sets socket options on the underlying socket
        /// </summary>
        /// <param name="level">SocketOptionLevel</param>
        /// <param name="name">SocketOptionName</param>
        /// <param name="value">SocketOptionValue</param>
        public void SetSocketOption(SocketOptionLevel level, SocketOptionName name, bool value) {
            if (m_socket == null)
                throw new InvalidOperationException("The underlying socket is null. Have you established a connection?");
            m_socket.SetSocketOption(level, name, value);
        }

        /// <summary>
        /// Connect to the specified host
        /// </summary>
        /// <param name="host">The host to connect to</param>
        /// <param name="port">The port to connect to</param>
        /// <param name="ipVersions">Internet Protocol versions to support durring the connection phase</param>
        public void Connect(string host, int port, FtpIpVersion ipVersions) {
            IAsyncResult ar = null;
            IPAddress[] addresses = Dns.GetHostAddresses(host);

            if (ipVersions == 0)
                throw new ArgumentException("The ipVersions parameter must contain at least 1 flag.");

            for (int i = 0; i < addresses.Length; i++) {
#if DEBUG
                FtpTrace.WriteLine("{0}: {1}", addresses[i].AddressFamily.ToString(), addresses[i].ToString());
#endif
                // we don't need to do this check unless
                // a particular version of IP has been
                // omitted so we won't.
                if (ipVersions != FtpIpVersion.ANY) {
                    switch (addresses[i].AddressFamily) {
                        case AddressFamily.InterNetwork:
                            if ((ipVersions & FtpIpVersion.IPv4) != FtpIpVersion.IPv4) {
#if DEBUG
                                FtpTrace.WriteLine("SKIPPED!");
#endif
                                continue;
                            }
                            break;
                        case AddressFamily.InterNetworkV6:
                            if ((ipVersions & FtpIpVersion.IPv6) != FtpIpVersion.IPv6) {
#if DEBUG
                                FtpTrace.WriteLine("SKIPPED!");
#endif
                                continue;
                            }
                            break;
                    }
                }

                m_socket = new Socket(addresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ar = m_socket.BeginConnect(addresses[i], port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(m_connectTimeout, true)) {
                    Close();

                    // check to see if we're out of addresses
                    // and if we are throw a TimeoutException
                    if (i + 1 == addresses.Length)
                        throw new TimeoutException("Timed out trying to connect!");
                }
                else {
                    m_socket.EndConnect(ar);
                    // we got a connection, break out
                    // of the loop.
                    break;
                }
            }

            // make sure that we actually connected to
            // one of the addresses returned from GetHostAddresses()
            if (m_socket == null || !m_socket.Connected) {
                Close();
                throw new IOException("Failed to connect to host.");
            }

            m_netStream = new NetworkStream(m_socket);
            m_lastActivity = DateTime.Now;
        }

        /// <summary>
        /// Activates SSL on this stream using default protocols. Fires the ValidateCertificate event. 
        /// If this event is not handled and there are SslPolicyErrors present, the certificate will 
        /// not be accepted.
        /// </summary>
        /// <param name="targethost">The host to authenticate the certiciate against</param>
        public void ActivateEncryption(string targethost) {
            ActivateEncryption(targethost, null, SslProtocols.Default);
        }

        /// <summary>
        /// Activates SSL on this stream using default protocols. Fires the ValidateCertificate event.
        /// If this event is not handled and there are SslPolicyErrors present, the certificate will 
        /// not be accepted.
        /// </summary>
        /// <param name="targethost">The host to authenticate the certiciate against</param>
        /// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
        public void ActivateEncryption(string targethost, X509CertificateCollection clientCerts) {
            ActivateEncryption(targethost, clientCerts, SslProtocols.Default);
        }

        /// <summary>
        /// Activates SSL on this stream using the specified protocols. Fires the ValidateCertificate event.
        /// If this event is not handled and there are SslPolicyErrors present, the certificate will 
        /// not be accepted.
        /// </summary>
        /// <param name="targethost">The host to authenticate the certiciate against</param>
        /// <param name="clientCerts">A collection of client certificates to use when authenticating the SSL stream</param>
        /// <param name="sslProtocols">A bitwise parameter for supported encryption protocols.</param>
        public void ActivateEncryption(string targethost, X509CertificateCollection clientCerts, SslProtocols sslProtocols) {
            if (!IsConnected)
                throw new InvalidOperationException("The FtpSocketStream object is not connected.");

            if (m_netStream == null)
                throw new InvalidOperationException("The base network stream is null.");

            if (m_sslStream != null)
                throw new InvalidOperationException("SSL Encryption has already been enabled on this stream.");

            try {
                DateTime auth_start;
                TimeSpan auth_time_total;

                m_sslStream = new SslStream(NetworkStream, true, new RemoteCertificateValidationCallback(
                    delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
                        return OnValidateCertificate(certificate, chain, sslPolicyErrors);
                    }));

                auth_start = DateTime.Now;
                m_sslStream.AuthenticateAsClient(targethost, clientCerts, sslProtocols, true);

                auth_time_total = DateTime.Now.Subtract(auth_start);
                FtpTrace.WriteLine("Time to activate encryption: {0}h {1}m {2}s, Total Seconds: {3}.",
                    auth_time_total.Hours,
                    auth_time_total.Minutes,
                    auth_time_total.Seconds,
                    auth_time_total.TotalSeconds);
            }
            catch (AuthenticationException ex) {
                // authentication failed and in addition it left our 
                // ssl stream in an unsuable state so cleanup needs
                // to be done and the exception can be re-thrown for
                // handling down the chain.
                Close();
                throw ex;
            }
        }

        /// <summary>
        /// Instructs this stream to listen for connections on the specified address and port
        /// </summary>
        /// <param name="address">The address to listen on</param>
        /// <param name="port">The port to listen on</param>
        public void Listen(IPAddress address, int port) {
            if (!IsConnected) {
                if (m_socket == null)
                    m_socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                m_socket.Bind(new IPEndPoint(address, port));
                m_socket.Listen(1);
            }
        }

        /// <summary>
        /// Accepts a connection from a listening socket
        /// </summary>
        public void Accept() {
            if (m_socket != null)
                m_socket = m_socket.Accept();
        }

        /// <summary>
        /// Asynchronously accepts a connection from a listening socket
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public IAsyncResult BeginAccept(AsyncCallback callback, object state) {
            if (m_socket != null)
                return m_socket.BeginAccept(callback, state);
            return null;
        }

        /// <summary>
        /// Completes a BeginAccept() operation
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginAccept</param>
        public void EndAccept(IAsyncResult ar) {
            if (m_socket != null) {
                m_socket = m_socket.EndAccept(ar);
                m_netStream = new NetworkStream(m_socket);
            }
        }
    }
}
