using System;

namespace System.Net.FtpClient {
    /// <summary>
    /// Represents a computed hash of an object
    /// on the FTP server. See the following link
    /// for more information:
    /// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
    /// </summary>
    public class FtpHash {
        FtpHashAlgorithm m_algorithm = FtpHashAlgorithm.NONE;
        /// <summary>
        /// Gets the algorithm that was used to compute the hash
        /// </summary>
        public FtpHashAlgorithm Algorithm {
            get { return m_algorithm; }
            internal set { m_algorithm = value; }
        }

        string m_value = null;
        /// <summary>
        /// Gets the computed hash returned by the server
        /// </summary>
        public string Value {
            get { return m_value; }
            internal set { m_value = value; }
        }

        /// <summary>
        /// Creates an empty instance.
        /// </summary>
        internal FtpHash() {
        }
    }
}
