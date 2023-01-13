using FluentFTP.Exceptions;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Does the server support checksums?
		/// </summary>
		/// <returns></returns>
		protected bool SupportsChecksum() {
			return HasFeature(FtpCapability.HASH) || HasFeature(FtpCapability.MD5) ||
					HasFeature(FtpCapability.XMD5) || HasFeature(FtpCapability.XCRC) ||
					HasFeature(FtpCapability.XSHA1) || HasFeature(FtpCapability.XSHA256) ||
					HasFeature(FtpCapability.XSHA512);
		}

		/// <summary>
		/// Is the checksum algorithm valid?
		/// </summary>
		/// <param name="algorithm"></param>
		/// <exception cref="FtpHashUnsupportedException"></exception>
		protected void ValidateChecksumAlgorithm(FtpHashAlgorithm algorithm) {

			// if NO hashing algos or commands supported, throw here
			if (!HasFeature(FtpCapability.HASH) &&
				!HasFeature(FtpCapability.MD5) &&
				!HasFeature(FtpCapability.XMD5) &&
				!HasFeature(FtpCapability.MMD5) &&
				!HasFeature(FtpCapability.XSHA1) &&
				!HasFeature(FtpCapability.XSHA256) &&
				!HasFeature(FtpCapability.XSHA512) &&
				!HasFeature(FtpCapability.XCRC)) {
				throw new FtpHashUnsupportedException();
			}

			// only if the user has specified a certain hash algorithm
			var useFirst = (algorithm == FtpHashAlgorithm.NONE);
			if (!useFirst) {

				// first check if the HASH command supports the required algo
				if (HasFeature(FtpCapability.HASH) && HashAlgorithms.HasFlag(algorithm)) {

					// we are good

				}
				else {

					// second check if the special FTP command is supported based on the algo
					if (algorithm == FtpHashAlgorithm.MD5 && !HasFeature(FtpCapability.MD5) &&
						!HasFeature(FtpCapability.XMD5) && !HasFeature(FtpCapability.MMD5)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.MD5, "MD5, XMD5, MMD5");
					}
					if (algorithm == FtpHashAlgorithm.SHA1 && !HasFeature(FtpCapability.XSHA1)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.SHA1, "XSHA1");
					}
					if (algorithm == FtpHashAlgorithm.SHA256 && !HasFeature(FtpCapability.XSHA256)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.SHA256, "XSHA256");
					}
					if (algorithm == FtpHashAlgorithm.SHA512 && !HasFeature(FtpCapability.XSHA512)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.SHA512, "XSHA512");
					}
					if (algorithm == FtpHashAlgorithm.CRC && !HasFeature(FtpCapability.XCRC)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.CRC, "XCRC");
					}

					// we are good
				}
			}
		}

		/// <summary>
		/// Cleanup the hash result
		/// </summary>
		protected static string CleanHashResult(string path, string response) {
			response = response.RemovePrefix(path);
			response = response.RemovePrefix($@"""{path}""");
			return response;
		}

		/// <summary>
		/// Get the first checksum algorithm mutually supported by both servers.
		/// </summary>
		protected FtpHashAlgorithm GetFirstMutualChecksum(BaseFtpClient destination) {

			// special handling for HASH command which is a meta-command supporting all hash types
			if (HasFeature(FtpCapability.HASH) && destination.HasFeature(FtpCapability.HASH)) {
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
					return FtpHashAlgorithm.MD5;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA1) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA1)) {
					return FtpHashAlgorithm.SHA1;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA256) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA256)) {
					return FtpHashAlgorithm.SHA256;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA512) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA512)) {
					return FtpHashAlgorithm.SHA512;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.CRC) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.CRC)) {
					return FtpHashAlgorithm.CRC;
				}
			}

			// handling for non-standard specific hashing commands
			if (HasFeature(FtpCapability.MD5) && destination.HasFeature(FtpCapability.MD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.XMD5) && destination.HasFeature(FtpCapability.XMD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.MMD5) && destination.HasFeature(FtpCapability.MMD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.XSHA1) && destination.HasFeature(FtpCapability.XSHA1)) {
				return FtpHashAlgorithm.SHA1;
			}
			if (HasFeature(FtpCapability.XSHA256) && destination.HasFeature(FtpCapability.XSHA256)) {
				return FtpHashAlgorithm.SHA256;
			}
			if (HasFeature(FtpCapability.XSHA512) && destination.HasFeature(FtpCapability.XSHA512)) {
				return FtpHashAlgorithm.SHA512;
			}
			if (HasFeature(FtpCapability.XCRC) && destination.HasFeature(FtpCapability.XCRC)) {
				return FtpHashAlgorithm.CRC;
			}
			return FtpHashAlgorithm.NONE;
		}

	}
}