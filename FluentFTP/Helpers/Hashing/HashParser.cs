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

			Match m;

			//  <Hash Method Name> [Optional Range] <Hash Code> [Optional File Name/Path]

			m = Regex.Match(reply, @"^(?<algorithm>\S+)(?: \d+-\d+)? (?<hash>\S+)");

			if (m != null && m.Success) {
				hash.Algorithm = HashAlgorithms.FromString(m.Groups["algorithm"].Value);
				hash.Value = m.Groups["hash"].Value;
			}

			return hash;
		}


	}
}
