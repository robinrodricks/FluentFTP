using System;
using System.Collections.Generic;
using System.Net.FtpClient;

namespace System.Net.FtpClient.Extensions {
    /// <summary>
    /// Implementation of the non-standard MD5 command
    /// </summary>
    public static class MD5 {
        delegate string AsyncGetMD5(string path);
        static Dictionary<IAsyncResult, AsyncGetMD5> m_asyncmethods = new Dictionary<IAsyncResult, AsyncGetMD5>();

        /// <summary>
        /// Gets the MD5 hash of the specified file using MD5. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the MD5 hash.</returns>
        public static string GetMD5(this FtpClient client, string path) {
            // http://tools.ietf.org/html/draft-twine-ftpmd5-00#section-3.1
            FtpReply reply;
            string response;

            if (!(reply = client.Execute("MD5 {0}", path)).Success)
                throw new FtpCommandException(reply);

            response = reply.Message;
            if (response.StartsWith(path)) {
                response = response.Remove(0, path.Length).Trim();
            }

            return response;
        }

        /// <summary>
        /// Asynchronusly retrieve a MD5 hash. The MD5 command is non-standard
        /// and not guaranteed to work.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State Object</param>
        /// <returns>IAsyncResult</returns>
        public static IAsyncResult BeginGetMD5(this FtpClient client, string path, AsyncCallback callback, object state) {
            AsyncGetMD5 func = new AsyncGetMD5(client.GetMD5);
            IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous call to BeginGetMD5()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetMD5()</param>
        /// <returns>The MD5 hash of the specified file.</returns>
        public static string EndGetMD5(IAsyncResult ar) {
            AsyncGetMD5 func = null;

            lock (m_asyncmethods) {
                if (!m_asyncmethods.ContainsKey(ar))
                    throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

                func = m_asyncmethods[ar];
                m_asyncmethods.Remove(ar);
            }

            return func.EndInvoke(ar);
        }
    }
}
