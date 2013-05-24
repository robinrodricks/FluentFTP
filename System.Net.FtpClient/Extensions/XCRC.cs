using System;
using System.Collections.Generic;
using System.Net.FtpClient;

namespace System.Net.FtpClient.Extensions {
    /// <summary>
    /// Implementation of the non-standard XCRC command
    /// </summary>
    public static class XCRC  {
        delegate string AsyncGetXCRC(string path);
        static Dictionary<IAsyncResult, AsyncGetXCRC> m_asyncmethods = new Dictionary<IAsyncResult, AsyncGetXCRC>();

        /// <summary>
        /// Get the CRC value of the specified file. This is a non-standard extension of the protocol 
        /// and may throw a FtpCommandException if the server does not support it.
        /// </summary>
        /// <param name="client">FtpClient object</param>
        /// <param name="path">The path of the file you'd like the server to compute the CRC value for.</param>
        /// <returns>The response from the server, typically the CRC value. FtpCommandException thrown on error</returns>
        public static string GetXCRC(this FtpClient client, string path) {
            FtpReply reply;
            
            if (!(reply = client.Execute("XCRC {0}", path)).Success)
                throw new FtpCommandException(reply);

            return reply.Message;
        }

        /// <summary>
        /// Asynchronusly retrieve a CRC hash. The XCRC command is non-standard
        /// and not guaranteed to work.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State Object</param>
        /// <returns>IAsyncResult</returns>
        public static IAsyncResult BeginGetXCRC(this FtpClient client, string path, AsyncCallback callback, object state) {
            AsyncGetXCRC func = new AsyncGetXCRC(client.GetXCRC);
            IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous call to BeginGetXCRC()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetXCRC()</param>
        /// <returns>The CRC hash of the specified file.</returns>
        public static string EndGetXCRC(IAsyncResult ar) {
            AsyncGetXCRC func = null;

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
