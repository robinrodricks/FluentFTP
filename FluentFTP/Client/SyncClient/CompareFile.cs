using System;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Compare the specified local file with the remote file on the FTP server using various kinds of quick equality checks.
		/// In Auto mode, the file size and checksum are compared.
		/// Comparing the checksum of a file is a quick way to check if the contents of the files are exactly equal without downloading a copy of the file.
		/// You can use the option flags to compare any combination of: file size, checksum, date modified.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="options">Types of equality checks to perform. Use Auto to compare file size and checksum.</param>
		/// <returns></returns>
		public FtpCompareResult CompareFile(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localPath));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(CompareFile), new object[] { localPath, remotePath, options });


			// ensure both files exists
			if (!File.Exists(localPath)) {
				return FtpCompareResult.FileNotExisting;
			}
			if (!FileExists(remotePath)) {
				return FtpCompareResult.FileNotExisting;
			}

			// if file size check enabled
			if (options == FtpCompareOption.Auto || options.HasFlag(FtpCompareOption.Size)) {

				// check file size
				var localSize = FtpFileStream.GetFileSize(localPath, false);
				var remoteSize = GetFileSize(remotePath);
				if (localSize != remoteSize) {
					return FtpCompareResult.NotEqual;
				}

			}

			// if date check enabled
			if (options.HasFlag(FtpCompareOption.DateModified)) {

				// check file size
				var localDate = FtpFileStream.GetFileDateModifiedUtc(localPath);
				var remoteDate = GetModifiedTime(remotePath);
				if (!localDate.Equals(remoteDate)) {
					return FtpCompareResult.NotEqual;
				}

			}

			// if checksum check enabled
			if (options == FtpCompareOption.Auto || options.HasFlag(FtpCompareOption.Checksum)) {

				// check file checksum
				if (SupportsChecksum()) {
					var hash = GetChecksum(remotePath);
					if (hash.IsValid) {
						if (!hash.Verify(localPath)) {
							return FtpCompareResult.NotEqual;
						}
					}
					else {
						return FtpCompareResult.ChecksumNotSupported;
					}
				}
				else {
					return FtpCompareResult.ChecksumNotSupported;
				}

			}

			// all checks passed!
			return FtpCompareResult.Equal;
		}

	}
}
