using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentFTP {
	public static class FtpUtils {

		private static string[] sizePostfix = { "bytes", "KB", "MB", "GB", "TB" };

		/// <summary>
		/// Get a pretty file size from a byte count (eg. 35234 becomes 35.2 KB)
		/// </summary>
		/// <param name="len"></param>
		/// <returns></returns>
		public static string BytesToString(long bytes) {
			int order = 0;
			double len = bytes;
			while (len >= 1024 && order < sizePostfix.Length - 1) {
				order++;
				len = len / 1024;
			}
			return String.Format("{0:0.#} {1}", len, sizePostfix[order]);
		}

	}
}
