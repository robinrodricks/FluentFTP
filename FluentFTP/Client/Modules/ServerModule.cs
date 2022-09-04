using FluentFTP.Servers;
using FluentFTP.Client.BaseClient;

namespace FluentFTP.Client.Modules {

	/// <summary>
	/// All servers with server-specific handling and support are listed here.
	/// Its possible you can connect to other FTP servers too.
	/// 
	/// To add support for another standard FTP server:
	///		1) Add a new enum in the `FtpServer` enum
	///		2) Add a new class extending `FtpBaseServer` under the `Servers.Handlers` NS
	///		3) Create a new instance of your class in `FtpHandlerIndex.AllServers`
	///		
	/// To support a custom FTP server you only need to extend `FtpBaseServer`
	/// and set it on your client.ServerHandler before calling Connect.
	/// </summary>
	internal static class ServerModule {

		/// <summary>
		/// Detect the FTP Server based on the welcome message sent by the server after getting the 220 connection command.
		/// Its the primary method.
		/// </summary>
		public static FtpServer DetectFtpServer(BaseFtpClient client, FtpReply handshakeReply) {
			var serverType = client.ServerType;

			if (handshakeReply.Success && (handshakeReply.Message != null || handshakeReply.InfoMessages != null)) {
				var message = (handshakeReply.Message ?? "") + (handshakeReply.InfoMessages ?? "");

				// try to detect any of the servers
				foreach (var server in FtpHandlerIndex.AllServers) {
					if (server.DetectByWelcome(message)) {
						serverType = server.ToEnum();
						break;
					}
				}

				// trace it
				if (serverType != FtpServer.Unknown) {
					((IInternalFtpClient)client).LogLine(FtpTraceLevel.Info, "Status:   Detected FTP server: " + serverType.ToString());
				}
			}

			return serverType;
		}

		/// <summary>
		/// Get a default FTP Server handler based on the enum value.
		/// </summary>
		public static FtpBaseServer GetServerHandler(FtpServer value) {
			if (value != FtpServer.Unknown) {
				foreach (var server in FtpHandlerIndex.AllServers) {
					if (server.ToEnum() == value) {
						return server;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Detect the FTP Server based on the response to the SYST connection command.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public static FtpOperatingSystem DetectFtpOSBySyst(BaseFtpClient client) {
			var serverOS = client.ServerOS;

			// detect OS type
			var system = client.SystemType.ToUpper();

			if (system.StartsWith("WINDOWS")) {
				// Windows OS
				serverOS = FtpOperatingSystem.Windows;
			}
			else if (system.Contains("Z/OS")) {
				// IBM z/OS, message can be one of the two, depending on realm (and server config)
				// Syst message: "215 MVS is the operating system of this server. FTP Server is running on z/OS."
				// Syst message: "215 UNIX is the operating system of this server. FTP Server is running on z/OS."
				//**
				//** Important: Keep this z/OS IN FRONT of the UNIX entry, both contain "UNIX".
				//**
				serverOS = FtpOperatingSystem.IBMzOS;
			}
			else if (system.Contains("UNIX") || system.Contains("AIX")) {
				// Unix OS
				serverOS = FtpOperatingSystem.Unix;
			}
			else if (system.Contains("VMS")) {
				// VMS or OpenVMS
				serverOS = FtpOperatingSystem.VMS;
			}
			else if (system.Contains("OS/400")) {
				// IBM OS/400
				serverOS = FtpOperatingSystem.IBMOS400;
			}
			else if (system.Contains("SUNOS")) {
				// SUN OS
				serverOS = FtpOperatingSystem.SunOS;
			}
			else {
				// assume Unix OS
				serverOS = FtpOperatingSystem.Unknown;
			}

			return serverOS;
		}

		/// <summary>
		/// Detect the FTP Server based on the response to the SYST connection command.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public static FtpServer DetectFtpServerBySyst(BaseFtpClient client) {
			var serverType = client.ServerType;

			// detect server type
			if (serverType == FtpServer.Unknown) {

				// try to detect any of the servers
				foreach (var server in FtpHandlerIndex.AllServers) {
					if (server.DetectBySyst(client.SystemType)) {
						serverType = server.ToEnum();
						break;
					}
				}

				// trace it
				if (serverType != FtpServer.Unknown) {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Info, "Detected FTP server: " + serverType.ToString());
				}

			}

			return serverType;
		}

	}
}
