using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Creates a remote directory asynchronously
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>True if directory was created, false if it was skipped</returns>
		public async Task<bool> CreateDirectory(string path, bool force, CancellationToken token = default(CancellationToken)) {
			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			path = path.GetFtpPath();

			LogFunction(nameof(CreateDirectory), new object[] { path, force });

			FtpReply reply;

			// cannot create root or working directory
			if (path.IsFtpRootDirectory()) {
				return false;
			}

			// server-specific directory creation
			// ask the server handler to create a directory
			if (ServerHandler != null) {
				if (await ServerHandler.CreateDirectoryAsync(this, path, path, force, token)) {
					return true;
				}
			}

			path = path.TrimEnd('/');

			if (force && !await DirectoryExists(path.GetFtpDirectoryName(), token)) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Create non-existent parent directory: " + path.GetFtpDirectoryName());
				await CreateDirectory(path.GetFtpDirectoryName(), true, token);
			}

			// fix: improve performance by skipping the directory exists check
			/*else if (await DirectoryExistsAsync(path, token)) {
				return false;
			}*/

			LogWithPrefix(FtpTraceLevel.Verbose, "CreateDirectory " + path);

			if (!(reply = await Execute("MKD " + path, token)).Success) {

				// if the error indicates the directory already exists, its not an error
				if (reply.Code == "550") {
					return false;
				}
				if (reply.Code[0] == '5' && reply.Message.ContainsAnyCI(ServerStringModule.folderExists)) {
					return false;
				}

				throw new FtpCommandException(reply);
			}
			return true;
		}

		/// <summary>
		/// Creates a remote directory asynchronously. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task<bool> CreateDirectory(string path, CancellationToken token = default(CancellationToken)) {
			return CreateDirectory(path, true, token);
		}

	}
}
