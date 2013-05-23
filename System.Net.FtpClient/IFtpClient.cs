using System;
using System.IO;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net;

namespace System.Net.FtpClient {
    /// <summary>
    /// This interfaces was added for users using the MoQ framework
    /// for unit testing.
    /// </summary>
    public interface IFtpClient {
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpCapability Capabilities { get; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        X509CertificateCollection ClientCertificates { get; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        int ConnectTimeout { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        NetworkCredential Credentials { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        int DataConnectionConnectTimeout { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        bool DataConnectionEncryption { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        int DataConnectionReadTimeout { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpDataConnectionType DataConnectionType { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        bool EnableThreadSafeDataConnections { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        Encoding Encoding { get; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        FtpEncryptionMode EncryptionMode { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        bool SocketKeepAlive { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        int SocketPollInterval { get; set; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        string SystemType { get; }

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        event FtpSslValidation ValidateCertificate;

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        void Connect();

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        void CreateDirectory(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="force"></param>
        void CreateDirectory(string path, bool force);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        void DeleteDirectory(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="force"></param>
        void DeleteDirectory(string path, bool force);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="force"></param>
        /// <param name="options"></param>
        void DeleteDirectory(string path, bool force, FtpListOption options);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        void DeleteFile(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        FtpReply Execute(string command);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        FtpReply Execute(string command, params object[] args);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool FileExists(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        bool FileExists(string path, FtpListOption options);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        long GetFileSize(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <returns></returns>
        FtpListItem[] GetListing();

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        FtpListItem[] GetListing(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        FtpListItem[] GetListing(string path, FtpListOption options);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        DateTime GetModifiedTime(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <returns></returns>
        string[] GetNameListing();

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string[] GetNameListing(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <returns></returns>
        string GetWorkingDirectory();

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Stream OpenAppend(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Stream OpenAppend(string path, FtpDataType type);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Stream OpenRead(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="restart"></param>
        /// <returns></returns>
        Stream OpenRead(string path, long restart);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Stream OpenRead(string path, FtpDataType type);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="restart"></param>
        /// <returns></returns>
        Stream OpenRead(string path, FtpDataType type, long restart);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Stream OpenWrite(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Stream OpenWrite(string path, FtpDataType type);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dest"></param>
        void Rename(string path, string dest);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="type"></param>
        void SetDataType(FtpDataType type);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        void SetWorkingDirectory(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginConnect(AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndConnect(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginCreateDirectory(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="force"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginCreateDirectory(string path, bool force, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndCreateDirectory(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginDeleteDirectory(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="force"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginDeleteDirectory(string path, bool force, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="force"></param>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginDeleteDirectory(string path, bool force, FtpListOption options, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndDeleteDirectory(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginDeleteFile(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndDeleteFile(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginDirectoryExists(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        bool EndDirectoryExists(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginDisconnect(AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndDisconnect(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="command"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginExecute(string command, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        FtpReply EndExecute(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginFileExists(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginFileExists(string path, FtpListOption options, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        bool EndFileExists(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetFileSize(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        long EndGetFileSize(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetListing(AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetListing(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetListing(string path, FtpListOption options, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        FtpListItem[] EndGetListing(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetModifiedTime(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        DateTime EndGetModifiedTime(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetNameListing(AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetNameListing(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        string[] EndGetNameListing(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetWorkingDirectory(AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        string EndGetWorkingDirectory(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenAppend(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenAppend(string path, FtpDataType type, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        Stream EndOpenAppend(IAsyncResult ar);
        
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenRead(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="restart"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenRead(string path, long restart, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenRead(string path, FtpDataType type, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="restart"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenRead(string path, FtpDataType type, long restart, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        Stream EndOpenRead(IAsyncResult ar);
        
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenWrite(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginOpenWrite(string path, FtpDataType type, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        Stream EndOpenWrite(IAsyncResult ar);
        
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dest"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginRename(string path, string dest, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndRename(IAsyncResult ar);
        
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginSetDataType(FtpDataType type, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndSetDataType(IAsyncResult ar);
        
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginSetWorkingDirectory(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndSetWorkingDirectory(IAsyncResult ar);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <returns></returns>
        FtpHashAlgorithm GetHashAlgorithm();

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetHashAlgorithm(AsyncCallback callback, object state);
        
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        FtpHashAlgorithm EndGetHashAlgorithm(IAsyncResult ar);
        
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="type"></param>
        void SetHashAlgorithm(FtpHashAlgorithm type);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginSetHashAlgorithm(FtpHashAlgorithm type, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndSetHashAlgorithm(IAsyncResult ar);
       
        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        FtpHash GetHash(string path);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IAsyncResult BeginGetHash(string path, AsyncCallback callback, object state);

        /// <summary>
        /// Added for the MoQ unit testing framework
        /// </summary>
        /// <param name="ar"></param>
        void EndGetHash(IAsyncResult ar);
    }
}
