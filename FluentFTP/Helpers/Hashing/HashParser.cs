using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FluentFTP.Helpers.Hashing {
	internal static class HashParser {

		/// <summary>
		/// Parses the received FTP hash response into a new FtpHash object.
		/// </summary>
		public static FtpHash Parse(string reply) {

			var hash = new FtpHash();

			// Current draft says the server should return this:
			//		<algorithm> <bytestart>-<byteend> <hash> <filename>

			// Note: filename might contain blanks.

			// Some servers respond with differing formats:

			//		<algorithm> <hash>							- some FileZilla versions)
			//		<algorithm> <bytestart>-<byteend> <hash>    - BrickFTP (files.com / Exavault.com)

			// Try to parse these differing formats:

			Match m;
			if (!(m = Regex.Match(reply, @"^(?<algorithm>\S+)\s(?<bytestart>[0-9]+)-(?<byteend>[0-9]+)\s(?<hash>\S+)\s*(?<filename>.*)$")).Success) {
				m = Regex.Match(reply, @"(?<algorithm>.+)\s(?<hash>.+)\s");
			}

			if (m != null && m.Success) {
				hash.Algorithm = HashAlgorithms.FromString(m.Groups["algorithm"].Value);
				hash.Value = m.Groups["hash"].Value;
			}
			else {
				// failed to parse
			}

			return hash;
		}


	}
}
