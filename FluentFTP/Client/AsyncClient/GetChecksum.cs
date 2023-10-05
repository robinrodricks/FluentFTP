using System;
using FluentFTP.Helpers;
using FluentFTP.Helpers.Hashing;
using HashAlgos = FluentFTP.Helpers.Hashing.HashAlgorithms;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;
using System.IO;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Retrieves a checksum of the given file using the specified checksum algorithm, or using the first available algorithm that the server supports.
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command using the first supported algorithm.
		/// 2. MD5 / XMD5 / MMD5 commands
		/// 3. XSHA1 command
		/// 4. XSHA256 command
		/// 5. XSHA512 command
		/// 6. XCRC command
		/// </remarks>
		/// <param name="remotePath">Full or relative path of the file to checksum</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="algorithm">Specify an algorithm that you prefer, or NONE to use the first available algorithm. If the preferred algorithm is not supported, a blank hash is returned.</param>
		/// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
		/// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
		/// the underlying calls.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<FtpHash> GetChecksum(string remotePath, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE, CancellationToken token = default(CancellationToken)) {

			if (remotePath == null) {
				throw new ArgumentException("Required argument is null", nameof(remotePath));
			}

			ValidateChecksumAlgorithm(algorithm);

			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(GetChecksum), new object[] { remotePath });

			var useFirst = (algorithm == FtpHashAlgorithm.NONE);

			// if HASH is supported and the caller prefers an algorithm and that algorithm is supported
			if (HasFeature(FtpCapability.HASH) && !useFirst && HashAlgorithms.HasFlag(algorithm)) {

				// switch to that algorithm
				await SetHashAlgorithmInternalAsync(algorithm, token);

				// get the hash of the file using HASH Command
				return await HashCommandInternalAsync(remotePath, token);

			}

			// if HASH is supported and the caller does not prefer any specific algorithm
			else if (HasFeature(FtpCapability.HASH) && useFirst) {

				// switch to the first preferred algorithm
				await SetHashAlgorithmInternalAsync(HashAlgos.FirstSupported(HashAlgorithms), token);

				// get the hash of the file using HASH Command
				return await HashCommandInternalAsync(remotePath, token);
			}

			else {
				var result = new FtpHash();

				// execute the first available algorithm, or the preferred algorithm if specified

				if (HasFeature(FtpCapability.MD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetHashInternalAsync(remotePath, "MD5", token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetHashInternalAsync(remotePath, "XMD5", token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.MMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetHashInternalAsync(remotePath, "MMD5", token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XSHA1) && (useFirst || algorithm == FtpHashAlgorithm.SHA1)) {
					result.Value = await GetHashInternalAsync(remotePath, "XSHA1", token);
					result.Algorithm = FtpHashAlgorithm.SHA1;
				}
				else if (HasFeature(FtpCapability.XSHA256) && (useFirst || algorithm == FtpHashAlgorithm.SHA256)) {
					result.Value = await GetHashInternalAsync(remotePath, "XSHA256", token);
					result.Algorithm = FtpHashAlgorithm.SHA256;
				}
				else if (HasFeature(FtpCapability.XSHA512) && (useFirst || algorithm == FtpHashAlgorithm.SHA512)) {
					result.Value = await GetHashInternalAsync(remotePath, "XSHA512", token);
					result.Algorithm = FtpHashAlgorithm.SHA512;
				}
				else if (HasFeature(FtpCapability.XCRC) && (useFirst || algorithm == FtpHashAlgorithm.CRC)) {
					result.Value = await GetHashInternalAsync(remotePath, "XCRC", token);
					result.Algorithm = FtpHashAlgorithm.CRC;
				}

				return result;
			}
		}

		/// <summary>
		/// Sets the hash algorithm on the server to be used with the HASH command asynchronously.
		/// </summary>
		internal async Task SetHashAlgorithmInternalAsync(FtpHashAlgorithm algorithm, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			// skip setting the hash algo if the server is already configured to it
			if (Status.LastHashAlgo == algorithm) {
				return;
			}

			if ((HashAlgorithms & algorithm) != algorithm) {
				throw new NotImplementedException("The hash algorithm " + algorithm.ToString() + " was not advertised by the server.");
			}

			string algoName = HashAlgos.PrintToString(algorithm);

			if (!(reply = await Execute("OPTS HASH " + algoName, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// save the current hash algo so no need to repeat this command
			Status.LastHashAlgo = algorithm;

		}

		/// <summary>
		/// Gets the hash of the specified file using the given command.
		/// </summary>
		internal async Task<string> GetHashInternalAsync(string remotePath, string command, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			string response;

			string remoteDirectory;
			string pwdSave = string.Empty;

			var autoNav = Config.ShouldAutoNavigate(remotePath);
			var autoRestore = Config.ShouldAutoRestore(remotePath);

			if (autoNav) {
				var temp = await GetAbsolutePathAsync(remotePath, token);
				remoteDirectory = Path.GetDirectoryName(temp).Replace("\\", "/");
				remotePath = Path.GetFileName(remotePath);

				pwdSave = await GetWorkingDirectory(token);
				if (pwdSave != remoteDirectory) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate to: \"" + remoteDirectory + "\"");
					await SetWorkingDirectory(remoteDirectory, token);
				}
			}

			if (!(reply = await Execute(command + " " + remotePath, token)).Success) {
				throw new FtpCommandException(reply);
			}

			if (autoRestore) {
				if (pwdSave != await GetWorkingDirectory(token)) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
					await SetWorkingDirectory(pwdSave, token);
				}
			}

			response = reply.Message;
			response = CleanHashResult(remotePath, response);

			return response;
		}

		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm.
		/// </summary>
		protected async Task<FtpHash> HashCommandInternalAsync(string remotePath, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			string remoteDirectory;
			string pwdSave = string.Empty;

			var autoNav = Config.ShouldAutoNavigate(remotePath);
			var autoRestore = Config.ShouldAutoRestore(remotePath);

			if (autoNav) {
				var temp = await GetAbsolutePathAsync(remotePath, token);
				remoteDirectory = Path.GetDirectoryName(temp).Replace("\\", "/");
				remotePath = Path.GetFileName(remotePath);

				pwdSave = await GetWorkingDirectory(token);
				if (pwdSave != remoteDirectory) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate to: \"" + remoteDirectory + "\"");
					await SetWorkingDirectory(remoteDirectory, token);
				}
			}

			if (!(reply = await Execute("HASH " + remotePath, token)).Success) {
				throw new FtpCommandException(reply);
			}

			if (autoRestore) {
				if (pwdSave != await GetWorkingDirectory(token)) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
					await SetWorkingDirectory(pwdSave, token);
				}
			}

			// parse hash from the server reply
			return HashParser.Parse(reply.Message);
		}

	}
}
