using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Text;

namespace FluentFTP {
	public class FtpProfile {

		public string Host;
		public NetworkCredential Credentials;
		public FtpEncryptionMode Encryption;
		public SslProtocols Protocols;
		public FtpDataConnectionType DataConnection;
		public Encoding Encoding;

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

			sb.Append("client.Encoding = Encoding.");
			sb.Append(Encoding.ToString());
			sb.AppendLine(";");

			if (Encryption != FtpEncryptionMode.None) {
				sb.AppendLine("client.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);");
			}

			sb.AppendLine("client.Connect();");

			sb.AppendLine();

			if (Encryption != FtpEncryptionMode.None) {
				sb.AppendLine("// add your logic to test if the SSL certificate is valid (see the FAQ for examples)");
				sb.AppendLine("private void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {");
				sb.AppendLine("	e.Accept = true;");
				sb.AppendLine("}");
			}

			return sb.ToString();
		}

	}
}
