using System;
using System.IO;
using System.Security.Cryptography;

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
        /// Gets a value indicating if this object represents a
        /// valid hash response from the server.
        /// </summary>
        public bool IsValid {
            get { return m_algorithm != FtpHashAlgorithm.NONE && !string.IsNullOrEmpty(m_value); }
        }

        /// <summary>
        /// Computes the hash for the specified file and compares
        /// it to the value in this object. CRC hashes are not supported 
        /// because there is no built-in support in the .net framework and
        /// a CRC implementation exceeds the scope of this project. If you
        /// attempt to Verify() a CRC hash a NotImplemented() exception will
        /// be thrown.
        /// </summary>
        /// <param name="file">The file to compute the hash for</param>
        /// <returns>True if the computed hash matches what's stored in this object.</returns>
        public bool Verify(string file) {
            using (FileStream istream = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                return Verify(istream);
            }
        }

        /// <summary>
        /// Computes the hash for the specified stream and compares
        /// it to the value in this object. CRC hashes are not supported 
        /// because there is no built-in support in the .net framework and
        /// a CRC implementation exceeds the scope of this project. If you
        /// attempt to Verify() a CRC hash a NotImplemented() exception will
        /// be thrown.
        /// </summary>
        /// <param name="istream">The stream to compute the hash for</param>
        /// <returns>True if the computed hash matches what's stored in this object.</returns>
        public bool Verify(Stream istream) {
            if (IsValid) {
                HashAlgorithm hashAlg = null;

                switch (m_algorithm) {
                    case FtpHashAlgorithm.SHA1:
                        hashAlg = new SHA1CryptoServiceProvider();
                        break;
#if !NET2
                    case FtpHashAlgorithm.SHA256:
                        hashAlg = new SHA256CryptoServiceProvider();
                        break;
                    case FtpHashAlgorithm.SHA512:
                        hashAlg = new SHA512CryptoServiceProvider();
                        break;
#endif
                    case FtpHashAlgorithm.MD5:
                        hashAlg = new MD5CryptoServiceProvider();
                        break;
                    case FtpHashAlgorithm.CRC:
                        throw new NotImplementedException("There is no built in support for computing CRC hashes.");
                    default:
                        throw new NotImplementedException("Unknown hash algorithm: " + m_algorithm.ToString());
                }

                try {
                    byte[] data = null;
                    string hash = "";

                    data = hashAlg.ComputeHash(istream);
                    if (data != null) {
                        foreach (byte b in data) {
                            hash += b.ToString("x2");
                        }

                        return (hash.ToUpper() == m_value.ToUpper());
                    }
                }
                finally {
#if !NET2 // .NET 2.0 doesn't provide access to Dispose() for HashAlgorithm
                    if (hashAlg != null)
                        hashAlg.Dispose();
#endif
                }
            }

            return false;
        }

        /// <summary>
        /// Creates an empty instance.
        /// </summary>
        internal FtpHash() {
        }
    }
}
