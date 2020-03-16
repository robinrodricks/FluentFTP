using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Text;
#if !CORE
using System.Runtime.Serialization;
#endif

namespace FluentFTP {
	
#if !CORE
	[Serializable]
#endif
	public class FtpProfile {
		/// <summary>
		/// The host IP address or URL of the FTP server
		/// </summary>
		public string Host;

		/// <summary>
		/// The FTP username and password used to login
		/// </summary>
		public NetworkCredential Credentials;

		/// <summary>
		/// A working Encryption Mode found for this profile
		/// </summary>
		public FtpEncryptionMode Encryption = FtpEncryptionMode.None;

		/// <summary>
		/// A working Ssl Protocol setting found for this profile
		/// </summary>
		public SslProtocols Protocols = SslProtocols.None;

		/// <summary>
		/// A working Data Connection Type found for this profile
		/// </summary>
		public FtpDataConnectionType DataConnection = FtpDataConnectionType.PASV;

		/// <summary>
		/// A working Encoding setting found for this profile
		/// </summary>
		public Encoding Encoding;

		/// <summary>
		/// A working Timeout setting found for this profile, or 0 if default value should be used
		/// </summary>
		public int Timeout;

		/// <summary>
		/// A working SocketPollInterval setting found for this profile, or 0 if default value should be used
		/// </summary>
		public int SocketPollInterval;

		/// <summary>
		/// A working RetryAttempts setting found for this profile, or 0 if default value should be used
		/// </summary>
		public int RetryAttempts;


		/// <summary>
		/// Generates valid C# code for this connection profile.
		/// </summary>
		/// <returns></returns>
		public string ToCode() {
			var sb = new StringBuilder();

			sb.AppendLine("// add this above your namespace declaration");
			sb.AppendLine("using FluentFTP;");
			sb.AppendLine("using System.Text;");
			sb.AppendLine("using System.Net;");
			sb.AppendLine("using System.Security.Authentication;");
			sb.AppendLine();

			sb.AppendLine("// add this to create and configure the FTP client");
			sb.AppendLine("var client = new FtpClient();");


			// use LoadProfile rather than setting each property manually
			// this also allows us to use the high level properties like Timeout without
			// setting each Timeout individually
			sb.AppendLine("client.LoadProfile(new FtpProfile {");

			sb.AppendLine("	Host = " + Host.EscapeStringLiteral() + ",");
			sb.AppendLine("	Credentials = new NetworkCredential(" + Credentials.UserName.EscapeStringLiteral() + ", " + Credentials.Password.EscapeStringLiteral() + "),");
			sb.AppendLine("	Encryption = FtpEncryptionMode." + Encryption.ToString() + ",");
			sb.AppendLine("	Protocols = SslProtocols." + Protocols.ToString() + ",");
			sb.AppendLine("	DataConnection = FtpDataConnectionType." + DataConnection.ToString() + ",");


			// Fix #468 - Invalid code generated: Encoding = System.Text.UTF8Encoding+UTF8EncodingSealed
			var encoding = Encoding.ToString();
			if (encoding.Contains("+")) {
				sb.AppendLine("	Encoding = " + encoding.Substring(0, encoding.IndexOf('+')) + ",");
			}
			else {
				sb.AppendLine("	Encoding = " + encoding + ",");
			}

			// Required for #533 - Auto detect Azure servers and use working settings
			if (Timeout != 0) {
				sb.AppendLine("	Timeout = " + Timeout + ",");
			}
			if (SocketPollInterval != 0) {
				sb.AppendLine("	SocketPollInterval = " + SocketPollInterval + ",");
			}
			if (RetryAttempts != 0) {
				sb.AppendLine("	RetryAttempts = " + RetryAttempts + ",");
			}

			sb.AppendLine("});");

			if (Encryption != FtpEncryptionMode.None) {
				sb.AppendLine("// if you want to accept any certificate then set ValidateAnyCertificate=true and delete the following event handler");
				sb.AppendLine("client.ValidateCertificate += new FtpSslValidation(delegate (FtpClient control, FtpSslValidationEventArgs e) {");
				sb.AppendLine("	// add your logic to test if the SSL certificate is valid (see the FAQ for examples)");
				sb.AppendLine("	e.Accept = true;");
				sb.AppendLine("});");
			}

			sb.AppendLine("client.Connect();");

			return sb.ToString();
		}
	}
}