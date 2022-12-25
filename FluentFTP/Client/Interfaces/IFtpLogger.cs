using FluentFTP.Model;

namespace FluentFTP {
	public interface IFtpLogger {
		void Log(FtpLogEntry entry);
	}
}
