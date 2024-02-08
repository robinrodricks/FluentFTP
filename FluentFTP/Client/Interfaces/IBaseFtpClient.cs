using FluentFTP.Servers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FluentFTP {

	/// <summary>
	/// Callback for any custom streams to handle certificate validation
	/// </summary>
	public delegate bool CustomRemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, string errorMessage);

	/// <summary>
	/// Base object for FtpClient, AsyncFtpClient and the internal client
	/// </summary>
	public interface IBaseFtpClient {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		FtpConfig Config { get; set; }
		IFtpLogger Logger { get; set; }
		bool IsDisposed { get; }
		bool IsConnected { get; }
		string Host { get; set; }
		int Port { get; set; }
		NetworkCredential Credentials { get; set; }
		List<FtpCapability> Capabilities { get; }
		FtpHashAlgorithm HashAlgorithms { get; }
		event FtpSslValidation ValidateCertificate;
		string SystemType { get; }
		FtpServer ServerType { get; }
		FtpBaseServer ServerHandler { get; set; }
		FtpOperatingSystem ServerOS { get; }
		string ConnectionType { get; }
		FtpReply LastReply { get; }
		List<FtpReply> LastReplies { get; set; }
		Encoding Encoding { get; set; }
		Action<FtpTraceLevel, string> LegacyLogger { get; set; }
		FtpClientState Status { get; }
		FtpIpVersion? InternetProtocol { get; }
		bool IsAuthenticated { get; }
		SslProtocols SslProtocolActive { get; }
		bool IsEncrypted { get; }
		bool ValidateCertificateHandlerExists { get; }
		bool RecursiveList { get; }
		IPEndPoint SocketLocalEndPoint { get; }
		IPEndPoint SocketRemoteEndPoint { get; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}