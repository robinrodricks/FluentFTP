using System;
using System.Collections.Generic;
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Servers {

	/// <summary>
	/// The base class used for all FTP server specific support.
	/// You may extend this class to implement support for custom FTP servers.
	/// </summary>
	public abstract class FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public virtual FtpServer ToEnum() {
			return FtpServer.Unknown;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public virtual bool DetectByWelcome(string message) {
			return false;
		}

		/// <summary>
		/// Return true if your server is detected by the given SYST response message.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public virtual bool DetectBySyst(string message) {
			return false;
		}

		/// <summary>
		/// Detect if your FTP server supports the recursive LIST command (LIST -R).
		/// If you know for sure that this is supported, return true here.
		/// </summary>
		public virtual bool RecursiveList() {
			return false;
		}

		/// <summary>
		/// Return your FTP server's default capabilities.
		/// Used if your server does not broadcast its capabilities using the FEAT command.
		/// </summary>
		public virtual string[] DefaultCapabilities() {
			return null;
		}

		/// <summary>
		/// Return the default file listing parser to be used with your FTP server.
		/// </summary>
		public virtual FtpParser GetParser() {
			return FtpParser.Unix;
		}

		/// <summary>
		/// Perform server-specific delete directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual bool DeleteDirectory(FtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options) {
			return false;
		}

#if ASYNC
		/// <summary>
		/// Perform async server-specific delete directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual Task<bool> DeleteDirectoryAsync(FtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options, CancellationToken token) {
			return Task.FromResult(false);
		}
#endif

		/// <summary>
		/// Perform server-specific create directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual bool CreateDirectory(FtpClient client, string path, string ftppath, bool force) {
			return false;
		}

#if ASYNC
		/// <summary>
		/// Perform async server-specific create directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual Task<bool> CreateDirectoryAsync(FtpClient client, string path, string ftppath, bool force, CancellationToken token) {
			return Task.FromResult(false);
		}
#endif

		/// <summary>
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual void AfterConnected(FtpClient client) {

		}

#if ASYNC
		/// <summary>
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual Task AfterConnectedAsync(FtpClient client, CancellationToken token) {
#if NET45
			return Task.FromResult(true);
#else
			return Task.CompletedTask;
#endif
		}
#endif

		/// <summary>
		/// Return true if your server requires custom handling of file size.
		/// </summary>
		public virtual bool IsCustomFileSize() {
			return false;
		}

		/// <summary>
		/// Perform server-specific file size fetching commands here.
		/// Return the file size in bytes.
		/// </summary>
		public virtual long GetFileSize(FtpClient client, string path) {
			return 0;
		}

#if ASYNC
		/// <summary>
		/// Perform server-specific file size fetching commands here.
		/// Return the file size in bytes.
		/// </summary>
		public virtual Task<long> GetFileSizeAsync(FtpClient client, string path, CancellationToken token) {
			return Task.FromResult(0L);
		}
#endif

		/// <summary>
		/// Check if the given path is a root directory on your FTP server.
		/// If you are unsure, return false.
		/// </summary>
		public virtual bool IsRoot(FtpClient client, string path) {
			return false;
		}

		/// <summary>
		/// Skip reporting a parser error
		/// </summary>
		public virtual bool SkipParserErrorReport()	{
			return false;
		}

		/// <summary>
		/// Always read to end of stream on a download
		/// If you are unsure, return false.
		/// </summary>
		public virtual bool AlwaysReadToEnd(string remotePath) {
			return false;
		}

		/// <summary>
		/// Return true if the path is an absolute path according to your server's convention.
		/// </summary>
		public virtual bool IsAbsolutePath(string path) {
			return false;
		}

		/// <summary>
		/// Return true if your server requires custom handling of absolute paths.
		/// </summary>
		public virtual bool IsCustomGetAbsolutePath() {
			return false;
		}

		/// <summary>
		/// Perform server-specific path modification here.
		/// Return the absolute path.
		/// </summary>
		public virtual string GetAbsolutePath(FtpClient client, string path) {
			return path;
		}

#if ASYNC
		/// <summary>
		/// Perform server-specific path modification here.
		/// Return the absolute path.
		/// </summary>
		public virtual Task<string> GetAbsolutePathAsync(FtpClient client, string path, CancellationToken token) {
			return Task.FromResult(path);
		}
#endif

		/// <summary>
		/// Return true if your server requires custom handling of absolute dir.
		/// </summary>
		public virtual bool IsCustomGetAbsoluteDir() {
			return false;
		}

		/// <summary>
		/// Perform server-specific path modification here.
		/// Return the absolute dir.
		/// </summary>
		public virtual string GetAbsoluteDir(FtpClient client, string path)	{
			return null;
		}

#if ASYNC
		/// <summary>
		/// Perform server-specific path modification here.
		/// Return the absolute path.
		/// </summary>
		public virtual Task<string> GetAbsoluteDirAsync(FtpClient client, string path, CancellationToken token) { 
			return Task.FromResult((string)null);
		}
#endif

		/// <summary>
		/// Return true if your server requires custom handling of path and filename concatenation.
		/// </summary>
		public virtual bool IsCustomGetAbsoluteFilePath() {
			return false;
		}

		/// <summary>
		/// Perform server-specific path modification here.
		/// Return concatenation of path and filename
		/// </summary>
		public virtual string GetAbsoluteFilePath(FtpClient client, string path, string fileName) {
			return !path.EndsWith("/") ? path + "/" + fileName : path + fileName;
		}

#if ASYNC
		/// <summary>
		/// Perform server-specific path modification here.
		/// Return concatenation of path and filename
		/// </summary>
		public virtual Task<string> GetAbsoluteFilePathAsync(FtpClient client, string path, string fileName, CancellationToken token) {
			return Task.FromResult(!path.EndsWith("/") ? path + "/" + fileName : path + fileName);
		}
#endif

		/// <summary>
		/// Return true if your server requires custom handling to handle listing analysis.
		/// </summary>
		public virtual bool IsCustomCalculateFullFtpPath() {
			return false;
		}

		/// <summary>
		/// Get the full path of a given FTP Listing entry
		/// Return null indicates custom code decided not to handle this
		/// </summary>
		public virtual bool? CalculateFullFtpPath(FtpClient client, string path, FtpListItem item) {
			return null;
		}

		/// <summary>
		/// Disable SIZE command even if server says it is supported
		/// </summary>
		public virtual bool DontUseSizeEvenIfCapable(string path) {
			return false;
		}

		/// <summary>
		/// Disable MDTM command even if server says it is supported
		/// </summary>
		public virtual bool DontUseMdtmEvenIfCapable(string path) {
			return false;
		}

		/// <summary>
		/// Return true if your server requires custom handling to check file existence.
		/// </summary>
		public virtual bool IsCustomFileExists() {
			return false;
		}

		/// <summary>
		/// Check for existence of a file
		/// Return null indicates custom code decided not to handle this
		/// </summary>
		public virtual bool? FileExists(FtpClient client, string path) {
			return null;
		}

#if ASYNC
		/// <summary>
		/// Check for existence of a file
		/// Return null indicates custom code decided not to handle this
		/// </summary>
		public virtual Task<bool?> FileExistsAsync(FtpClient client, string path, CancellationToken token) {
			return Task.FromResult((bool?)null);
		}
#endif
	}
}
