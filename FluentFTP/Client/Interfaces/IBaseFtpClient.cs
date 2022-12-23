using FluentFTP.Logging;
using FluentFTP.Rules;
using FluentFTP.Servers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {

	/// <summary>
	/// Base object for FtpClient, AsyncFtpClient and the internal client
	/// </summary>
	public interface IBaseFtpClient {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		FtpConfig Config { get; set; }
		IFluentLogger Logger { get; set; }
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

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}