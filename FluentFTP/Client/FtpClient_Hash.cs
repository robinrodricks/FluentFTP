using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Helpers;
using FluentFTP.Proxy;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Helpers.Hashing;
using HashAlgos = FluentFTP.Helpers.Hashing.HashAlgorithms;

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

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

		private void ValidateHashAlgorithm(FtpHashAlgorithm algorithm) {

			// if NO hashing algos or commands supported, throw here
			if (!HasFeature(FtpCapability.HASH) &&
				!HasFeature(FtpCapability.MD5) &&
				!HasFeature(FtpCapability.XMD5) &&
				!HasFeature(FtpCapability.MMD5) &&
				!HasFeature(FtpCapability.XSHA1) &&
				!HasFeature(FtpCapability.XSHA256) &&
				!HasFeature(FtpCapability.XSHA512) &&
				!HasFeature(FtpCapability.XCRC)) {
				throw new FtpHashUnsupportedException();
			}

			// only if the user has specified a certain hash algorithm
			var useFirst = (algorithm == FtpHashAlgorithm.NONE);
			if (!useFirst) {

				// first check if the HASH command supports the required algo
				if (HasFeature(FtpCapability.HASH) && HashAlgorithms.HasFlag(algorithm)) {

					// we are good

				}
				else {

					// second check if the special FTP command is supported based on the algo
					if (algorithm == FtpHashAlgorithm.MD5 && !HasFeature(FtpCapability.MD5) &&
						!HasFeature(FtpCapability.XMD5) && !HasFeature(FtpCapability.MMD5)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.MD5, "MD5, XMD5, MMD5");
					}
					if (algorithm == FtpHashAlgorithm.SHA1 && !HasFeature(FtpCapability.XSHA1)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.SHA1, "XSHA1");
					}
					if (algorithm == FtpHashAlgorithm.SHA256 && !HasFeature(FtpCapability.XSHA256)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.SHA256, "XSHA256");
					}
					if (algorithm == FtpHashAlgorithm.SHA512 && !HasFeature(FtpCapability.XSHA512)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.SHA512, "XSHA512");
					}
					if (algorithm == FtpHashAlgorithm.CRC && !HasFeature(FtpCapability.XCRC)) {
						throw new FtpHashUnsupportedException(FtpHashAlgorithm.CRC, "XCRC");
					}

					// we are good
				}
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

		private static string CleanHashResult(string path, string response) {
			response = response.RemovePrefix(path);
			response = response.RemovePrefix($@"""{path}""");
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
		/// Gets the currently selected hash algorithm for the HASH command.
		/// </summary>
		internal FtpHashAlgorithm GetHashAlgorithmUnused() {
			FtpReply reply;
			var type = FtpHashAlgorithm.NONE;

#if !CORE14
			lock (m_lock) {
#endif
				LogFunc(nameof(GetHashAlgorithmUnused));

				if ((reply = Execute("OPTS HASH")).Success) {
					try {
						type = HashAlgos.FromString(reply.Message);
					}
					catch (InvalidOperationException ex) {
						// Do nothing
					}
				}

#if !CORE14
			}
#endif

			return type;
		}

#if ASYNC
		/// <summary>
		/// Gets the currently selected hash algorithm for the HASH command asynchronously.
		/// </summary>
		internal async Task<FtpHashAlgorithm> GetHashAlgorithmUnusedAsync(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			var type = FtpHashAlgorithm.NONE;

			LogFunc(nameof(GetHashAlgorithmUnusedAsync));

			if ((reply = await ExecuteAsync("OPTS HASH", token)).Success) {
				try {
					type = HashAlgos.FromString(reply.Message);
				}
				catch (InvalidOperationException ex) {
					// Do nothing
				}
			}

			return type;
		}
#endif

		/// <summary>
		/// Sets the hash algorithm on the server to use for the HASH command. 
		/// </summary>
		internal void SetHashAlgorithmInternal(FtpHashAlgorithm algorithm) {
			FtpReply reply;

			// skip setting the hash algo if the server is already configured to it
			if (Status.LastHashAlgo == algorithm) {
				return;
			}

#if !CORE14
			lock (m_lock) {
#endif
				if ((HashAlgorithms & algorithm) != algorithm) {
					throw new NotImplementedException("The hash algorithm " + algorithm.ToString() + " was not advertised by the server.");
				}

				string algoName = HashAlgos.PrintToString(algorithm);

				if (!(reply = Execute("OPTS HASH " + algoName)).Success) {
					throw new FtpCommandException(reply);
				}

				// save the current hash algo so no need to repeat this command
				Status.LastHashAlgo = algorithm;

#if !CORE14
			}

#endif
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

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("HASH " + path)).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}
#endif

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

		#region FXP Hash Algorithm

		/// <summary>
		/// Get the first checksum algorithm mutually supported by both servers.
		/// </summary>
		private FtpHashAlgorithm GetFirstMutualChecksum(FtpClient destination) {

			// special handling for HASH command which is a meta-command supporting all hash types
			if (HasFeature(FtpCapability.HASH) && destination.HasFeature(FtpCapability.HASH)) {
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
					return FtpHashAlgorithm.MD5;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA1) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA1)) {
					return FtpHashAlgorithm.SHA1;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA256) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA256)) {
					return FtpHashAlgorithm.SHA256;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA512) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA512)) {
					return FtpHashAlgorithm.SHA512;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.CRC) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.CRC)) {
					return FtpHashAlgorithm.CRC;
				}
			}

			// handling for non-standard specific hashing commands
			if (HasFeature(FtpCapability.MD5) && destination.HasFeature(FtpCapability.MD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.XMD5) && destination.HasFeature(FtpCapability.XMD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.MMD5) && destination.HasFeature(FtpCapability.MMD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.XSHA1) && destination.HasFeature(FtpCapability.XSHA1)) {
				return FtpHashAlgorithm.SHA1;
			}
			if (HasFeature(FtpCapability.XSHA256) && destination.HasFeature(FtpCapability.XSHA256)) {
				return FtpHashAlgorithm.SHA256;
			}
			if (HasFeature(FtpCapability.XSHA512) && destination.HasFeature(FtpCapability.XSHA512)) {
				return FtpHashAlgorithm.SHA512;
			}
			if (HasFeature(FtpCapability.XCRC) && destination.HasFeature(FtpCapability.XCRC)) {
				return FtpHashAlgorithm.CRC;
			}
			return FtpHashAlgorithm.NONE;
		}

		#endregion

		#region Obsolete Commands

		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need. Or use CompareFile.", true)]
		public FtpHashAlgorithm GetHashAlgorithm() {
			return FtpHashAlgorithm.NONE;
		}

		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need. Or use CompareFile.", true)]
		public void SetHashAlgorithm(FtpHashAlgorithm algorithm) { }

		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need. Or use CompareFile.", true)]
		public FtpHash GetHash(string path) {
			return null;
		}

		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5. Or use CompareFile.", true)]
		public string GetMD5(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to CRC. Or use CompareFile.", true)]
		public string GetXCRC(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5. Or use CompareFile.", true)]
		public string GetXMD5(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA1. Or use CompareFile.", true)]
		public string GetXSHA1(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA256. Or use CompareFile.", true)]
		public string GetXSHA256(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA512. Or use CompareFile.", true)]
		public string GetXSHA512(string path) {
			return null;
		}


#if ASYNC
		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need. Or use CompareFile.", true)]
		public Task<FtpHashAlgorithm> GetHashAlgorithmAsync(CancellationToken token = default(CancellationToken)) {
			return Task.FromResult(FtpHashAlgorithm.NONE);
		}
		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need. Or use CompareFile.", true)]
		public Task SetHashAlgorithmAsync(FtpHashAlgorithm algorithm, CancellationToken token = default(CancellationToken)) {
#if NET45
			return Task.FromResult(true);
#else
			return Task.CompletedTask;
#endif
		}
		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need. Or use CompareFile.", true)]
		public Task<FtpHash> GetHashAsync(string path, CancellationToken token = default(CancellationToken)) {
			return Task.FromResult<FtpHash>(null);
		}

		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5. Or use CompareFile.", true)]
		public Task<string> GetMD5Async(string path, CancellationToken token = default(CancellationToken)) {
			return Task.FromResult<string>(null);
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to CRC. Or use CompareFile.", true)]
		public Task<string> GetXCRCAsync(string path, CancellationToken token = default(CancellationToken)) {
			return Task.FromResult<string>(null);
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5. Or use CompareFile.", true)]
		public Task<string> GetXMD5Async(string path, CancellationToken token = default(CancellationToken)) {
			return Task.FromResult<string>(null);
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA1. Or use CompareFile.", true)]
		public Task<string> GetXSHA1Async(string path, CancellationToken token = default(CancellationToken)) {
			return Task.FromResult<string>(null);
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA256. Or use CompareFile.", true)]
		public Task<string> GetXSHA256Async(string path, CancellationToken token = default(CancellationToken)) {
			return Task.FromResult<string>(null);
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA512. Or use CompareFile.", true)]
		public Task<string> GetXSHA512Async(string path, CancellationToken token = default(CancellationToken)) {
			return Task.FromResult<string>(null);
		}
#endif

		#endregion
	}
}