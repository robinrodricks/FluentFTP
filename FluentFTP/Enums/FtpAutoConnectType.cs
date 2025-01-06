using System;

namespace FluentFTP {
	/// <summary>
	/// Server AutoConnect behaviour
	/// </summary>
	///

	public enum FtpAutoConnectType {
		Never,
		OnConnectionLost,
		Always,
	}
}