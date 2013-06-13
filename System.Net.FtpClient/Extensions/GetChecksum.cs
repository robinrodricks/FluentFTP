using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient.Extensions {
    /// <summary>
    /// Retrieve checksum of file on the server
    /// </summary>
    public static class ChecksumExtension {
        delegate FtpHash AsyncGetChecksum(string path);
        static Dictionary<IAsyncResult, AsyncGetChecksum> m_asyncmethods = new Dictionary<IAsyncResult, AsyncGetChecksum>();

        /// <summary>
        /// Retrieves a checksum of the given file using a checksumming method
        /// that the server supports, if any. The algorithm used goes in this order:
        /// 1. HASH command; server preferred algorithm. See FtpClient.SetHashAlgorithm()
        /// 2. MD5 / XMD5 commands
        /// 3. XSHA1 command
        /// 4. XSHA256 command
        /// 5. XSHA512 command
        /// 6. XCRC command
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path of the file to checksum</param>
        /// <returns>FtpHash object containing the value and algorithm. Use the IsValid property to
        /// determine if this command was successfull. FtpCommandException's can be thrown from
        /// the underlying calls.</returns>
        /// <example><code source="..\Examples\GetChecksum.cs" lang="cs" /></example>
        public static FtpHash GetChecksum(this FtpClient client, string path) {
            if (client.HasFeature(FtpCapability.HASH)) {
                return client.GetHash(path);
            }
            else {
                FtpHash res = new FtpHash();

                if (client.HasFeature(FtpCapability.MD5)) {
                    res.Value = client.GetMD5(path);
                    res.Algorithm = FtpHashAlgorithm.MD5;
                }
                else if (client.HasFeature(FtpCapability.XMD5)) {
                    res.Value = client.GetXMD5(path);
                    res.Algorithm = FtpHashAlgorithm.MD5;
                }
                else if (client.HasFeature(FtpCapability.XSHA1)) {
                    res.Value = client.GetXSHA1(path);
                    res.Algorithm = FtpHashAlgorithm.SHA1;
                }
                else if (client.HasFeature(FtpCapability.XSHA256)) {
                    res.Value = client.GetXSHA256(path);
                    res.Algorithm = FtpHashAlgorithm.SHA256;
                }
                else if (client.HasFeature(FtpCapability.XSHA512)) {
                    res.Value = client.GetXSHA512(path);
                    res.Algorithm = FtpHashAlgorithm.SHA512;
                }
                else if (client.HasFeature(FtpCapability.XCRC)) {
                    res.Value = client.GetXCRC(path);
                    res.Algorithm = FtpHashAlgorithm.CRC;
                }

                return res;
            }
        }

        /// <summary>
        /// Asynchronusly retrieve a checksum of the specified file. This feature
        /// is non-standard.
        /// </summary>
        /// <param name="client">FtpClient Object</param>
        /// <param name="path">Full or relative path to remote file</param>
        /// <param name="callback">AsyncCallback</param>
        /// <param name="state">State Object</param>
        /// <returns>IAsyncResult</returns>
        public static IAsyncResult BeginGetChecksum(this FtpClient client, string path, AsyncCallback callback, object state) {
            AsyncGetChecksum func = new AsyncGetChecksum(client.GetChecksum);
            IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

            lock (m_asyncmethods) {
                m_asyncmethods.Add(ar, func);
            }

            return ar;
        }

        /// <summary>
        /// Ends an asynchronous call to BeginGetChecksum()
        /// </summary>
        /// <param name="ar">IAsyncResult returned from BeginGetChecksum()</param>
        /// <returns>FtpHash object containing the value and algorithm. Use the IsValid property to
        /// determine if this command was successfull. FtpCommandException's can be thrown from
        /// the underlying calls.</returns>
        public static FtpHash EndGetChecksum(IAsyncResult ar) {
            AsyncGetChecksum func = null;

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
