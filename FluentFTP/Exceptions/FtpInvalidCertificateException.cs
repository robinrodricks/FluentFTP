using System;
using System.Security.Authentication;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace FluentFTP.Exceptions {

	/// <summary>
	/// Exception is thrown when TLS/SSL encryption could not be negotiated by the FTP server.
	/// </summary>
#if NETFRAMEWORK
	[Serializable]
#endif
	public class FtpInvalidCertificateException : FtpException {

		/// <summary>
		/// AuthenticationException that caused this.
		/// </summary>
		public new Exception InnerException { get; private set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public FtpInvalidCertificateException(Exception innerException)
			: base("FTPS security could not be established on the server. The certificate was not accepted.", innerException) {
		}

		/// <summary>
		/// Custom error message
		/// </summary>
		/// <param name="message">Error message</param>
		public FtpInvalidCertificateException(string message)
			: base(message) {
		}

#if NETFRAMEWORK
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpInvalidCertificateException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

#endif
	}
}