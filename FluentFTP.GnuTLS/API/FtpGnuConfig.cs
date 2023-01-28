using System;
using FluentFTP.Streams;

namespace FluentFTP.GnuTLS {

	public class FtpGnuConfig : IFtpStreamConfig {

		public int LogLevel { get; set; } = 2;
		public int LogBuffSize { get; set; } = 50;

	}
}