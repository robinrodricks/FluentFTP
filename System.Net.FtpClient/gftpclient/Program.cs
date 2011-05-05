using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace gftpclient {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FormMain());
		}

		//
		// http://sharpertutorials.com/pretty-format-bytes-kb-mb-gb/
		//
		public static string FormatBytes(long bytes) {
			const int scale = 1024;
			string[] orders = new string[] { "GB", "MB", "KB", "B" };
			long max = (long)Math.Pow(scale, orders.Length - 1);

			foreach (string order in orders) {
				if (bytes > max)
					return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

				max /= scale;
			}
			return "0 B";
		}
	}
}
