using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class QuickStart {

		public static void Example() {

			// create an FTP client and specify the host, username and password
			// (delete the credentials to use the "anonymous" account)
			var client = new FtpClient("123.123.123.123", "david", "pass123");

			// connect to the server and automatically detect working FTP settings
			client.AutoConnect();

			// get a list of files and directories in the "/htdocs" folder
			foreach (FtpListItem item in client.GetListing("/htdocs")) {

				// if this is a file
				if (item.Type == FtpObjectType.File) {

					// get the file size
					long size = client.GetFileSize(item.FullName);

					// calculate a hash for the file on the server side (default algorithm)
					FtpHash hash = client.GetChecksum(item.FullName);
				}

				// get modified date/time of the file or folder
				DateTime time = client.GetModifiedTime(item.FullName);

			}

			// upload a file
			client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/MyVideo.mp4");

			// move the uploaded file
			client.MoveFile("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4");

			// download the file again
			client.DownloadFile(@"C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4");

			// compare the downloaded file with the server
			if (client.CompareFile(@"C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4") == FtpCompareResult.Equal) { }

			// delete the file
			client.DeleteFile("/htdocs/MyVideo_2.mp4");

			// upload a folder and all its files
			client.UploadDirectory(@"C:\website\videos\", @"/public_html/videos", FtpFolderSyncMode.Update);

			// upload a folder and all its files, and delete extra files on the server
			client.UploadDirectory(@"C:\website\assets\", @"/public_html/assets", FtpFolderSyncMode.Mirror);

			// download a folder and all its files
			client.DownloadDirectory(@"C:\website\logs\", @"/public_html/logs", FtpFolderSyncMode.Update);

			// download a folder and all its files, and delete extra files on disk
			client.DownloadDirectory(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror);

			// delete a folder recursively
			client.DeleteDirectory("/htdocs/extras/");

			// check if a file exists
			if (client.FileExists("/htdocs/big2.txt")) { }

			// check if a folder exists
			if (client.DirectoryExists("/htdocs/extras/")) { }

			// upload a file and retry 3 times before giving up
			client.Config.RetryAttempts = 3;
			client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/big.txt", FtpRemoteExists.Overwrite, false, FtpVerify.Retry);

			// disconnect! good bye!
			client.Disconnect();

		}

		public static async Task ExampleAsync() {

			// create an FTP client and specify the host, username and password
			// (delete the credentials to use the "anonymous" account)
			var client = new AsyncFtpClient("123.123.123.123", "david", "pass123");

			// connect to the server and automatically detect working FTP settings
			await client.AutoConnect();

			// get a list of files and directories in the "/htdocs" folder
			foreach (FtpListItem item in await client.GetListing("/htdocs")) {

				// if this is a file
				if (item.Type == FtpObjectType.File) {

					// get the file size
					long size = await client.GetFileSize(item.FullName);

					// calculate a hash for the file on the server side (default algorithm)
					FtpHash hash = await client.GetChecksum(item.FullName);
				}

				// get modified date/time of the file or folder
				DateTime time = await client.GetModifiedTime(item.FullName);
			}

			// upload a file
			await client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/MyVideo.mp4");

			// move the uploaded file
			await client.MoveFile("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4");

			// download the file again
			await client.DownloadFile(@"C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4");

			// compare the downloaded file with the server
			if (await client.CompareFile(@"C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4") == FtpCompareResult.Equal) { }

			// delete the file
			await client.DeleteFile("/htdocs/MyVideo_2.mp4");

			// upload a folder and all its files
			await client.UploadDirectory(@"C:\website\videos\", @"/public_html/videos", FtpFolderSyncMode.Update);

			// upload a folder and all its files, and delete extra files on the server
			await client.UploadDirectory(@"C:\website\assets\", @"/public_html/assets", FtpFolderSyncMode.Mirror);

			// download a folder and all its files
			await client.DownloadDirectory(@"C:\website\logs\", @"/public_html/logs", FtpFolderSyncMode.Update);

			// download a folder and all its files, and delete extra files on disk
			await client.DownloadDirectory(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror);

			// delete a folder recursively
			await client.DeleteDirectory("/htdocs/extras/");

			// check if a file exists
			if (await client.FileExists("/htdocs/big2.txt")) { }

			// check if a folder exists
			if (await client.DirectoryExists("/htdocs/extras/")) { }

			// upload a file and retry 3 times before giving up
			client.Config.RetryAttempts = 3;
			await client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/big.txt", FtpRemoteExists.Overwrite, false, FtpVerify.Retry);

			// disconnect! good bye!
			await client.Disconnect();
		}

	}
}