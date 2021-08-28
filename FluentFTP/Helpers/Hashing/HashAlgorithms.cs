using System;
using System.Collections.Generic;

namespace FluentFTP.Helpers.Hashing {
	/// <summary>
	/// Helper class to convert FtpHashAlgorithm
	/// </summary>
	internal static class HashAlgorithms {
		private static readonly Dictionary<string, FtpHashAlgorithm> NameToEnum = new Dictionary<string, FtpHashAlgorithm> {
			{ "SHA-1", FtpHashAlgorithm.SHA1 },
			{ "SHA-256", FtpHashAlgorithm.SHA256 },
			{ "SHA-512", FtpHashAlgorithm.SHA512 },
			{ "MD5", FtpHashAlgorithm.MD5 },
			{ "CRC", FtpHashAlgorithm.CRC },
		};

		private static readonly Dictionary<FtpHashAlgorithm, string> EnumToName = new Dictionary<FtpHashAlgorithm, string> {
			{ FtpHashAlgorithm.SHA1, "SHA-1" },
			{ FtpHashAlgorithm.SHA256, "SHA-256" },
			{ FtpHashAlgorithm.SHA512, "SHA-512" },
			{ FtpHashAlgorithm.MD5, "MD5" },
			{ FtpHashAlgorithm.CRC, "CRC" },
		};

		/// <summary>
		/// Get FtpHashAlgorithm from it's string representation
		/// </summary>
		/// <param name="name">Name of the hash algorithm</param>
		/// <returns>The FtpHashAlgorithm</returns>
		public static FtpHashAlgorithm FromString(string name) {
			if (!NameToEnum.ContainsKey(name.ToUpper())) {
				throw new NotImplementedException("Unknown hash algorithm: " + name);
			}

			return NameToEnum[name];
		}

		/// <summary>
		/// Get string representation of FtpHashAlgorithm
		/// </summary>
		/// <param name="name">FtpHashAlgorithm to be converted into string</param>
		/// <returns>Name of the hash algorithm</returns>
		public static string ToString(FtpHashAlgorithm name)
		{
			if (!EnumToName.ContainsKey(name))
			{
				return name.ToString();
			}

			return EnumToName[name];
		}
	}
}
