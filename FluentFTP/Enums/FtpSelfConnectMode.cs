using System;

namespace FluentFTP {
	/// <summary>
	/// Server AutoConnect behaviour
	/// </summary>

	public enum FtpSelfConnectMode {

		/// <summary>
		/// Always: If the control connection is needed to process an API process (either for entering
		/// commands to the server or to retrieve information about the server (FEAT capabilities,
		/// HASH algorithms, Current Working Directory) it will be connected or reconnected if not available.
		/// </summary>
		Always,

		/// <summary>
		/// OnConnectionLost (Default): As with "Always", BUT only if there had been a connection in the
		/// first place before it got lost.
		/// </summary>
		OnConnectionLost,

		/// <summary>
		/// Never: Connections will not be made unless you explicitly call Connect(...) yourself. If a
		/// connection is needed and not established, your API processes will fail instead of attempting
		/// to connect or reconnect.
		/// </summary>
		Never,
	}
}