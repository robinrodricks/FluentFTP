using System;
using System.Collections.Generic;
using FluentFTP;

#if (CORE || NETFX45)
using System.Threading.Tasks;
#endif

namespace FluentFTP.Extensions {
	/// <summary>
	/// Implementation of the non-standard XSHA256 command
	/// </summary>
	public static class XSHA256 {
		delegate string AsyncGetXSHA256(string path);
		static Dictionary<IAsyncResult, AsyncGetXSHA256> m_asyncmethods = new Dictionary<IAsyncResult, AsyncGetXSHA256>();

		/// <summary>
		/// Gets the SHA-256 hash of the specified file using XSHA256. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-256 hash.</returns>
		public static string GetXSHA256(this FtpClient client, string path) {
			FtpReply reply;

			if (!(reply = client.Execute("XSHA256 {0}", path)).Success)
				throw new FtpCommandException(reply);

			return reply.Message;
		}

		/// <summary>
        /// Begins an asynchronous operation to retrieve a SHA256 hash. The XSHA256 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public static IAsyncResult BeginGetXSHA256(this FtpClient client, string path, AsyncCallback callback, object state) {
			AsyncGetXSHA256 func = new AsyncGetXSHA256(client.GetXSHA256);
			IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends an asynchronous call to <see cref="BeginGetXSHA256"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginGetXSHA256"/></param>
		/// <returns>The SHA-256 hash of the specified file.</returns>
		public static string EndGetXSHA256(IAsyncResult ar) {
			AsyncGetXSHA256 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Gets the SHA-256 hash of the specified file using XSHA256 asynchronously. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the SHA-256 hash.</returns>
        public static async Task<string> GetXSHA256Async(this FtpClient client, string path) {
            return await Task.Factory.FromAsync<FtpClient, string, string>(
                (c, p, ac, s) => BeginGetXSHA256(c, p, ac, s),
                ar => EndGetXSHA256(ar),
                client, path, null);
        }
#endif
	}
}