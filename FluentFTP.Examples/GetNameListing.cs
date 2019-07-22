using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal class GetNameListingExample {
		public static void GetNameListing() {
			using (var cl = new FtpClient()) {
				cl.Credentials = new NetworkCredential("ftp", "ftp");
				cl.Host = "ftp.example.com";
				cl.Connect();

				foreach (var s in cl.GetNameListing()) {
					// load some information about the object
					// returned from the listing...
					var isDirectory = cl.DirectoryExists(s);
					var modify = cl.GetModifiedTime(s);
					var size = isDirectory ? 0 : cl.GetFileSize(s);
				}
			}
		}
	}
}