using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpException : Exception {
		public FtpException(string message) : base(message) { }
	}
}
