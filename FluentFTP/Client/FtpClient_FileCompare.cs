using System;
using FluentFTP.Streams;
using FluentFTP.Rules;
using FluentFTP.Helpers;
using System.IO;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
#endif
#if (CORE || NET45)
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

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
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(CompareFile), new object[] { localPath, remotePath, options });


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

#if ASYNC
		/// <summary>
		/// Compare the specified local file with the remote file on the FTP server using various kinds of quick equality checks.
		/// In Auto mode, the file size and checksum are compared.
		/// Comparing the checksum of a file is a quick way to check if the contents of the files are exactly equal without downloading a copy of the file.
		/// You can use the option flags to compare any combination of: file size, checksum, date modified.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="options">Types of equality checks to perform. Use Auto to compare file size and checksum.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns></returns>
		public async Task<FtpCompareResult> CompareFileAsync(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto,
			CancellationToken token = default(CancellationToken)) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(CompareFileAsync), new object[] { localPath, remotePath, options });


			// ensure both files exists
			if (!File.Exists(localPath)) {
				return FtpCompareResult.FileNotExisting;
			}
			if (!await FileExistsAsync(remotePath, token)) {
				return FtpCompareResult.FileNotExisting;
			}

			// if file size check enabled
			if (options == FtpCompareOption.Auto || options.HasFlag(FtpCompareOption.Size)) {

				// check file size
				var localSize = await FtpFileStream.GetFileSizeAsync(localPath, false, token);
				var remoteSize = await GetFileSizeAsync(remotePath, -1, token);
				if (localSize != remoteSize) {
					return FtpCompareResult.NotEqual;
				}

			}

			// if date check enabled
			if (options.HasFlag(FtpCompareOption.DateModified)) {

				// check file size
				var localDate = await FtpFileStream.GetFileDateModifiedUtcAsync(localPath, token);
				var remoteDate = await GetModifiedTimeAsync(remotePath, token);
				if (!localDate.Equals(remoteDate)) {
					return FtpCompareResult.NotEqual;
				}

			}

			// if checksum check enabled
			if (options == FtpCompareOption.Auto || options.HasFlag(FtpCompareOption.Checksum)) {

				// check file checksum
				if (SupportsChecksum()) {
					var hash = await GetChecksumAsync(remotePath, FtpHashAlgorithm.NONE, token);
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

#endif


	}
}