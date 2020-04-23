using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Sockets;
using FluentFTP.Servers;
#if (CORE || NETFX)
using System.Diagnostics;
#endif
#if NET45
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class FtpExtensions {
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

		/// <summary>
		/// Checks if the reply contains any of the known error strings
		/// </summary>
		public static bool IsKnownError(this string reply, string[] strings) {
			reply = reply.ToLower();
			foreach (var msg in strings) {
				if (reply.Contains(msg)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Converts the specified path into a valid FTP file system path
		/// </summary>
		/// <param name="path">The file system path</param>
		/// <returns>A path formatted for FTP</returns>
		public static string GetFtpPath(this string path) {
			if (string.IsNullOrEmpty(path)) {
				return "./";
			}

			path = path.Replace('\\', '/');
			path = Regex.Replace(path, "[/]+", "/");
			path = path.TrimEnd('/');

			if (path.Length == 0) {
				path = "/";
			}

			return path;
		}

		/// <summary>
		/// Creates a valid FTP path by appending the specified segments to this string
		/// </summary>
		/// <param name="path">This string</param>
		/// <param name="segments">The path segments to append</param>
		/// <returns>A valid FTP path</returns>
		public static string GetFtpPath(this string path, params string[] segments) {
			if (string.IsNullOrEmpty(path)) {
				path = "./";
			}

			foreach (var part in segments) {
				if (part != null) {
					if (path.Length > 0 && !path.EndsWith("/")) {
						path += "/";
					}

					path += Regex.Replace(part.Replace('\\', '/'), "[/]+", "/").TrimEnd('/');
				}
			}

			path = Regex.Replace(path.Replace('\\', '/'), "[/]+", "/").TrimEnd('/');
			if (path.Length == 0) {
				path = "/";
			}

			return path;
		}

		/// <summary>
		/// Gets the parent directory path (formatted for a FTP server)
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The parent directory path</returns>
		public static string GetFtpDirectoryName(this string path) {
			var tpath = path == null ? "" : path.GetFtpPath();

			if (tpath.Length == 0 || tpath == "/") {
				return "/";
			}

			var lastslash = tpath.LastIndexOf('/');
			if (lastslash < 0) {
				return ".";
			}

			if (lastslash == 0) {
				return "/";
			}

			return tpath.Substring(0, lastslash);
		}

		/// <summary>
		/// Gets the file name and extension from the path
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <returns>The file name</returns>
		public static string GetFtpFileName(this string path) {
			var tpath = path == null ? null : path;
			var lastslash = -1;

			if (tpath == null) {
				return null;
			}

			lastslash = tpath.LastIndexOf('/');
			if (lastslash < 0) {
				return tpath;
			}

			lastslash += 1;
			if (lastslash >= tpath.Length) {
				return tpath;
			}

			return tpath.Substring(lastslash, tpath.Length - lastslash);
		}

		private static string[] FtpDateFormats = { "yyyyMMddHHmmss", "yyyyMMddHHmmss'.'f", "yyyyMMddHHmmss'.'ff", "yyyyMMddHHmmss'.'fff", "MMM dd  yyyy", "MMM  d  yyyy", "MMM dd HH:mm", "MMM  d HH:mm" };

		/// <summary>
		/// Tries to convert the string FTP date representation into a <see cref="DateTime"/> object
		/// </summary>
		/// <param name="date">The date</param>
		/// <param name="style">UTC/Local Time</param>
		/// <returns>A <see cref="DateTime"/> object representing the date, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public static DateTime GetFtpDate(this string date, DateTimeStyles style) {
			DateTime parsed;

			if (DateTime.TryParseExact(date, FtpDateFormats, CultureInfo.InvariantCulture, style, out parsed)) {
				return parsed;
			}

			return DateTime.MinValue;
		}

		private static string[] sizePostfix = { "bytes", "KB", "MB", "GB", "TB" };

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this int bytes) {
			return ((long)bytes).FileSizeToString();
		}

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this uint bytes) {
			return ((long)bytes).FileSizeToString();
		}

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this ulong bytes) {
			return ((long)bytes).FileSizeToString();
		}

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this long bytes) {
			var order = 0;
			double len = bytes;
			while (len >= 1024 && order < sizePostfix.Length - 1) {
				order++;
				len = len / 1024;
			}

			return string.Format("{0:0.#} {1}", len, sizePostfix[order]);
		}

