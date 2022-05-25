using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FluentFTP.Helpers.Hashing {
	internal static class HashParser {

		/// <summary>
		/// Parses the received FTP hash response into a new FtpHash object.
		/// </summary>
		public static FtpHash Parse(string reply) {

			// Current draft says the server should return this:
			//		SHA-256 0-49 169cd22282da7f147cb491e559e9dd filename.ext

			// Current version of FileZilla returns this:
			//		SHA-1 21c2ca15cf570582949eb59fb78038b9c27ffcaf 

			// Real reply that was failing:
			//		213 MD5 0-170500096 3197bf4ec5fa2d441c0f50264ca52f11


			var hash = new FtpHash();

			// FIX #722 - remove the FTP status code causing a wrong hash to be returned
			if (reply.StartsWith("2") && reply.Length > 10) {
				reply = reply.Substring(4);
			}

			Match m;
			if (!(m = Regex.Match(reply, @"^(?<algorithm>\S+)\s(?<bytestart>\d+)-(?<byteend>\d+)\s(?<hash>\S+)\s(?<filename>.+)$")).Success) {
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
