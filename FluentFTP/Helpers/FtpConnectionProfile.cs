using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;

namespace FluentFTP {
	public class FtpConnectionProfile {

		public FtpEncryptionMode Encryption;
		public SslProtocols Protocols;
		public FtpDataConnectionType DataConnection;
		public Encoding Encoding;

	}
}
