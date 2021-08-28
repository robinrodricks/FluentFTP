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

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

		#region Checksum

		/// <summary>
		/// Retrieves a checksum of the given file using the specified checksum algorithum, or using the first available algorithm that the server supports.
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithmInternal"/>
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

			path = path.GetFtpPath();

			LogFunc(nameof(GetChecksum), new object[] { path });

			// if HASH is supported and the caller prefers an algorithm and that algorithm is supported
			var useFirst = algorithm == FtpHashAlgorithm.NONE;
			if (HasFeature(FtpCapability.HASH) && !useFirst && HashAlgorithms.HasFlag(algorithm)) {

				// switch to that algorithm
				SetHashAlgorithmInternal(algorithm);

				// get the hash of the file
				return HashCommandInternal(path);

			}

			// if HASH is supported and the caller does not prefer any specific algorithm
			else if (HasFeature(FtpCapability.HASH) && useFirst) {
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
				/*else if (HasFeature(FtpCapability.MMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = GetHashInternal(path, "MD5");
					result.Algorithm = FtpHashAlgorithm.MD5;
				}*/
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
		/// Retrieves a checksum of the given file using the specified checksum algorithum, or using the first available algorithm that the server supports.
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithmInternal"/>
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

			path = path.GetFtpPath();

			LogFunc(nameof(GetChecksumAsync), new object[] { path });

			// if HASH is supported and the caller prefers an algorithm and that algorithm is supported
			var useFirst = algorithm == FtpHashAlgorithm.NONE;
			if (HasFeature(FtpCapability.HASH) && !useFirst && HashAlgorithms.HasFlag(algorithm)) {

				// switch to that algorithm
				await SetHashAlgorithmInternalAsync(algorithm, token);

				// get the hash of the file
				return await HashCommandInternalAsync(path, token);

			}

			// if HASH is supported and the caller does not prefer any specific algorithm
			else if (HasFeature(FtpCapability.HASH) && useFirst) {
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
				/*else if (HasFeature(FtpCapability.MMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetHashInternalAsync(path, "MD5", token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}*/
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
		/// Gets the hash of the specified file using the given comomand.
		/// This is a non-standard extension to the protocol and may or may not work.
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
		/// Gets the MD5 hash of the specified file using MD5 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
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
		/// <remarks>
		///  This feature is experimental. See this link for details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.NONE"/> if there was a problem.</returns>
		internal FtpHashAlgorithm GetHashAlgorithmUnused() {
			FtpReply reply;
			var type = FtpHashAlgorithm.NONE;

#if !CORE14
			lock (m_lock) {
#endif
				LogFunc(nameof(GetHashAlgorithmUnused));

				if ((reply = Execute("OPTS HASH")).Success) {
					try {
						type = Helpers.Hashing.HashAlgorithms.FromString(reply.Message);
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
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.NONE"/> if there was a problem.</returns>
		internal async Task<FtpHashAlgorithm> GetHashAlgorithmUnusedAsync(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			var type = FtpHashAlgorithm.NONE;

			LogFunc(nameof(GetHashAlgorithmUnusedAsync));

			if ((reply = await ExecuteAsync("OPTS HASH", token)).Success) {
				try {
					type = Helpers.Hashing.HashAlgorithms.FromString(reply.Message);
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
		/// <remarks>
		/// If you specify an algorithm not listed in <see cref="FtpClient.HashAlgorithms"/>
		/// a <see cref="NotImplementedException"/> will be thrown
		/// so be sure to query that list of Flags before
		/// selecting a hash algorithm. Support for the
		/// HASH command is experimental. Please see
		/// the following link for more details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="algorithm">Hash Algorithm</param>
		/// <exception cref="System.NotImplementedException">Thrown if the selected algorithm is not available on the server</exception>
		internal void SetHashAlgorithmInternal(FtpHashAlgorithm algorithm) {
			FtpReply reply;

			// skip setting the hash algo if the server is already configured to it
			if (_LastHashAlgo == algorithm) {
				return;
			}

#if !CORE14
			lock (m_lock) {
#endif
				if ((HashAlgorithms & algorithm) != algorithm) {
					throw new NotImplementedException("The hash algorithm " + algorithm.ToString() + " was not advertised by the server.");
				}

				string algoName = Helpers.Hashing.HashAlgorithms.ToString(algorithm);

				if (!(reply = Execute("OPTS HASH " + algoName)).Success) {
					throw new FtpCommandException(reply);
				}

				// save the current hash algo so no need to repeat this command
				_LastHashAlgo = algorithm;

#if !CORE14
			}

#endif
		}

#if ASYNC
		/// <summary>
		/// Sets the hash algorithm on the server to be used with the HASH command asynchronously.
		/// </summary>
		/// <param name="algorithm">Hash algorithm to use</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <exception cref="System.NotImplementedException">Thrown if the selected algorithm is not available on the server</exception>
		internal async Task SetHashAlgorithmInternalAsync(FtpHashAlgorithm algorithm, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			// skip setting the hash algo if the server is already configured to it
			if (_LastHashAlgo == algorithm) {
				return;
			}

			if ((HashAlgorithms & algorithm) != algorithm) {
				throw new NotImplementedException("The hash algorithm " + algorithm.ToString() + " was not advertised by the server.");
			}

			string algoName = Helpers.Hashing.HashAlgorithms.ToString(algorithm);

			if (!(reply = await ExecuteAsync("OPTS HASH " + algoName, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// save the current hash algo so no need to repeat this command
			_LastHashAlgo = algorithm;

		}
#endif

		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm, or null if hash cannot be parsed.
		/// </summary>
		/// <param name="path">Full or relative path of the object to compute the hash for.</param>
		/// <returns>The hash of the file.</returns>
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
		/// Gets the hash of an object on the server using the currently selected hash algorithm, or null if hash cannot be parsed.
		/// </summary>
		/// <remarks>
		/// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
		/// property. You should confirm that it's not equal
		/// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
		/// otherwise the server trigger a <see cref="FtpCommandException"/>
		/// due to a lack of support for the HASH command. You can
		/// set the algorithm using the <see cref="SetHashAlgorithmInternal"/> method and
		/// you can query the server for the current hash algorithm
		/// using the <see cref="GetHashAlgorithm"/> method.
		/// </remarks>
		/// <param name="path">The file you want the server to compute the hash for</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <exception cref="FtpCommandException">
		/// Thrown if the <see cref="HashAlgorithms"/> property is <see cref="FtpHashAlgorithm.NONE"/>, 
		/// the remote path does not exist, or the command cannot be executed.
		/// </exception>
		/// <exception cref="ArgumentException">Path argument is null</exception>
		/// <exception cref="NotImplementedException">Thrown when an unknown hash algorithm type is returned by the server</exception>
		/// <returns>The hash of the file.</returns>
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

		#region Obsolete Commands

		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need.", true)]
		public FtpHashAlgorithm GetHashAlgorithm() {
			return FtpHashAlgorithm.NONE;
		}

		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need.", true)]
		public void SetHashAlgorithm(FtpHashAlgorithm algorithm) { }

		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need.", true)]
		public FtpHash GetHash(string path) {
			return null;
		}

		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5.", true)]
		public string GetMD5(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to CRC.", true)]
		public string GetXCRC(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5.", true)]
		public string GetXMD5(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA1.", true)]
		public string GetXSHA1(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA256.", true)]
		public string GetXSHA256(string path) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA512.", true)]
		public string GetXSHA512(string path) {
			return null;
		}


#if ASYNC
		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need.", true)]
		public async Task<FtpHashAlgorithm> GetHashAlgorithmAsync(CancellationToken token = default(CancellationToken)) {
			return FtpHashAlgorithm.NONE;
		}
		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need.", true)]
		public async Task SetHashAlgorithmAsync(FtpHashAlgorithm algorithm, CancellationToken token = default(CancellationToken)) {
		}
		[ObsoleteAttribute("Use GetChecksum instead and pass the algorithm type that you need.", true)]
		public async Task<FtpHash> GetHashAsync(string path, CancellationToken token = default(CancellationToken)) {
			return null;
		}

		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5.", true)]
		public async Task<string> GetMD5Async(string path, CancellationToken token = default(CancellationToken)) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to CRC.", true)]
		public async Task<string> GetXCRCAsync(string path, CancellationToken token = default(CancellationToken)) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to MD5.", true)]
		public async Task<string> GetXMD5Async(string path, CancellationToken token = default(CancellationToken)) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA1.", true)]
		public async Task<string> GetXSHA1Async(string path, CancellationToken token = default(CancellationToken)) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA256.", true)]
		public async Task<string> GetXSHA256Async(string path, CancellationToken token = default(CancellationToken)) {
			return null;
		}
		[ObsoleteAttribute("Use GetChecksum instead and set the algorithm to SHA512.", true)]
		public async Task<string> GetXSHA512Async(string path, CancellationToken token = default(CancellationToken)) {
			return null;
		}
#endif

		#endregion
	}
}