using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Streams {
	public interface IFtpStreamConfig {
		int LogLevel { get; set; }
		FluentFTP.GnuTLS.Core.LogDebugInformationMessagesT LogDebugInformation { get; set; }
		int LogBuffSize { get; set; }
		string Priority { get; set; }
		int HandshakeTimeout { get; set; }
	}
}
