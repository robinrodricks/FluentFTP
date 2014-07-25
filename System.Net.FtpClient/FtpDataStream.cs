using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace System.Net.FtpClient {
    /// <summary>
    /// Base class for data stream connections
    /// </summary>
    public class FtpDataStream : FtpSocketStream {
        FtpReply m_commandStatus;
        /// <summary>
        /// Gets the status of the command that was used to open
        /// this data channel
        /// </summary>
        public FtpReply CommandStatus {
            get {
                return m_commandStatus;
            }
            set {
                m_commandStatus = value;
            }
        }

        FtpClient m_control = null;
        /// <summary>
        /// Gets or sets the control connection for this data stream. Setting
        /// the control connection causes the object to be clonded and a new
        /// connection is made to the server to carry out the task. This ensures
        /// that multiple streams can be opened simultainously.
        /// </summary>
        public FtpClient ControlConnection {
            get {
                return m_control;
            }
            set {
                m_control = value;
            }
        }

        long m_length = 0;
        /// <summary>
        /// Gets or sets the length of the stream. Only valid for file transfers
        /// and only valid on servers that support the Size command.
        /// </summary>
        public override long Length {
            get {
                return m_length;
            }
        }

        long m_position = 0;
        /// <summary>
        /// Gets or sets the position of the stream
        /// </summary>
        public override long Position {
            get {
                return m_position;
            }
            set {
                throw new InvalidOperationException("You cannot modify the position of a FtpDataStream. This property is updated as data is read or written to the stream.");
            }
        }

        /// <summary>
        /// Reads data off the stream
        /// </summary>
        /// <param name="buffer">The buffer to read into</param>
        /// <param name="offset">Where to start in the buffer</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>The number of bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            int read = base.Read(buffer, offset, count);
            m_position += read;
            return read;
        }

        /// <summary>
        /// Writes data to the stream
        /// </summary>
        /// <param name="buffer">The buffer to write to the stream</param>
        /// <param name="offset">Where to start in the buffer</param>
        /// <param name="count">The number of bytes to write to the buffer</param>
        public override void Write(byte[] buffer, int offset, int count) {
            base.Write(buffer, offset, count);
            m_position += count;
        }

        /// <summary>
        /// Sets the length of this stream
        /// </summary>
        /// <param name="value">Value to apply to the Length property</param>
        public override void SetLength(long value) {
            m_length = value;
        }

        /// <summary>
        /// Sets the position of the stream. Inteneded to be used
        /// internally by FtpControlConnection.
        /// </summary>
        /// <param name="pos">The position</param>
        public void SetPosition(long pos) {
            m_position = pos;
        }

        /// <summary>
        /// Disconnects (if necessary) and releases associated resources
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (IsConnected)
                    Close();

                m_control = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Closes the connection and reads the server's reply
        /// </summary>
        public new FtpReply Close() {
            base.Close();

            try {
                if (ControlConnection != null)
                    return ControlConnection.CloseDataStream(this);
            }
            finally {
                m_commandStatus = new FtpReply();
                m_control = null;
            }

            return new FtpReply();
        }

        /// <summary>
        /// Creates a new data stream object
        /// </summary>
        /// <param name="conn">The control connection to be used for carrying out this operation</param>
        public FtpDataStream(FtpClient conn) {
            if (conn == null)
                throw new ArgumentException("The control connection cannot be null.");

            ControlConnection = conn;
            // always accept certficate no matter what because if code execution ever
            // gets here it means the certificate on the control connection object being
            // cloned was already accepted.
            ValidateCertificate += new FtpSocketStreamSslValidation(delegate(FtpSocketStream obj, FtpSslValidationEventArgs e) {
                e.Accept = true;
            });

            m_position = 0;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~FtpDataStream() {
            try {
                Dispose();
            }
            catch (Exception ex) {
                FtpTrace.WriteLine("[Finalizer] Caught and discarded an exception while disposing the FtpDataStream: {0}", ex.ToString());
            }
        }
    }
}