#if NET45
		/// <summary>
		/// This creates a <see cref="System.Threading.Tasks.Task{TResult}"/> that represents a pair of begin and end methods
		/// that conform to the Asynchronous Programming Model pattern.  This extends the maximum amount of arguments from
		///  <see cref="o:System.Threading.TaskFactory.FromAsync"/> to 4 from a 3.  
		/// </summary>
		/// <typeparam name="TArg1">The type of the first argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TArg2">The type of the second argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TArg3">The type of the third argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TArg4">The type of the forth argument passed to the <paramref name="beginMethod"/> delegate</typeparam>
		/// <typeparam name="TResult">The type of the result.</typeparam>
		/// <param name="factory">The <see cref="TaskFactory"/> used</param>
		/// <param name="beginMethod">The delegate that begins the asynchronous operation</param>
		/// <param name="endMethod">The delegate that ends the asynchronous operation</param>
		/// <param name="arg1">The first argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="arg2">The second argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="arg3">The third argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="arg4">The forth argument passed to the <paramref name="beginMethod"/> delegate</param>
		/// <param name="state">An object containing data to be used by the <paramref name="beginMethod"/> delegate</param>
		/// <returns>The created <see cref="System.Threading.Tasks.Task{TResult}"/> that represents the asynchronous operation</returns>
		/// <exception cref="System.ArgumentNullException">
		/// beginMethod is null
		/// or
		/// endMethod is null
		/// </exception>
		public static Task<TResult> FromAsync<TArg1, TArg2, TArg3, TArg4, TResult>(this TaskFactory factory,
			Func<TArg1, TArg2, TArg3, TArg4, AsyncCallback, object, IAsyncResult> beginMethod,
			Func<IAsyncResult, TResult> endMethod,
			TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, object state) {
			if (beginMethod == null) {
				throw new ArgumentNullException("beginMethod");
			}

			if (endMethod == null) {
				throw new ArgumentNullException("endMethod");
			}

			TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, factory.CreationOptions);
			try {
				AsyncCallback callback = delegate(IAsyncResult asyncResult) { tcs.TrySetResult(endMethod(asyncResult)); };

				beginMethod(arg1, arg2, arg3, arg4, callback, state);
			}
			catch {
				tcs.TrySetResult(default(TResult));
				throw;
			}

			return tcs.Task;
		}
