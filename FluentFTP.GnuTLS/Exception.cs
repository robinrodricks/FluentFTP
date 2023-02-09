using System;

namespace FluentFTP.GnuTLS {
	public class GnuTlsException : Exception {
		public GnuTlsException() : base() { }
		public GnuTlsException(string message) : base(message) { }
		public GnuTlsException(string message, Exception e) : base(message, e) { }
		public string ExMethod { get; set; } = null;
		public int ExResult { get; set; } = 0;
		public string ExMeaning { get; set; } = string.Empty;
	}
}
