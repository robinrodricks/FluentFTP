using System;
using System.Collections.Generic;
using System.Net.FtpClient;

namespace System.Net.FtpClient.Extensions {
    /// <summary>
    /// Implementation of non-standard XMD5 command.
    /// </summary>
    public static class XMD5 {
        delegate string AsyncGetXMD5(string path);
        static Dictionary<IAsyncResult, AsyncGetXMD5> m_asyncmethods = new Dictionary<IAsyncResult, AsyncGetXMD5>();

        /// <summary>
        /// Gets the MD5 hash of the specified file using XMD5. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the MD5 hash.</returns>
        public static string GetXMD5(this FtpClient client, string path) {
            FtpReply reply;

            if (!(reply = client.Execute("XMD5 {0}", path)).Success)
                throw new FtpCommandException(reply);

            return reply.Message;
        }

        /// <summary>
        /// Asynchronusly retrieve a MD5 hash. The XMD5 command is non-standard
        /// and not guaranteed to work.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State Object</param>
        /// <returns>IAsyncResult</returns>
        public static IAsyncResult BeginGetXMD5(this FtpClient client, string path, AsyncCallback callback, object state) {
            AsyncGetXMD5 func = new AsyncGetXMD5(client.GetXMD5);
            IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous call to BeginGetXMD5()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetXMD5()</param>
        /// <returns>The MD5 hash of the specified file.</returns>
        public static string EndGetXMD5(IAsyncResult ar) {
            AsyncGetXMD5 func = null;

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
