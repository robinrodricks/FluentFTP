using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Text;

namespace FluentFTP {
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

			sb.AppendLine("client.Host = " + Host.EscapeStringLiteral() + ";");

			sb.AppendLine("client.Credentials = new NetworkCredential(" + Credentials.UserName.EscapeStringLiteral() + ", " + Credentials.Password.EscapeStringLiteral() + ");");

			sb.Append("client.EncryptionMode = FtpEncryptionMode.");
			sb.Append(Encryption.ToString());
			sb.AppendLine(";");

			sb.Append("client.SslProtocols = SslProtocols.");
			sb.Append(Protocols.ToString());
			sb.AppendLine(";");

			sb.Append("client.DataConnectionType = FtpDataConnectionType.");
			sb.Append(DataConnection.ToString());
			sb.AppendLine(";");

			sb.Append("client.Encoding = ");
			sb.Append(Encoding.ToString());
			sb.AppendLine(";");

			if (Encryption != FtpEncryptionMode.None) {
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