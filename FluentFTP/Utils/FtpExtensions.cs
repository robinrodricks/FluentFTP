using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
#if (CORE || NETFX)
using System.Diagnostics;
#endif
#if NET45
using System.Threading.Tasks;
using System.Collections;
#endif

namespace FluentFTP {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class FtpExtensions {

		/// <summary>
		/// Converts the specified path into a valid FTP file system path
		/// </summary>
		/// <param name="path">The file system path</param>
		/// <returns>A path formatted for FTP</returns>
		public static string GetFtpPath(this string path) {
			if (String.IsNullOrEmpty(path))
				return "./";

			path = path.Replace('\\', '/');
			path = Regex.Replace(path, "[/]+", "/");
			path = path.TrimEnd('/');

			if (path.Length == 0)
				path = "/";

			return path;
		}

		/// <summary>
		/// Creates a valid FTP path by appending the specified segments to this string
		/// </summary>
		/// <param name="path">This string</param>
		/// <param name="segments">The path segments to append</param>
		/// <returns>A valid FTP path</returns>
		public static string GetFtpPath(this string path, params string[] segments) {
			if (String.IsNullOrEmpty(path))
				path = "./";

			foreach (string part in segments) {
				if (part != null) {
					if (path.Length > 0 && !path.EndsWith("/"))
						path += "/";
					path += Regex.Replace(part.Replace('\\', '/'), "[/]+", "/").TrimEnd('/');
				}
			}

			path = Regex.Replace(path.Replace('\\', '/'), "[/]+", "/").TrimEnd('/');
			if (path.Length == 0)
				path = "/";

			/*if (!path.StartsWith("/") || !path.StartsWith("./"))
				path = "./" + path;*/

			return path;
		}

		/// <summary>
		/// Gets the parent directory path (formatted for a FTP server)
		/// </summary>
		/// <param name="path">The path</param>
		/// <returns>The parent directory path</returns>
		public static string GetFtpDirectoryName(this string path) {
			string tpath = (path == null ? "" : path.GetFtpPath());

			if (tpath.Length == 0 || tpath == "/")
				return "/";

			int lastslash = tpath.LastIndexOf('/');
			if (lastslash < 0)
				return ".";
			if (lastslash == 0)
				return "/";

			return tpath.Substring(0, lastslash);
		}

		/*public static string GetFtpDirectoryName(this string path) {
			if (path == null || path.Length == 0 || path.GetFtpPath() == "/")
				return "/";

			return System.IO.Path.GetDirectoryName(path).GetFtpPath();
		}*/

		/// <summary>
		/// Gets the file name and extension from the path
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <returns>The file name</returns>
		public static string GetFtpFileName(this string path) {
			string tpath = (path == null ? null : path);
			int lastslash = -1;

			if (tpath == null)
				return null;

			lastslash = tpath.LastIndexOf('/');
			if (lastslash < 0)
				return tpath;

			lastslash += 1;
			if (lastslash >= tpath.Length)
				return tpath;

			return tpath.Substring(lastslash, tpath.Length - lastslash);
		}

		/*public static string GetFtpFileName(this string path) {
			return System.IO.Path.GetFileName(path).GetFtpPath();
		}*/

		private static string[] FtpDateFormats = { "yyyyMMddHHmmss", "yyyyMMddHHmmss'.'f", "yyyyMMddHHmmss'.'ff", "yyyyMMddHHmmss'.'fff", "MMM dd  yyyy","MMM  d  yyyy","MMM dd HH:mm","MMM  d HH:mm" };

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
			int order = 0;
			double len = bytes;
			while (len >= 1024 && order < sizePostfix.Length - 1) {
				order++;
				len = len / 1024;
			}
			return String.Format("{0:0.#} {1}", len, sizePostfix[order]);
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
	        if (beginMethod == null)
	            throw new ArgumentNullException("beginMethod");

	        if (endMethod == null)
	            throw new ArgumentNullException("endMethod");

	        TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, factory.CreationOptions);
	        try {
	            AsyncCallback callback = delegate(IAsyncResult asyncResult) {
	                tcs.TrySetResult(endMethod(asyncResult));
	            };

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
			if (value == null) return true;

			for (int i = 0; i < value.Length; i++) {
				if (!Char.IsWhiteSpace(value[i])) return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if the string is null or 0 length.
		/// </summary>
		public static bool IsBlank(this string value) {
			return value == null || value.Length == 0;
		}

#if NET45
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
			if (value == null){
				return true;
			}
			if (value is IList){
				return ((IList)value).Count == 0;
			}
			if (value is byte[]){
				return ((byte[])value).Length == 0;
			}
			return false;
		}
#endif

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
			List<string> results = new List<string>();
			foreach (string v in values) {
				string txt = prefix + (trim ? v.Trim() : v);
				results.Add(txt);
			}
			return results.ToArray();
		}
		/// <summary>
		/// Adds a prefix to the given strings, returns a new array.
		/// </summary>
		public static List<string> AddPrefix(this List<string> values, string prefix, bool trim = false) {
			List<string> results = new List<string>();
			foreach (string v in values) {
				string txt = prefix + (trim ? v.Trim() : v);
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
		/// Adds a prefix to the given strings, returns a new array.
		/// </summary>
		public static List<string> ItemsToString(this object[] args) {
			List<string> results = new List<string>();
			if (args == null) {
				return results;
			}
			foreach (object v in args) {
				string txt;
				if (v == null){
					txt = "null";
				} else if (v is string) {
					txt = ("\"" + v as string + "\"");
				} else {
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
		public static bool HasFlag(this FtpCapability flags, FtpCapability flag) {
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
			string pathName = path.GetFtpFileName();
			string pathPrefixed = path.EnsurePrefix("/");

			// per entry in the name list
			foreach (string fileListEntry in fileList) {

				// FIX: support servers that return:  1) full paths,  2) only filenames,  3) full paths without slash prefixed
				if (fileListEntry == pathName || fileListEntry == path || fileListEntry.EnsurePrefix("/") == pathPrefixed) {
					return true;
				}
			}
			return false;
		}


	}
}