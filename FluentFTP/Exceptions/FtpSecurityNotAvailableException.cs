using System;
#if !CORE
using System.Runtime.Serialization;
#endif

namespace FluentFTP {

	/// <summary>
	/// Exception is thrown when TLS/SSL encryption could not be negotiated by the FTP server.
	/// </summary>
#if !CORE
	[Serializable]
#endif
	public class FtpSecurityNotAvailableException : FtpException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public FtpSecurityNotAvailableException()
			: base("FTPS security is not available on the server. To disable FTPS, set the EncryptionMode property to None.") {
			
		}

		/// <summary>
		/// Custom error message
		/// </summary>
		/// <param name="message">Error message</param>
		public FtpSecurityNotAvailableException(string message)
			: base(message) {
		}

#if !CORE
		/// <summary>
		/// Must be implemented so every Serializer can Deserialize the Exception
		/// </summary>
		protected FtpSecurityNotAvailableException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

#endif
	}
}