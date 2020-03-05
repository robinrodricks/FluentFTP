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

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		#region File Hashing - HASH

		/// <summary>
		/// Gets the currently selected hash algorithm for the HASH command.
		/// </summary>
		/// <remarks>
		///  This feature is experimental. See this link for details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.NONE"/> if there was a problem.</returns>
		/// <example><code source="..\Examples\GetHashAlgorithm.cs" lang="cs" /></example>
		public FtpHashAlgorithm GetHashAlgorithm() {
			FtpReply reply;
			var type = FtpHashAlgorithm.NONE;

#if !CORE14
			lock (m_lock) {
#endif
				if ((reply = Execute("OPTS HASH")).Success) {
					try {
						type = FtpHashAlgorithms.FromString(reply.Message);
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

#if !ASYNC
		private delegate FtpHashAlgorithm AsyncGetHashAlgorithm();

		/// <summary>
		/// Begins an asynchronous operation to get the currently selected hash algorithm for the HASH command.
		/// </summary>
		/// <remarks>
		///  This feature is experimental. See this link for details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetHashAlgorithm(AsyncCallback callback, object state) {
			AsyncGetHashAlgorithm func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = GetHashAlgorithm).BeginInvoke(callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetHashAlgorithm"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetHashAlgorithm"/></param>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.NONE"/> if there was a problem.</returns>
		public FtpHashAlgorithm EndGetHashAlgorithm(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetHashAlgorithm>(ar).EndInvoke(ar);
		}
#endif

#if ASYNC
		/// <summary>
		/// Gets the currently selected hash algorithm for the HASH command asynchronously.
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.NONE"/> if there was a problem.</returns>
		public async Task<FtpHashAlgorithm> GetHashAlgorithmAsync(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			var type = FtpHashAlgorithm.NONE;

			if ((reply = await ExecuteAsync("OPTS HASH", token)).Success) {
				try {
					type = FtpHashAlgorithms.FromString(reply.Message);
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
		/// <param name="type">Hash Algorithm</param>
		/// <exception cref="System.NotImplementedException">Thrown if the selected algorithm is not available on the server</exception>
		/// <example><code source="..\Examples\SetHashAlgorithm.cs" lang="cs" /></example>
		public void SetHashAlgorithm(FtpHashAlgorithm type) {
			FtpReply reply;
			string algorithm;

#if !CORE14
			lock (m_lock) {
#endif
				if ((HashAlgorithms & type) != type) {
					throw new NotImplementedException("The hash algorithm " + type.ToString() + " was not advertised by the server.");
				}

				algorithm = FtpHashAlgorithms.ToString(type);

				if (!(reply = Execute("OPTS HASH " + algorithm)).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}

#endif
		}

#if !ASYNC
		private delegate void AsyncSetHashAlgorithm(FtpHashAlgorithm type);

		/// <summary>
		/// Begins an asynchronous operation to set the hash algorithm on the server to use for the HASH command. 
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
		/// <param name="type">Hash algorithm to use</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginSetHashAlgorithm(FtpHashAlgorithm type, AsyncCallback callback, object state) {
			AsyncSetHashAlgorithm func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = SetHashAlgorithm).BeginInvoke(type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginSetHashAlgorithm"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginSetHashAlgorithm"/></param>
		public void EndSetHashAlgorithm(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetHashAlgorithm>(ar).EndInvoke(ar);
		}
#endif

#if ASYNC
		/// <summary>
		/// Sets the hash algorithm on the server to be used with the HASH command asynchronously.
		/// </summary>
		/// <param name="type">Hash algorithm to use</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <exception cref="System.NotImplementedException">Thrown if the selected algorithm is not available on the server</exception>
		public async Task SetHashAlgorithmAsync(FtpHashAlgorithm type, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			string algorithm;

			if ((HashAlgorithms & type) != type) {
				throw new NotImplementedException("The hash algorithm " + type.ToString() + " was not advertised by the server.");
			}

			algorithm = FtpHashAlgorithms.ToString(type);

			if (!(reply = await ExecuteAsync("OPTS HASH " + algorithm, token)).Success) {
				throw new FtpCommandException(reply);
			}

		}
#endif

		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm. 
		/// </summary>
		/// <remarks>
		/// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
		/// property. You should confirm that it's not equal
		/// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
		/// otherwise the server trigger a <see cref="FtpCommandException"/>
		/// due to a lack of support for the HASH command. You can
		/// set the algorithm using the <see cref="SetHashAlgorithm"/> method and
		/// you can query the server for the current hash algorithm
		/// using the <see cref="GetHashAlgorithm"/> method.
		/// </remarks>
		/// <param name="path">Full or relative path of the object to compute the hash for.</param>
		/// <returns>The hash of the file.</returns>
		/// <exception cref="FtpCommandException">
		/// Thrown if the <see cref="HashAlgorithms"/> property is <see cref="FtpHashAlgorithm.NONE"/>, 
		/// the remote path does not exist, or the command cannot be executed.
		/// </exception>
		/// <exception cref="ArgumentException">Path argument is null</exception>
		/// <exception cref="NotImplementedException">Thrown when an unknown hash algorithm type is returned by the server</exception>
		/// <example><code source="..\Examples\GetHash.cs" lang="cs" /></example>
		public FtpHash GetHash(string path) {
			FtpReply reply;
			var hash = new FtpHash();
			Match m;

			if (path == null) {
				throw new ArgumentException("GetHash(path) argument can't be null");
			}

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("HASH " + path.GetFtpPath())).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}
#endif

			// parse hash from the server reply
			m = ParseHashValue(reply, hash);

			return hash;
		}

		/// <summary>
		/// Parses the recieved hash value into the FtpHash object
		/// </summary>
		private Match ParseHashValue(FtpReply reply, FtpHash hash) {
			Match m;
			// Current draft says the server should return this:
			// SHA-256 0-49 169cd22282da7f147cb491e559e9dd filename.ext
			if (!(m = Regex.Match(reply.Message,
				@"(?<algorithm>.+)\s" +
				@"(?<bytestart>\d+)-(?<byteend>\d+)\s" +
				@"(?<hash>.+)\s" +
				@"(?<filename>.+)")).Success) {
				// Current version of FileZilla returns this:
				// SHA-1 21c2ca15cf570582949eb59fb78038b9c27ffcaf 
				m = Regex.Match(reply.Message, @"(?<algorithm>.+)\s(?<hash>.+)\s");
			}

			if (m != null && m.Success) {
				hash.Algorithm = FtpHashAlgorithms.FromString(m.Groups["algorithm"].Value);
				hash.Value = m.Groups["hash"].Value;
			}
			else {
				LogStatus(FtpTraceLevel.Warn, "Failed to parse hash from: " + reply.Message);
			}

			return m;
		}

#if !ASYNC
		private delegate FtpHash AsyncGetHash(string path);

		/// <summary>
		/// Begins an asynchronous operation to get the hash of an object on the server using the currently selected hash algorithm. 
		/// </summary>
		/// <remarks>
		/// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
		/// property. You should confirm that it's not equal
		/// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
		/// otherwise the server trigger a <see cref="FtpCommandException"/>
		/// due to a lack of support for the HASH command. You can
		/// set the algorithm using the <see cref="SetHashAlgorithm"/> method and
		/// you can query the server for the current hash algorithm
		/// using the <see cref="GetHashAlgorithm"/> method.
		/// </remarks>
		/// <param name="path">The file you want the server to compute the hash for</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetHash(string path, AsyncCallback callback, object state) {
			AsyncGetHash func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = GetHash).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetHash"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetHash"/></param>
		public FtpHash EndGetHash(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetHash>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm asynchronously. 
		/// </summary>
		/// <remarks>
		/// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
		/// property. You should confirm that it's not equal
		/// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
		/// otherwise the server trigger a <see cref="FtpCommandException"/>
		/// due to a lack of support for the HASH command. You can
		/// set the algorithm using the <see cref="SetHashAlgorithm"/> method and
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
		public async Task<FtpHash> GetHashAsync(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			var hash = new FtpHash();
			Match m;

			if (path == null) {
				throw new ArgumentException("GetHash(path) argument can't be null");
			}

			if (!(reply = await ExecuteAsync("HASH " + path.GetFtpPath(), token)).Success) {
				throw new FtpCommandException(reply);
			}

			// parse hash from the server reply
			m = ParseHashValue(reply, hash);

			return hash;
		}
#endif

		#endregion

		#region File Checksum

		/// <summary>
		/// Retrieves a checksum of the given file using the specified checksum algorithum, or using the first available algorithm that the server supports.
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithm"/>
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
		/// <example><code source="..\Examples\GetChecksum.cs" lang="cs" /></example>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public FtpHash GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE) {

			// if HASH is supported and the caller prefers an algorithm and that algorithm is supported
			var useFirst = algorithm == FtpHashAlgorithm.NONE;
			if (HasFeature(FtpCapability.HASH) && !useFirst && HashAlgorithms.HasFlag(algorithm)) {

				// switch to that algorithm
				SetHashAlgorithm(algorithm);

				// get the hash of the file
				return GetHash(path);

			}

			// if HASH is supported and the caller does not prefer any specific algorithm
			else if (HasFeature(FtpCapability.HASH) && useFirst) {
				return GetHash(path);
			}
			else {
				var result = new FtpHash();

				// execute the first available algorithm, or the preferred algorithm if specified

				if (HasFeature(FtpCapability.MD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = GetMD5(path);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = GetXMD5(path);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.MMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = GetMD5(path);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XSHA1) && (useFirst || algorithm == FtpHashAlgorithm.SHA1)) {
					result.Value = GetXSHA1(path);
					result.Algorithm = FtpHashAlgorithm.SHA1;
				}
				else if (HasFeature(FtpCapability.XSHA256) && (useFirst || algorithm == FtpHashAlgorithm.SHA256)) {
					result.Value = GetXSHA256(path);
					result.Algorithm = FtpHashAlgorithm.SHA256;
				}
				else if (HasFeature(FtpCapability.XSHA512) && (useFirst || algorithm == FtpHashAlgorithm.SHA512)) {
					result.Value = GetXSHA512(path);
					result.Algorithm = FtpHashAlgorithm.SHA512;
				}
				else if (HasFeature(FtpCapability.XCRC) && (useFirst || algorithm == FtpHashAlgorithm.CRC)) {
					result.Value = GetXCRC(path);
					result.Algorithm = FtpHashAlgorithm.CRC;
				}

				return result;
			}
		}

#if !ASYNC
		private delegate FtpHash AsyncGetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE);

		/// <summary>
		/// Begins an asynchronous operation to retrieve a checksum of the given file using a checksum method that the server supports, if any. 
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithm"/>
		/// 2. MD5 / XMD5 / MMD5 commands
		/// 3. XSHA1 command
		/// 4. XSHA256 command
		/// 5. XSHA512 command
		/// 6. XCRC command
		/// </remarks>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetChecksum(string path, AsyncCallback callback,
			object state, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE) {
			var func = new AsyncGetChecksum(GetChecksum);
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = func.BeginInvoke(path, algorithm, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetChecksum"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetChecksum"/></param>
		/// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
		/// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
		/// the underlying calls.</returns>
		public FtpHash EndGetChecksum(IAsyncResult ar) {
			AsyncGetChecksum func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");
				}

				func = (AsyncGetChecksum)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Retrieves a checksum of the given file using the specified checksum algorithum, or using the first available algorithm that the server supports.
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithm"/>
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
		/// <example><code source="..\Examples\GetChecksum.cs" lang="cs" /></example>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<FtpHash> GetChecksumAsync(string path, CancellationToken token = default(CancellationToken), FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE) {

			// if HASH is supported and the caller prefers an algorithm and that algorithm is supported
			var useFirst = algorithm == FtpHashAlgorithm.NONE;
			if (HasFeature(FtpCapability.HASH) && !useFirst && HashAlgorithms.HasFlag(algorithm)) {

				// switch to that algorithm
				await SetHashAlgorithmAsync(algorithm, token);

				// get the hash of the file
				return await GetHashAsync(path, token);

			}

			// if HASH is supported and the caller does not prefer any specific algorithm
			else if (HasFeature(FtpCapability.HASH) && useFirst) {
				return await GetHashAsync(path, token);
			}

			else {
				var result = new FtpHash();

				// execute the first available algorithm, or the preferred algorithm if specified

				if (HasFeature(FtpCapability.MD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetMD5Async(path, token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetXMD5Async(path, token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.MMD5) && (useFirst || algorithm == FtpHashAlgorithm.MD5)) {
					result.Value = await GetMD5Async(path, token);
					result.Algorithm = FtpHashAlgorithm.MD5;
				}
				else if (HasFeature(FtpCapability.XSHA1) && (useFirst || algorithm == FtpHashAlgorithm.SHA1)) {
					result.Value = await GetXSHA1Async(path, token);
					result.Algorithm = FtpHashAlgorithm.SHA1;
				}
				else if (HasFeature(FtpCapability.XSHA256) && (useFirst || algorithm == FtpHashAlgorithm.SHA256)) {
					result.Value = await GetXSHA256Async(path, token);
					result.Algorithm = FtpHashAlgorithm.SHA256;
				}
				else if (HasFeature(FtpCapability.XSHA512) && (useFirst || algorithm == FtpHashAlgorithm.SHA512)) {
					result.Value = await GetXSHA512Async(path, token);
					result.Algorithm = FtpHashAlgorithm.SHA512;
				}
				else if (HasFeature(FtpCapability.XCRC) && (useFirst || algorithm == FtpHashAlgorithm.CRC)) {
					result.Value = await GetXCRCAsync(path, token);
					result.Algorithm = FtpHashAlgorithm.CRC;
				}

				return result;
			}
		}
#endif

		#endregion

		#region MD5

		/// <summary>
		/// Gets the MD5 hash of the specified file using MD5. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public string GetMD5(string path) {
			// http://tools.ietf.org/html/draft-twine-ftpmd5-00#section-3.1
			FtpReply reply;
			string response;

			if (!(reply = Execute("MD5 " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			response = reply.Message;
			foreach (var responsePrefix in new[] { path, $@"""{path}""" }) {
				if (!response.StartsWith(responsePrefix)) {
					continue;
				}

				response = response.Remove(0, responsePrefix.Length).Trim();
				break;
			}

			return response;
		}

#if !ASYNC
		private delegate string AsyncGetMD5(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve a MD5 hash. The MD5 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetMD5(string path, AsyncCallback callback, object state) {
			var func = new AsyncGetMD5(GetMD5);
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = func.BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetMD5"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetMD5"/></param>
		/// <returns>The MD5 hash of the specified file.</returns>
		public string EndGetMD5(IAsyncResult ar) {
			AsyncGetMD5 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");
				}

				func = (AsyncGetMD5)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#endif
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
		public async Task<string> GetMD5Async(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			string response;

			if (!(reply = await ExecuteAsync("MD5 " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			response = reply.Message;
			if (response.StartsWith(path)) {
				response = response.Remove(0, path.Length).Trim();
			}

			return response;
		}

#endif

		#endregion

		#region XCRC

		/// <summary>
		/// Get the CRC value of the specified file. This is a non-standard extension of the protocol 
		/// and may throw a FtpCommandException if the server does not support it.
		/// </summary>
		/// <param name="path">The path of the file you'd like the server to compute the CRC value for.</param>
		/// <returns>The response from the server, typically the XCRC value. FtpCommandException thrown on error</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public string GetXCRC(string path) {
			FtpReply reply;

			if (!(reply = Execute("XCRC " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}

#if !ASYNC
		private delegate string AsyncGetXCRC(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve a CRC hash. The XCRC command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXCRC(string path, AsyncCallback callback, object state) {
			var func = new AsyncGetXCRC(GetXCRC);
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = func.BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXCRC"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXCRC"/></param>
		/// <returns>The CRC hash of the specified file.</returns>
		public string EndGetXCRC(IAsyncResult ar) {
			AsyncGetXCRC func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");
				}

				func = (AsyncGetXCRC)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the CRC hash of the specified file using XCRC asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>Server response, presumably the CRC hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<string> GetXCRCAsync(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			string response;

			if (!(reply = await ExecuteAsync("MD5 " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			response = reply.Message;
			if (response.StartsWith(path)) {
				response = response.Remove(0, path.Length).Trim();
			}

			return response;
		}
#endif

		#endregion

		#region XMD5

		/// <summary>
		/// Gets the MD5 hash of the specified file using XMD5. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public string GetXMD5(string path) {
			FtpReply reply;

			if (!(reply = Execute("XMD5 " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}

#if !ASYNC
		private delegate string AsyncGetXMD5(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve a XMD5 hash. The XMD5 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXMD5(string path, AsyncCallback callback, object state) {
			var func = new AsyncGetXMD5(GetXMD5);
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = func.BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXMD5"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXMD5"/></param>
		/// <returns>The MD5 hash of the specified file.</returns>
		public string EndGetXMD5(IAsyncResult ar) {
			AsyncGetXMD5 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");
				}

				func = (AsyncGetXMD5)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the MD5 hash of the specified file using XMD5 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<string> GetXMD5Async(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			if (!(reply = await ExecuteAsync("XMD5 " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}
#endif

		#endregion

		#region XSHA1

		/// <summary>
		/// Gets the SHA-1 hash of the specified file using XSHA1. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-1 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public string GetXSHA1(string path) {
			FtpReply reply;

			if (!(reply = Execute("XSHA1 " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}

#if !ASYNC
		private delegate string AsyncGetXSHA1(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve a SHA1 hash. The XSHA1 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXSHA1(string path, AsyncCallback callback, object state) {
			var func = new AsyncGetXSHA1(GetXSHA1);
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = func.BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXSHA1"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXSHA1"/></param>
		/// <returns>The SHA-1 hash of the specified file.</returns>
		public string EndGetXSHA1(IAsyncResult ar) {
			AsyncGetXSHA1 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");
				}

				func = (AsyncGetXSHA1)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the SHA-1 hash of the specified file using XSHA1 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>Server response, presumably the SHA-1 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<string> GetXSHA1Async(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			if (!(reply = await ExecuteAsync("XSHA1 " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}
#endif

		#endregion

		#region XSHA256

		/// <summary>
		/// Gets the SHA-256 hash of the specified file using XSHA256. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-256 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public string GetXSHA256(string path) {
			FtpReply reply;

			if (!(reply = Execute("XSHA256 " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}

#if !ASYNC
		private delegate string AsyncGetXSHA256(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve a SHA256 hash. The XSHA256 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXSHA256(string path, AsyncCallback callback, object state) {
			var func = new AsyncGetXSHA256(GetXSHA256);
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = func.BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXSHA256"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXSHA256"/></param>
		/// <returns>The SHA-256 hash of the specified file.</returns>
		public string EndGetXSHA256(IAsyncResult ar) {
			AsyncGetXSHA256 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");
				}

				func = (AsyncGetXSHA256)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the SHA-256 hash of the specified file using XSHA256 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>Server response, presumably the SHA-256 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<string> GetXSHA256Async(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			if (!(reply = await ExecuteAsync("XSHA256 " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}
#endif

		#endregion

		#region XSHA512

		/// <summary>
		/// Gets the SHA-512 hash of the specified file using XSHA512. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-512 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public string GetXSHA512(string path) {
			FtpReply reply;

			if (!(reply = Execute("XSHA512 " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}

#if !ASYNC
		private delegate string AsyncGetXSHA512(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve a SHA512 hash. The XSHA512 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXSHA512(string path, AsyncCallback callback, object state) {
			var func = new AsyncGetXSHA512(GetXSHA512);
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = func.BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXSHA512"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXSHA512"/></param>
		/// <returns>The SHA-512 hash of the specified file.</returns>
		public string EndGetXSHA512(IAsyncResult ar) {
			AsyncGetXSHA512 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");
				}

				func = (AsyncGetXSHA512)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the SHA-512 hash of the specified file using XSHA512 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>Server response, presumably the SHA-512 hash.</returns>
		/// <exception cref="FtpCommandException">The command fails</exception>
		public async Task<string> GetXSHA512Async(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			if (!(reply = await ExecuteAsync("XSHA512 " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			return reply.Message;
		}

#endif

		#endregion
	}
}