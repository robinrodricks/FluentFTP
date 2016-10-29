using System;

namespace Examples {
	public static class Extensions {
		public static string FormatBytes(this long val) {
			return ((double)val).FormatBytes();
		}

		public static string FormatBytes(this int val) {
			return ((double)val).FormatBytes();
		}

		public static string FormatBytes(this double val) {
			string[] units = new string[] { "B", "KB", "MB", "GB", "TB" };
			int count = 0;

			while (count + 1 < units.Length && val >= 1024) {
				count += 1;
				val /= 1024;
			}

			return string.Format("{0:0.00} {1}", val, units[count]);
		}
	}
}

