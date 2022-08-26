using System;
using FluentFTP.Helpers;
using FluentFTP.Helpers.Hashing;
using HashAlgos = FluentFTP.Helpers.Hashing.HashAlgorithms;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {


		#region Checksum

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
		/// <param name="path">Full or relative path of the file to checksum</param>
		/// <param name="algorithm">Specify an algorithm that you prefer, or NONE to use the first available algorithm. If the preferred algorithm is not supported, a blank hash is returned.</param>
		/// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
		/// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
		/// the underlying calls.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public FtpHash GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE) {

			if (path == null) {
				throw new ArgumentException("Required argument is null", "path");
			}

			ValidateHashAlgorithm(algorithm);

			path = path.GetFtpPath();

			LogFunc(nameof(GetChecksum), new object[] { path });

			var useFirst = (algorithm == FtpHashAlgorithm.NONE);

			// if HASH is supported and the caller prefers an algorithm and that algorithm is supported
			if (HasFeature(FtpCapability.HASH) && !useFirst && HashAlgorithms.HasFlag(algorithm)) {

				// switch to that algorithm
				SetHashAlgorithmInternal(algorithm);

				// get the hash of the file using HASH Command
				return HashCommandInternal(path);

			}

			// if HASH is supported and the caller does not prefer any specific algorithm
			else if (HasFeature(FtpCapability.HASH) && useFirst) {

				// switch to the first preferred algorithm
				SetHashAlgorithmInternal(HashAlgos.FirstSupported(HashAlgorithms));

				// get the hash of the file using HASH Command
				return HashCommandInternal(path);
			}
			else {
				var result = new FtpHash();

				// execute the first available algorithm, or the preferred algorithm if specified

				if (HasFeature(FtpCapability.MD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = GetHashInternal(path, "MD5");
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = GetHashInternal(path, "XMD5");
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.MMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = GetHashInternal(path, "MMD5");
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XSHA1) && (useFirst || algorithm == FtpHashAlgorithm.SHA1)) {
					result.Value = GetHashInternal(path, "XSHA1");
					result.Algorithm = FtpHashAlgorithm.SHA1;
				}
				else if (HasFeature(FtpCapability.XSHA256) && (useFirst || algorithm == FtpHashAlgorithm.SHA256)) {
					result.Value = GetHashInternal(path, "XSHA256");
					result.Algorithm = FtpHashAlgorithm.SHA256;
				}
				else if (HasFeature(FtpCapability.XSHA512) && (useFirst || algorithm == FtpHashAlgorithm.SHA512)) {
					result.Value = GetHashInternal(path, "XSHA512");
					result.Algorithm = FtpHashAlgorithm.SHA512;
				}
				else if (HasFeature(FtpCapability.XCRC) && (useFirst || algorithm == FtpHashAlgorithm.CRC)) {
					result.Value = GetHashInternal(path, "XCRC");
					result.Algorithm = FtpHashAlgorithm.CRC;
				}

				return result;
			}
		}

