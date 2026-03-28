using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP paths.
	/// </summary>
	public static class PathSanitizer {

		/// <summary>
		/// Converts the specified path into a valid and sanitized FTP file system path.
		/// Replaces invalid back-slashes with valid forward-slashes.
		/// Replaces multiple slashes with single slashes.
		/// Removes the ending postfix slash if any.
		/// Multiline commands are stripped and only the first line of the command is retained.
		/// Performs many corrections to the path to prevent path-injection and command-injection.
		/// </summary>
		/// <param name="path">The file system path</param>
		/// <returns>A secure and sanitized path formatted for FTP servers</returns>
		public static string SanitizeFtpPath(this string path) {
			if (string.IsNullOrEmpty(path)) {
				return "/";
			}

			// Replace backslashes
			if (path.IndexOf('\\') >= 0)
				path = ReplaceBackslashes(path);

			// Collapse slashes
			if (path.IndexOf("//", StringComparison.Ordinal) >= 0)
				path = CollapseSlashes(path);

			// Trim trailing slash
			if (path.Length > 1 && path[path.Length - 1] == '/')
				path = path.TrimEnd('/');

			// Trim whitespace
			if (path.Length > 0 && (char.IsWhiteSpace(path[0]) || char.IsWhiteSpace(path[path.Length - 1])))
				path = path.Trim();

			// Decode URL encoding
			if (path.IndexOf('%') >= 0)
				path = DecodeUrl(path);

			// Remove control chars
			if (ContainsControlChars(path))
				path = SanitizePayloads(path);

			// Re-normalize slashes after decode
			if (path.IndexOf('\\') >= 0)
				path = ReplaceBackslashes(path);

			if (path.IndexOf("//", StringComparison.Ordinal) >= 0)
				path = CollapseSlashes(path);

			// Remove unicode spoofing chars
			if (ContainsUnicodeControl(path))
				path = RemoveUnicodeControl(path);

			// Resolve traversal
			if (path.IndexOf("..", StringComparison.Ordinal) >= 0)
				path = ResolveTraversal(path);
			else if (path.Length == 0/* || path[0] != '/'*/)
				path = EnsureLeadingSlash(path);

			// Final trailing slash trim
			if (path.Length > 1 && path[path.Length - 1] == '/')
				path = path.TrimEnd('/');

			if (path.Length == 0)
				return "/";

			return path;
		}


		/// <summary>Replaces '\' with '/'</summary>
		private static string ReplaceBackslashes(string path) {
			return path.Replace('\\', '/');
		}

		/// <summary>Collapses multiple '/' into one</summary>
		private static string CollapseSlashes(string path) {
			var sb = new System.Text.StringBuilder(path.Length);
			bool prevSlash = false;

			foreach (char c in path) {
				if (c == '/') {
					if (!prevSlash) sb.Append(c);
					prevSlash = true;
				}
				else {
					sb.Append(c);
					prevSlash = false;
				}
			}

			return sb.ToString();
		}

		/// <summary>Checks for any control chars, newlines and command delimiters</summary>
		private static bool ContainsControlChars(string path) {
			for (int i = 0; i < path.Length; i++) {
				char c = path[i];

				// single condition: control chars, unix-command delimiters, newlines (CR / LF)
				if (c < 32 || c == 127 || c == ';' || c == '|'/* || c == '&'*/)
					return true;
			}
			return false;
		}

		/// <summary>Removes control chars and remove injected payloads</summary>
		private static string SanitizePayloads(string path) {

			var sb = new StringBuilder(path.Length);

			for (int i = 0; i < path.Length; i++) {
				char c = path[i];

				// truncate everything after the first found char
				// (control chars, unix-command delimiters, newlines (CR / LF))
				if (c < 32 || c == 127 || c == ';' || c == '|'/* || c == '&'*/) {
					break;
				}

				sb.Append(c);
			}

			return sb.ToString().TrimEnd();
		}

		/// <summary>Decodes URL encoding (double pass)</summary>
		private static string DecodeUrl(string path) {
			try {
				path = Uri.UnescapeDataString(path);
				if (path.IndexOf('%') >= 0)
					path = Uri.UnescapeDataString(path);
			}
			catch { }
			return path;
		}

		/// <summary>Checks unicode control chars</summary>
		private static bool ContainsUnicodeControl(string path) {
			for (int i = 0; i < path.Length; i++) {
				char c = path[i];
				if ((c >= '\u202A' && c <= '\u202E') || (c >= '\u2066' && c <= '\u2069'))
					return true;
			}
			return false;
		}

		/// <summary>Removes unicode spoofing chars</summary>
		private static string RemoveUnicodeControl(string path) {
			var sb = new System.Text.StringBuilder(path.Length);
			foreach (char c in path) {
				if (!((c >= '\u202A' && c <= '\u202E') || (c >= '\u2066' && c <= '\u2069')))
					sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>Resolves '.' and '..'</summary>
		private static string ResolveTraversal(string path) {
#if NET6_0_OR_GREATER
			var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
#else
			var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
#endif
			var stack = new System.Collections.Generic.List<string>(parts.Length);

			foreach (var part in parts) {
				if (part == ".") continue;

				if (part == "..") {
					if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
					continue;
				}

				stack.Add(part);
			}

			return "/" + string.Join("/", stack);
		}

		/// <summary>Ensures path starts with '/'</summary>
		private static string EnsureLeadingSlash(string path) {
			return "/" + path;
		}

	}
}
