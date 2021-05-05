using System;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	internal static class Uris {
		/// <summary>
		/// Ensures that the URI points to a server, and not a directory or invalid path.
		/// </summary>
		/// <param name="uri"></param>
		public static void ValidateFtpServer(this Uri uri) {
			if (string.IsNullOrEmpty(uri.PathAndQuery)) {
				throw new UriFormatException("The supplied URI does not contain a valid path.");
			}

			if (uri.PathAndQuery.EndsWith("/")) {
				throw new UriFormatException("The supplied URI points at a directory.");
			}
		}


	}
}