#if ASYNC
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
		/// <param name="path">Full or relative path of the file to checksum</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="algorithm">Specify an algorithm that you prefer, or NONE to use the first available algorithm. If the preferred algorithm is not supported, a blank hash is returned.</param>
		/// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
		/// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
		/// the underlying calls.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<FtpHash> GetChecksumAsync(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE, CancellationToken token = default(CancellationToken)) {

			if (path == null) {
				throw new ArgumentException("Required argument is null", "path");
			}

			ValidateHashAlgorithm(algorithm);

			path = path.GetFtpPath();

			LogFunc(nameof(GetChecksumAsync), new object[] { path });

			var useFirst = (algorithm == FtpHashAlgorithm.NONE);

			// if HASH is supported and the caller prefers an algorithm and that algorithm is supported
			if (HasFeature(FtpCapability.HASH) && !useFirst && HashAlgorithms.HasFlag(algorithm)) {

				// switch to that algorithm
				await SetHashAlgorithmInternalAsync(algorithm, token);

				// get the hash of the file using HASH Command
				return await HashCommandInternalAsync(path, token);

			}

			// if HASH is supported and the caller does not prefer any specific algorithm
			else if (HasFeature(FtpCapability.HASH) && useFirst) {

				// switch to the first preferred algorithm
				await SetHashAlgorithmInternalAsync(HashAlgos.FirstSupported(HashAlgorithms), token);

				// get the hash of the file using HASH Command
				return await HashCommandInternalAsync(path, token);
			}

			else {
				var result = new FtpHash();

				// execute the first available algorithm, or the preferred algorithm if specified

				if (HasFeature(FtpCapability.MD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetHashInternalAsync(path, "MD5", token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetHashInternalAsync(path, "XMD5", token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.MMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetHashInternalAsync(path, "MMD5", token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XSHA1) && (useFirst || algorithm == FtpHashAlgorithm.SHA1)) {
					result.Value = await GetHashInternalAsync(path, "XSHA1", token);
					result.Algorithm = FtpHashAlgorithm.SHA1;
				}
				else if (HasFeature(FtpCapability.XSHA256) && (useFirst || algorithm == FtpHashAlgorithm.SHA256)) {
					result.Value = await GetHashInternalAsync(path, "XSHA256", token);
					result.Algorithm = FtpHashAlgorithm.SHA256;
				}
				else if (HasFeature(FtpCapability.XSHA512) && (useFirst || algorithm == FtpHashAlgorithm.SHA512)) {
					result.Value = await GetHashInternalAsync(path, "XSHA512", token);
					result.Algorithm = FtpHashAlgorithm.SHA512;
				}
				else if (HasFeature(FtpCapability.XCRC) && (useFirst || algorithm == FtpHashAlgorithm.CRC)) {
					result.Value = await GetHashInternalAsync(path, "XCRC", token);
					result.Algorithm = FtpHashAlgorithm.CRC;
				}

				return result;
			}
		}
#endif

		#endregion

		#region MD5, SHA1, SHA256, SHA512 Commands

		/// <summary>
		/// Gets the hash of the specified file using the given command.
		/// </summary>
		internal string GetHashInternal(string path, string command) {
			FtpReply reply;
			string response;

			if (!(reply = Execute(command + " " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			response = reply.Message;
			response = CleanHashResult(path, response);
			return response;
		}


#if ASYNC
		/// <summary>
		/// Gets the hash of the specified file using the given command.
		/// </summary>
		internal async Task<string> GetHashInternalAsync(string path, string command, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			string response;

			if (!(reply = await ExecuteAsync(command + " " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			response = reply.Message;
			response = CleanHashResult(path, response);
			return response;
		}

#endif

		#endregion

		#region HASH Command

		/// <summary>
		/// Sets the hash algorithm on the server to use for the HASH command. 
		/// </summary>
		internal void SetHashAlgorithmInternal(FtpHashAlgorithm algorithm) {
			FtpReply reply;

			// skip setting the hash algo if the server is already configured to it
			if (Status.LastHashAlgo == algorithm) {
				return;
			}

			lock (m_lock) {
				if ((HashAlgorithms & algorithm) != algorithm) {
					throw new NotImplementedException("The hash algorithm " + algorithm.ToString() + " was not advertised by the server.");
				}

				string algoName = HashAlgos.PrintToString(algorithm);

				if (!(reply = Execute("OPTS HASH " + algoName)).Success) {
					throw new FtpCommandException(reply);
				}

				// save the current hash algo so no need to repeat this command
				Status.LastHashAlgo = algorithm;

			}
		}

#if ASYNC
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

			if (!(reply = await ExecuteAsync("OPTS HASH " + algoName, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// save the current hash algo so no need to repeat this command
			Status.LastHashAlgo = algorithm;

		}
#endif

		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm.
		/// </summary>
		internal FtpHash HashCommandInternal(string path) {
			FtpReply reply;

			lock (m_lock) {
				if (!(reply = Execute("HASH " + path)).Success) {
					throw new FtpCommandException(reply);
				}

			}

			// parse hash from the server reply
			return HashParser.Parse(reply.Message);
		}

#if ASYNC
		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm.
		/// </summary>
		public async Task<FtpHash> HashCommandInternalAsync(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			if (!(reply = await ExecuteAsync("HASH " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// parse hash from the server reply
			return HashParser.Parse(reply.Message);
		}
#endif

		#endregion

	}
}
