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

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class LocalPaths {

		/// <summary>
		/// Returns true if the given path is a directory path.
		/// </summary>
		public static bool IsLocalFolderPath(string localPath) {
			return localPath.EndsWith("/") || localPath.EndsWith("\\") || Directory.Exists(localPath);
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

	}
}