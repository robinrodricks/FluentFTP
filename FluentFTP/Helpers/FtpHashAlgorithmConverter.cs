using System;
using System.Collections.Generic;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Helper class to convert FtpHashAlgorithm
	/// </summary>
	public static class FtpHashAlgorithmConverter {
		private static readonly Dictionary<string, FtpHashAlgorithm> ftpHashAlgorithmsByStringKeys = new Dictionary<string, FtpHashAlgorithm> {
			{ "SHA-1", FtpHashAlgorithm.SHA1 },
			{ "SHA-256", FtpHashAlgorithm.SHA256 },
			{ "SHA-512", FtpHashAlgorithm.SHA512 },
			{ "MD5", FtpHashAlgorithm.MD5 },
		};

		private static readonly Dictionary<FtpHashAlgorithm, string> ftpHashAlgorithmsByFtpHashAlgorithmKeys = new Dictionary<FtpHashAlgorithm, string> {
			{ FtpHashAlgorithm.SHA1, "SHA-1" },
			{ FtpHashAlgorithm.SHA256, "SHA-256" },
			{ FtpHashAlgorithm.SHA512, "SHA-512" },
			{ FtpHashAlgorithm.MD5, "MD5" },
		};

		/// <summary>
		/// Get FtpHashAlgorithm from it's string representation
		/// </summary>
		/// <param name="ftpHashAlgorithm">Name of the hash algorithm</param>
		/// <returns>The FtpHashAlgorithm</returns>
		public static FtpHashAlgorithm FromString(string ftpHashAlgorithm) {
			if (!ftpHashAlgorithmsByStringKeys.ContainsKey(ftpHashAlgorithm)) {
				throw new NotImplementedException("Unknown hash algorithm: " + ftpHashAlgorithm);
			}

			return ftpHashAlgorithmsByStringKeys[ftpHashAlgorithm];
		}

		/// <summary>
		/// Get string representation of FtpHashAlgorithm
		/// </summary>
		/// <param name="ftpHashAlgorithm">FtpHashAlgorithm to be converted into string</param>
		/// <returns>Name of the hash algorithm</returns>
		public static string ToString(FtpHashAlgorithm ftpHashAlgorithm)
		{
			if (!ftpHashAlgorithmsByFtpHashAlgorithmKeys.ContainsKey(ftpHashAlgorithm))
			{
				return ftpHashAlgorithm.ToString();
			}

			return ftpHashAlgorithmsByFtpHashAlgorithmKeys[ftpHashAlgorithm];
		}
	}
}
