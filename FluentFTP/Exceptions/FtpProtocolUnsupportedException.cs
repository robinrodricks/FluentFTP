using System;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace FluentFTP {
#if NETFRAMEWORK
	[Serializable]
#endif
	public class FtpProtocolUnsupportedException : FtpException {

		/// <summary>
		/// Custom error message
		/// </summary>
		/// <param name="message">Error message</param>
		public FtpProtocolUnsupportedException(string message)
			: base(message) {
		}
	}
}