#endif

		/// <summary>
		/// Validates that the FtpError flags set are not in an invalid combination.
		/// </summary>
		/// <param name="options">The error handling options set</param>
		/// <returns>True if a valid combination, otherwise false</returns>
		public static bool IsValidCombination(this FtpError options) {
			return options != (FtpError.Stop | FtpError.Throw) &&
				   options != (FtpError.Throw | FtpError.Stop | FtpError.DeleteProcessed);
		}

		/// <summary>
		/// Checks if every character in the string is whitespace, or the string is null.
		/// </summary>
		public static bool IsNullOrWhiteSpace(string value) {
			if (value == null) {
				return true;
			}

			for (var i = 0; i < value.Length; i++) {
				if (!char.IsWhiteSpace(value[i])) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if the string is null or 0 length.
		/// </summary>
		public static bool IsBlank(this string value) {
			return value == null || value.Length == 0;
		}

		/// <summary>
		/// Checks if the array is null or 0 length.
		/// </summary>
		public static bool IsBlank(this IList value) {
			return value == null || value.Count == 0;
		}

		/// <summary>
		/// Checks if the array is null or 0 length.
		/// </summary>
		public static bool IsBlank(this IEnumerable value) {
			if (value == null) {
				return true;
			}

			if (value is IList) {
				return ((IList)value).Count == 0;
			}

			if (value is byte[]) {
				return ((byte[])value).Length == 0;
			}

			return false;
		}

		/// <summary>
		/// Join the given strings by a delimiter.
		/// </summary>
		public static string Join(this string[] values, string delimiter) {
			return string.Join(delimiter, values);
		}

		/// <summary>
		/// Join the given strings by a delimiter.
		/// </summary>
		public static string Join(this List<string> values, string delimiter) {
#if NET20 || NET35
			return string.Join(delimiter, values.ToArray());
#else
			return string.Join(delimiter, values);
#endif
		}

		/// <summary>
		/// Adds a prefix to the given strings, returns a new array.
		/// </summary>
		public static string[] AddPrefix(this string[] values, string prefix, bool trim = false) {
			var results = new List<string>();
			foreach (var v in values) {
				var txt = prefix + (trim ? v.Trim() : v);
				results.Add(txt);
			}

			return results.ToArray();
		}

		/// <summary>
		/// Adds a prefix to the given strings, returns a new array.
		/// </summary>
		public static List<string> AddPrefix(this List<string> values, string prefix, bool trim = false) {
			var results = new List<string>();
			foreach (var v in values) {
				var txt = prefix + (trim ? v.Trim() : v);
				results.Add(txt);
			}

			return results;
		}

		/// <summary>
		/// Ensure a string has the given prefix
		/// </summary>
		public static string EnsurePrefix(this string text, string prefix) {
			if (!text.StartsWith(prefix)) {
				return prefix + text;
			}

			return text;
		}

		/// <summary>
		/// Ensure a string has the given postfix
		/// </summary>
		public static string EnsurePostfix(this string text, string postfix) {
			if (!text.EndsWith(postfix)) {
				return text + postfix;
			}

			return text;
		}

		/// <summary>
		/// Remove a prefix from a string, only if it has the given prefix
		/// </summary>
		public static string RemovePrefix(this string text, string prefix) {
			if (text.StartsWith(prefix)) {
				return text.Substring(prefix.Length);
			}
			return text;
		}

		/// <summary>
		/// Remove a postfix from a string, only if it has the given postfix
		/// </summary>
		public static string RemovePostfix(this string text, string postfix) {
			if (text.EndsWith(postfix)) {
				return text.Substring(0, text.Length - postfix.Length);
			}
			return text;
		}

		/// <summary>
		/// Combine the given base path with the relative path
		/// </summary>
		public static string CombineLocalPath(this string path, string fileOrFolder) {

			string directorySeperator = Path.DirectorySeparatorChar.ToString();

			// fast mode if there is exactly one slash between path & file
			var pathHasSep = path.EndsWith(directorySeperator);
			var fileHasSep = fileOrFolder.StartsWith(directorySeperator);
			if ((pathHasSep && !fileHasSep) || (!pathHasSep && fileHasSep)) {
				return path + fileOrFolder;
			}

			// slow mode if slashes need to be fixed
			if (pathHasSep && fileHasSep) {
				return path + fileOrFolder.Substring(1);
			}
			if (!pathHasSep && !fileHasSep) {
				return path + directorySeperator + fileOrFolder;
			}

			// nothing
			return null;
		}

		/// <summary>
		/// Adds a prefix to the given strings, returns a new array.
		/// </summary>
		public static List<string> ItemsToString(this object[] args) {
			var results = new List<string>();
			if (args == null) {
				return results;
			}

			foreach (var v in args) {
				string txt;
				if (v == null) {
					txt = "null";
				}
				else if (v is string) {
					txt = "\"" + v as string + "\"";
				}
				else {
					txt = v.ToString();
				}

				results.Add(txt);
			}

			return results;
		}

#if NET20 || NET35
		public static bool HasFlag(this FtpHashAlgorithm flags, FtpHashAlgorithm flag) {
			return (flags & flag) == flag;
		}

		public static bool HasFlag(this FtpListOption flags, FtpListOption flag) {
			return (flags & flag) == flag;
		}

		public static bool HasFlag(this FtpCompareOption flags, FtpCompareOption flag) {
			return (flags & flag) == flag;
		}

		public static bool HasFlag(this FtpVerify flags, FtpVerify flag) {
			return (flags & flag) == flag;
		}

		public static bool HasFlag(this FtpError flags, FtpError flag) {
			return (flags & flag) == flag;
		}

		public static void Restart(this Stopwatch watch) {
			watch.Stop();
			watch.Start();
		}
#endif

		/// <summary>
		/// Checks if the given file exists in the given file listing.
		/// Supports servers that return:  1) full paths,  2) only filenames,  3) full paths without slash prefixed
		/// </summary>
		/// <param name="fileList">The listing returned by GetNameListing</param>
		/// <param name="path">The full file path you want to check</param>
		/// <returns></returns>
		public static bool FileExistsInNameListing(string[] fileList, string path) {
			// exit quickly if no paths
			if (fileList.Length == 0) {
				return false;
			}

			// cleanup file path, get file name
			var pathName = path.GetFtpFileName();
			var pathPrefixed = path.EnsurePrefix("/");

			// per entry in the name list
			foreach (var fileListEntry in fileList) {
				// FIX: support servers that return:  1) full paths,  2) only filenames,  3) full paths without slash prefixed
				if (fileListEntry == pathName || fileListEntry == path || fileListEntry.EnsurePrefix("/") == pathPrefixed) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if the given file exists in the given file listing.
		/// </summary>
		/// <param name="fileList">The listing returned by GetListing</param>
		/// <param name="path">The full file path you want to check</param>
		/// <returns></returns>
		public static bool FileExistsInListing(FtpListItem[] fileList, string path) {
			// exit quickly if no paths
			if (fileList == null || fileList.Length == 0) {
				return false;
			}

			// cleanup file path, get file name
			var trimSlash = new char[] { '/' };
			var pathClean = path.Trim(trimSlash);

			// per entry in the list
			foreach (var fileListEntry in fileList) {
				if (fileListEntry.FullName.Trim(trimSlash) == pathClean) {
					return true;
				}
			}

			return false;
		}


		/// <summary>
		/// Checks if the given path is a root directory or working directory path
		/// </summary>
		/// <param name="ftppath"></param>
		/// <returns></returns>
		public static bool IsFtpRootDirectory(this string ftppath) {
			return ftppath == "." || ftppath == "./" || ftppath == "/";
		}

		/// <summary>
		/// Calculate the CHMOD integer value given a set of permissions.
		/// </summary>
		public static int CalcChmod(FtpPermission owner, FtpPermission group, FtpPermission other) {
			var chmod = 0;

			if (HasPermission(owner, FtpPermission.Read)) {
				chmod += 400;
			}

			if (HasPermission(owner, FtpPermission.Write)) {
				chmod += 200;
			}

			if (HasPermission(owner, FtpPermission.Execute)) {
				chmod += 100;
			}

			if (HasPermission(group, FtpPermission.Read)) {
				chmod += 40;
			}

			if (HasPermission(group, FtpPermission.Write)) {
				chmod += 20;
			}

			if (HasPermission(group, FtpPermission.Execute)) {
				chmod += 10;
			}

			if (HasPermission(other, FtpPermission.Read)) {
				chmod += 4;
			}

			if (HasPermission(other, FtpPermission.Write)) {
				chmod += 2;
			}

			if (HasPermission(other, FtpPermission.Execute)) {
				chmod += 1;
			}

			return chmod;
		}

		/// <summary>
		/// Checks if the permission value has the given flag
		/// </summary>
		public static bool HasPermission(FtpPermission owner, FtpPermission flag) {
			return (owner & flag) == flag;
		}

		/// <summary>
		/// Escape a string into a valid C# string literal.
		/// Implementation from StackOverflow - https://stackoverflow.com/a/14087738
		/// </summary>
		public static string EscapeStringLiteral(this string input) {
			var literal = new StringBuilder(input.Length + 2);
			literal.Append("\"");
			foreach (var c in input) {
				switch (c) {
					case '\'':
						literal.Append(@"\'");
						break;

					case '\"':
						literal.Append("\\\"");
						break;

					case '\\':
						literal.Append(@"\\");
						break;

					case '\0':
						literal.Append(@"\0");
						break;

					case '\a':
						literal.Append(@"\a");
						break;

					case '\b':
						literal.Append(@"\b");
						break;

					case '\f':
						literal.Append(@"\f");
						break;

					case '\n':
						literal.Append(@"\n");
						break;

					case '\r':
						literal.Append(@"\r");
						break;

					case '\t':
						literal.Append(@"\t");
						break;

					case '\v':
						literal.Append(@"\v");
						break;

					default:

						// ASCII printable character
						if (c >= 0x20 && c <= 0x7e) {
							literal.Append(c);
						}
						else {
							// As UTF16 escaped character
							literal.Append(@"\u");
							literal.Append(((int)c).ToString("x4"));
						}

						break;
				}
			}

			literal.Append("\"");
			return literal.ToString();
		}


		/// <summary>
		/// Split into fields by splitting on tokens
		/// </summary>
		public static string[] SplitString(this string str) {
			var allTokens = new List<string>(str.Split(null));
			for (var i = allTokens.Count - 1; i >= 0; i--) {
				if (((string)allTokens[i]).Trim().Length == 0) {
					allTokens.RemoveAt(i);
				}
			}

			return (string[])allTokens.ToArray();
		}

		/// <summary>
		/// Get the full path of a given FTP Listing entry
		/// </summary>
		public static void CalculateFullFtpPath(this FtpListItem item, FtpClient client, string path, bool isVMS) {
			// EXIT IF NO DIR PATH PROVIDED
			if (path == null) {
				// check if the path is absolute
				if (IsAbsolutePath(item.Name)) {
					item.FullName = item.Name;
					item.Name = item.Name.GetFtpFileName();
				}

				return;
			}


			// ONLY IF DIR PATH PROVIDED

			// if this is a vax/openvms file listing
			// there are no slashes in the path name
			if (isVMS) {
				item.FullName = path + item.Name;
			}
			else {
				//this.client.LogStatus(item.Name);

				// remove globbing/wildcard from path
				if (path.GetFtpFileName().Contains("*")) {
					path = path.GetFtpDirectoryName();
				}

				if (item.Name != null) {
					// absolute path? then ignore the path input to this method.
					if (IsAbsolutePath(item.Name)) {
						item.FullName = item.Name;
						item.Name = item.Name.GetFtpFileName();
					}
					else if (path != null) {
						item.FullName = path.GetFtpPath(item.Name); //.GetFtpPathWithoutGlob();
					}
					else {
						client.LogStatus(FtpTraceLevel.Warn, "Couldn't determine the full path of this object: " +
															 Environment.NewLine + item.ToString());
					}
				}


				// if a link target is set and it doesn't include an absolute path
				// then try to resolve it.
				if (item.LinkTarget != null && !item.LinkTarget.StartsWith("/")) {
					if (item.LinkTarget.StartsWith("./")) {
						item.LinkTarget = path.GetFtpPath(item.LinkTarget.Remove(0, 2)).Trim();
					}
					else {
						item.LinkTarget = path.GetFtpPath(item.LinkTarget).Trim();
					}
				}
			}
		}

		/// <summary>
		/// Checks if this FTP path is a top level path
		/// </summary>
		public static bool IsAbsolutePath(this string path) {
			return path.StartsWith("/") || path.StartsWith("./") || path.StartsWith("../");
		}

		/// <summary>
		/// Calculates the CHMOD value from the permissions flags
		/// </summary>
		public static void CalculateChmod(this FtpListItem item) {
			item.Chmod = CalcChmod(item.OwnerPermissions, item.GroupPermissions, item.OthersPermissions);
		}

		/// <summary>
		/// Calculates the permissions flags from the CHMOD value
		/// </summary>
		public static void CalculateUnixPermissions(this FtpListItem item, string permissions) {
			var perms = Regex.Match(permissions,
				@"[\w-]{1}(?<owner>[\w-]{3})(?<group>[\w-]{3})(?<others>[\w-]{3})",
				RegexOptions.IgnoreCase);

			if (perms.Success) {
				if (perms.Groups["owner"].Value.Length == 3) {
					if (perms.Groups["owner"].Value[0] == 'r') {
						item.OwnerPermissions |= FtpPermission.Read;
					}

					if (perms.Groups["owner"].Value[1] == 'w') {
						item.OwnerPermissions |= FtpPermission.Write;
					}

					if (perms.Groups["owner"].Value[2] == 'x' || perms.Groups["owner"].Value[2] == 's') {
						item.OwnerPermissions |= FtpPermission.Execute;
					}

					if (perms.Groups["owner"].Value[2] == 's' || perms.Groups["owner"].Value[2] == 'S') {
						item.SpecialPermissions |= FtpSpecialPermissions.SetUserID;
					}
				}

				if (perms.Groups["group"].Value.Length == 3) {
					if (perms.Groups["group"].Value[0] == 'r') {
						item.GroupPermissions |= FtpPermission.Read;
					}

					if (perms.Groups["group"].Value[1] == 'w') {
						item.GroupPermissions |= FtpPermission.Write;
					}

					if (perms.Groups["group"].Value[2] == 'x' || perms.Groups["group"].Value[2] == 's') {
						item.GroupPermissions |= FtpPermission.Execute;
					}

					if (perms.Groups["group"].Value[2] == 's' || perms.Groups["group"].Value[2] == 'S') {
						item.SpecialPermissions |= FtpSpecialPermissions.SetGroupID;
					}
				}

				if (perms.Groups["others"].Value.Length == 3) {
					if (perms.Groups["others"].Value[0] == 'r') {
						item.OthersPermissions |= FtpPermission.Read;
					}

					if (perms.Groups["others"].Value[1] == 'w') {
						item.OthersPermissions |= FtpPermission.Write;
					}

					if (perms.Groups["others"].Value[2] == 'x' || perms.Groups["others"].Value[2] == 't') {
						item.OthersPermissions |= FtpPermission.Execute;
					}

					if (perms.Groups["others"].Value[2] == 't' || perms.Groups["others"].Value[2] == 'T') {
						item.SpecialPermissions |= FtpSpecialPermissions.Sticky;
					}
				}

				CalculateChmod(item);
			}
		}

		/// <summary>
		/// Checks if all the characters in this string are digits or dots
		/// </summary>
		public static bool IsNumeric(this string field) {
			field = field.Replace(".", ""); // strip dots
			for (var i = 0; i < field.Length; i++) {
				if (!char.IsDigit(field[i])) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if the string contains any of the given values
		/// </summary>
		public static bool ContainsAny(this string field, string[] values, int afterChar = -1) {
			foreach (var value in values) {
				if (field.IndexOf(value, StringComparison.Ordinal) > afterChar) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Ensures the given item is only added once. If it was not present true is returned, else false is returned.
		/// </summary>
		public static bool AddOnce<T>(this List<T> items, T item) {
			if (!items.Contains(item)) {
				items.Add(item);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Ensures the given directory exists.
		/// </summary>
		public static bool EnsureDirectory(this string localPath) {
			if (!Directory.Exists(localPath)) {
				Directory.CreateDirectory(localPath);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Checks if the operation was successful or skipped (indicating success).
		/// </summary>
		public static bool IsSuccess(this FtpStatus status) {
			return status == FtpStatus.Success || status == FtpStatus.Skipped;
		}

		/// <summary>
		/// Checks if the operation has failed.
		/// </summary>
		public static bool IsFailure(this FtpStatus status) {
			return status == FtpStatus.Failed;
		}

		/// <summary>
		/// Checks if RexEx Pattern is valid
		/// </summary>
		public static bool IsValidRegEx(this string pattern) {
			bool isValid = true;

			if ((pattern != null) && (pattern.Trim().Length > 0)) {
				try {
					Regex.Match("", pattern);
				}
				catch (ArgumentException) {
					// BAD PATTERN: Syntax error
					isValid = false;
				}
			}
			else {
				//BAD PATTERN: Pattern is null or blank
				isValid = false;
			}

			return (isValid);
		}

		/// <summary>
		/// Converts a Windows or Unix-style path into its segments for segment-wise processing
		/// </summary>
		/// <returns></returns>
		public static string[] GetPathSegments(this string path) {
			if (path.Contains("/")) {
				return path.Split('/');
			}
			else if (path.Contains("\\")) {
				return path.Split('\\');
			}
			else {
				return new string[] { path };
			}
		}

		/// <summary>
		/// Check if operation can resume after <see cref="IOException"/>.
		/// </summary>
		/// <param name="exception">Received exception.</param>
		/// <returns>Result of checking.</returns>
		public static bool IsResumeAllowed(this IOException exception)
		{
			// resume if server disconnects midway (fixes #39 and #410)
			if (exception.InnerException != null || exception.Message.IsKnownError(FtpServerStrings.unexpectedEOF))
			{
				if (exception.InnerException is SocketException socketException)
				{
#if CORE
					return (int)socketException.SocketErrorCode == 10054;
#else
					return socketException.ErrorCode == 10054;
#endif
				}

				return true;
			}

			return false;
		}
	}
}