using FluentFTP;
using FluentFTP.Proxy;
using FluentFTP.Proxy.AsyncProxy;
using FluentFTP.Proxy.SyncProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Examples {
	internal static class ConnectProxySocks4 {

		// Add your Proxy server details here
		static string proxy_host = "http://myproxy.com/";
		static int proxy_port = 1234;
		static string proxy_user = "system";
		static string proxy_pass = "123";

		// Add your FTP server details here
		static string ftp_host = "123.123.123.123";
		static int ftp_port = 1234;
		static string ftp_user = "david";
		static string ftp_pass = "pass123";

		public static void ConnectAndGetListing() {

			// create an FTP client connecting through a SOCKS4 Proxy
			var client = new FtpClientSocks4Proxy(new FtpProxyProfile() {
				ProxyHost = proxy_host,
				ProxyPort = proxy_port,
				ProxyCredentials = new NetworkCredential(proxy_user, proxy_pass),
				FtpHost = ftp_host,
				FtpPort = ftp_port,
				FtpCredentials = new NetworkCredential(ftp_user, ftp_pass),
			});

			// begin connecting to the server
			client.Connect();

			// get a list of files and directories in the "/" folder
			foreach (FtpListItem item in client.GetListing("/")) {

				// if this is a file
				if (item.Type == FtpObjectType.File) {

					// get the file size
					long size = client.GetFileSize(item.FullName);

					// get modified date/time
					DateTime time = client.GetModifiedTime(item.FullName);

					// print out the file name
					Console.WriteLine(item.FullName + " - " + size.FormatBytes() + " - " + time.ToShortDateString());

				}

				// if this is a folder
				else if (item.Type == FtpObjectType.Directory) {

					// print out the folder name
					Console.WriteLine(item.FullName);

				}

			}

		}

		public static void ConnectAndManipulate() {

			// create an FTP client connecting through a SOCKS4 Proxy
			var client = new FtpClientSocks4Proxy(new FtpProxyProfile() {
				ProxyHost = proxy_host,
				ProxyPort = proxy_port,
				ProxyCredentials = new NetworkCredential(proxy_user, proxy_pass),
				FtpHost = ftp_host,
				FtpPort = ftp_port,
				FtpCredentials = new NetworkCredential(ftp_user, ftp_pass),
			});

			// begin connecting to the server
			client.Connect();

			// upload a file
			client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/MyVideo.mp4");

			// rename the uploaded file
			client.Rename("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4");

			// download the file again
			client.DownloadFile(@"C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4");

			// delete the file
			client.DeleteFile("/htdocs/MyVideo_2.mp4");

			// disconnect! good bye!
			client.Disconnect();

		}

	}
}
