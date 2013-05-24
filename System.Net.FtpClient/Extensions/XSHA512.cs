using System;
using System.Collections.Generic;
using System.Net.FtpClient;

namespace System.Net.FtpClient.Extensions {
    /// <summary>
    /// Implementation of the non-standard XSHA512 command
    /// </summary>
    public static class XSHA512 {
        delegate string AsyncGetXSHA512(string path);
        static Dictionary<IAsyncResult, AsyncGetXSHA512> m_asyncmethods = new Dictionary<IAsyncResult, AsyncGetXSHA512>();

        /// <summary>
        /// Gets the SHA-512 hash of the specified file using XSHA512. This is a non-standard extension
        /// to the protocol and may or may not work. A FtpCommandException will be
        /// thrown if the command fails.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <returns>Server response, presumably the SHA-512 hash.</returns>
        public static string GetXSHA512(this FtpClient client, string path) {
            FtpReply reply;

            if (!(reply = client.Execute("XSHA512 {0}", path)).Success)
                throw new FtpCommandException(reply);

            return reply.Message;
        }

        /// <summary>
        /// Asynchronusly retrieve a SHA512 hash. The XSHA512 command is non-standard
        /// and not guaranteed to work.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State Object</param>
        /// <returns>IAsyncResult</returns>
        public static IAsyncResult BeginGetXSHA512(this FtpClient client, string path, AsyncCallback callback, object state) {
            AsyncGetXSHA512 func = new AsyncGetXSHA512(client.GetXSHA512);
            IAsyncResult ar = func.BeginInvoke(path, callback, state); ;
            
            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous call to BeginGetXSHA512()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetXSHA512()</param>
        /// <returns>The SHA-512 hash of the specified file.</returns>
        public static string EndGetXSHA512(IAsyncResult ar) {
            AsyncGetXSHA512 func = null;

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
