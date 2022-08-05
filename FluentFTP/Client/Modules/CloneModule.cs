using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentFTP.Client.Modules {
	internal class CloneModule {

		public static void Clone(FtpClient read, FtpClient write) {

			// configure new connection as clone of self
			write.InternetProtocolVersions = read.InternetProtocolVersions;
			write.SocketPollInterval = read.SocketPollInterval;
			write.StaleDataCheck = read.StaleDataCheck;
			write.EnableThreadSafeDataConnections = read.EnableThreadSafeDataConnections;
			write.NoopInterval = read.NoopInterval;
			write.Encoding = read.Encoding;
			write.Host = read.Host;
			write.Port = read.Port;
			write.Credentials = read.Credentials;
			write.MaximumDereferenceCount = read.MaximumDereferenceCount;
			write.ClientCertificates.AddRange(read.ClientCertificates);
			write.DataConnectionType = read.DataConnectionType;
			write.DisconnectWithQuit = read.DisconnectWithQuit;
			write.DisconnectWithShutdown = read.DisconnectWithShutdown;
			write.ConnectTimeout = read.ConnectTimeout;
			write.ReadTimeout = read.ReadTimeout;
			write.DataConnectionConnectTimeout = read.DataConnectionConnectTimeout;
			write.DataConnectionReadTimeout = read.DataConnectionReadTimeout;
			write.SocketKeepAlive = read.SocketKeepAlive;
			write.EncryptionMode = read.EncryptionMode;
			write.DataConnectionEncryption = read.DataConnectionEncryption;
			write.SslProtocols = read.SslProtocols;
			write.SslBuffering = read.SslBuffering;
			write.TransferChunkSize = read.TransferChunkSize;
			write.LocalFileBufferSize = read.LocalFileBufferSize;
			write.ListingDataType = read.ListingDataType;
			write.ListingParser = read.ListingParser;
			write.ListingCulture = read.ListingCulture;
			write.ListingCustomParser = read.ListingCustomParser;
			write.TimeZone = read.TimeZone;
			write.TimeConversion = read.TimeConversion;
			write.RetryAttempts = read.RetryAttempts;
			write.UploadRateLimit = read.UploadRateLimit;
			write.DownloadZeroByteFiles = read.DownloadZeroByteFiles;
			write.DownloadRateLimit = read.DownloadRateLimit;
			write.DownloadDataType = read.DownloadDataType;
			write.UploadDataType = read.UploadDataType;
			write.ActivePorts = read.ActivePorts;
			write.PassiveBlockedPorts = read.PassiveBlockedPorts;
			write.PassiveMaxAttempts = read.PassiveMaxAttempts;
			write.SendHost = read.SendHost;
			write.SendHostDomain = read.SendHostDomain;
			write.FXPDataType = read.FXPDataType;
			write.FXPProgressInterval = read.FXPProgressInterval;
			write.ServerHandler = read.ServerHandler;
			write.UploadDirectoryDeleteExcluded = read.UploadDirectoryDeleteExcluded;
			write.DownloadDirectoryDeleteExcluded = read.DownloadDirectoryDeleteExcluded;

			// copy capabilities
			try {
				write.SetFeatures(read.Capabilities);
			}
			catch (Exception ex) {}


			// configure new connection as clone of self (newer version .NET only)
#if ASYNC && !CORE14 && !CORE16
			write.SocketLocalIp = read.SocketLocalIp;
#endif

			// configure new connection as clone of self (.NET core props only)
#if CORE
			write.LocalTimeZone = read.LocalTimeZone;
#endif

			// configure new connection as clone of self (.NET framework props only)
#if !CORE
			write.PlainTextEncryption = read.PlainTextEncryption;
#endif

			// always accept certificate no matter what because if code execution ever
			// gets here it means the certificate on the control connection object being
			// cloned was already accepted.
			write.ValidateCertificate += new FtpSslValidation(
				delegate (FtpClient obj, FtpSslValidationEventArgs e) { e.Accept = true; });

		}

	}
}
