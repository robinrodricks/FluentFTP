using FluentFTP.Rules;
using FluentFTP.Servers;
using Microsoft.Extensions.Logging;
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

	public interface IBaseFtpClient {

		FtpConfig Config { get; set; }
		ILogger Logger { get; set; }
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
		Encoding Encoding { get; set; }

		Action<FtpTraceLevel, string> LegacyLogger { get; set; }

}
}