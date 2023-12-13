using FluentFTP.Helpers;
using FluentFTP.Helpers.Hashing;
using FluentFTP.Streams;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FluentFTP {
	/// <summary>
	/// Represents a computed hash of an object
	/// on the FTP server. See the following link
	/// for more information:
	/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
	/// </summary>
	public class FtpHash {
		private FtpHashAlgorithm m_algorithm = FtpHashAlgorithm.NONE;

		/// <summary>
		/// Gets the algorithm that was used to compute the hash
		/// </summary>
		public FtpHashAlgorithm Algorithm {
			get => m_algorithm;
			internal set => m_algorithm = value;
		}

		private string m_value = null;

		/// <summary>
		/// Gets the computed hash returned by the server
		/// </summary>
		public string Value {
			get => m_value;
			internal set => m_value = value;
		}

		/// <summary>
		/// Gets a value indicating if this object represents a
		/// valid hash response from the server.
		/// </summary>
		public bool IsValid => m_algorithm != FtpHashAlgorithm.NONE && !string.IsNullOrEmpty(m_value);

		/// <summary>
		/// Computes the hash for the specified file and compares
		/// it to the value in this object. CRC hashes are not supported 
		/// because there is no built-in support in the .net framework and
		/// a CRC implementation exceeds the scope of this project. If you
		/// attempt to call this on a CRC hash a <see cref="NotImplementedException"/> will
		/// be thrown.
		/// </summary>
		/// <param name="file">The file to compute the hash for</param>
		/// <returns>True if the computed hash matches what's stored in this object.</returns>
		/// <exception cref="NotImplementedException">Thrown if called on a CRC Hash</exception>
		public bool Verify(string file) {

			// read the file using a FileStream or by reading it entirely into memory if it fits within 1 MB
			using (var istream = FtpFileStream.GetFileReadStream(null, file, false, 1024 * 1024)) {

				// verify the file data against the hash reported by the FTP server
				return Verify(istream);
			}
		}

		/// <summary>
		/// Computes the hash for the specified stream and compares
		/// it to the value in this object. CRC hashes are not supported 
		/// because there is no built-in support in the .net framework and
		/// a CRC implementation exceeds the scope of this project. If you
		/// attempt to call this on a CRC hash a <see cref="NotImplementedException"/> will
		/// be thrown.
		/// </summary>
		/// <param name="istream">The stream to compute the hash for</param>
		/// <returns>True if the computed hash matches what's stored in this object.</returns>
		/// <exception cref="NotImplementedException">Thrown if called on a CRC Hash</exception>
		public bool Verify(Stream istream) {
			if (IsValid) {
				HashAlgorithm hashAlg = null;

				switch (m_algorithm) {
					case FtpHashAlgorithm.SHA1:
#if NETSTANDARD || NET5_0_OR_GREATER
						hashAlg = SHA1.Create();
#else
						hashAlg = new SHA1CryptoServiceProvider();
#endif
						break;

					case FtpHashAlgorithm.SHA256:
#if NETSTANDARD || NET5_0_OR_GREATER
						hashAlg = SHA256.Create();
#else
						hashAlg = new SHA256CryptoServiceProvider();
#endif
						break;

					case FtpHashAlgorithm.SHA512:
#if NETSTANDARD || NET5_0_OR_GREATER
						hashAlg = SHA512.Create();
#else
						hashAlg = new SHA512CryptoServiceProvider();
#endif
						break;

					case FtpHashAlgorithm.MD5:
#if NETSTANDARD || NET5_0_OR_GREATER
						hashAlg = MD5.Create();
#else
						hashAlg = new MD5CryptoServiceProvider();
#endif
						break;

					case FtpHashAlgorithm.CRC:

						hashAlg = new CRC32();

						break;

					default:
						throw new NotImplementedException("Unknown hash algorithm: " + m_algorithm.ToString());
				}

				try {
					byte[] data = null;
					var hash = new StringBuilder();

					data = hashAlg.ComputeHash(istream);
					if (data != null) {

						// convert hash to hex string
						foreach (var b in data) {
							hash.Append(b.ToString("x2"));
						}
						var hashStr = hash.ToString();

						// check if hash exactly matches
						if (hashStr.Equals(m_value, StringComparison.OrdinalIgnoreCase)) {
							return true;
						}
						// check if hash matches without the "0" prefix that .NET CRC sometimes generates
						// to fix #820: Validation of short CRC checksum fails due to mismatch with hex format
						if (Strings.RemovePrefix(hashStr, "0").Equals(Strings.RemovePrefix(m_value, "0"), StringComparison.OrdinalIgnoreCase)) {
							return true;
						}
						return false;
					}
				}
				finally {
					hashAlg?.Dispose();
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