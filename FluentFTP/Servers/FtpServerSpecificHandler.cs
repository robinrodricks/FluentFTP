using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Authentication;
using FluentFTP.Servers.Handlers;
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Servers {
	internal static class FtpServerSpecificHandler {

		internal static List<FtpBaseServer> AllServers = new List<FtpBaseServer> {
			new BFtpdServer(),
			new CerberusServer(),
			new CrushFtpServer(),
			new FileZillaServer(),
			new Ftp2S3GatewayServer(),
			new GlFtpdServer(),
			new GlobalScapeEftServer(),
			new HomegateFtpServer(),
			new IbmZosFtpServer(),
			new NonStopTandemServer(),
			new OpenVmsServer(),
			new ProFtpdServer(),
			new PureFtpdServer(),
			new ServUServer(),
			new SolarisFtpServer(),
			new VsFtpdServer(),
			new WindowsCeServer(),
			new WindowsIisServer(),
			new WuFtpdServer(),
			new XLightServer(),
		};

		#region Working Connection Profiles

		/// <summary>
		/// Return a known working connection profile from the host/port combination.
		/// </summary>
		public static FtpProfile GetWorkingProfileFromHost(string Host, int Port) {

			// Azure App Services / Azure Websites
			if (Host.IndexOf("ftp.azurewebsites.windows.net", StringComparison.OrdinalIgnoreCase) > -1) {

				return new FtpProfile {
					Protocols = SslProtocols.Tls,
					DataConnection = FtpDataConnectionType.PASV,
					RetryAttempts = 5,
					SocketPollInterval = 1000,
					Timeout = 2000,
				};

			}

			return null;
		}

		#endregion

		#region Detect Server

		/// <summary>
		/// Detect the FTP Server based on the welcome message sent by the server after getting the 220 connection command.
		/// Its the primary method.
		/// </summary>
		public static FtpServer DetectFtpServer(FtpClient client, FtpReply HandshakeReply) {
			var serverType = client.ServerType;

			if (HandshakeReply.Success && (HandshakeReply.Message != null || HandshakeReply.InfoMessages != null)) {
				var message = (HandshakeReply.Message ?? "") + (HandshakeReply.InfoMessages ?? "");

				// try to detect any of the servers
				foreach (var server in AllServers) {
					if (server.DetectedByWelcome(message)) {
						serverType = server.ToEnum();
						break;
					}
				}

				// trace it
				if (serverType != FtpServer.Unknown) {
					client.LogLine(FtpTraceLevel.Info, "Status:   Detected FTP server: " + serverType.ToString());
				}
			}

			return serverType;
		}

		/// <summary>
		/// Detect the FTP Server based on the response to the SYST connection command.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public static FtpOperatingSystem DetectFtpOSBySyst(FtpClient client) {
			var serverOS = client.ServerOS;

			// detect OS type
			var system = client.SystemType.ToUpper();

			if (system.StartsWith("WINDOWS")) {
				// Windows OS
				serverOS = FtpOperatingSystem.Windows;
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
			else if (system.Contains("Z/OS")) {
				// IBM OS/400
				// Syst message: "215 MVS is the operating system of this server. FTP Server is running on z/OS."
				serverOS = FtpOperatingSystem.IBMzOS;
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
		public static FtpServer DetectFtpServerBySyst(FtpClient client) {
			var serverType = client.ServerType;

			// detect server type
			if (serverType == FtpServer.Unknown) {

				// try to detect any of the servers
				foreach (var server in AllServers) {
					if (server.DetectedBySyst(client.SystemType)) {
						serverType = server.ToEnum();
						break;
					}
				}

				// trace it
				if (serverType != FtpServer.Unknown) {
					client.LogStatus(FtpTraceLevel.Info, "Detected FTP server: " + serverType.ToString());
				}

			}

			return serverType;
		}

		#endregion

		#region Detect Capabilities

		/// <summary>
		/// Populates the capabilities flags based on capabilities given in the list of strings.
		/// </summary>
		public static void GetFeatures(FtpClient client, List<FtpCapability> m_capabilities, ref FtpHashAlgorithm m_hashAlgorithms, string[] features) {
			foreach (var feat in features) {
				var featName = feat.Trim().ToUpper();

				if (featName.StartsWith("MLST") || featName.StartsWith("MLSD")) {
					m_capabilities.AddOnce(FtpCapability.MLSD);
				}
				else if (featName.StartsWith("MDTM")) {
					m_capabilities.AddOnce(FtpCapability.MDTM);
				}
				else if (featName.StartsWith("REST STREAM")) {
					m_capabilities.AddOnce(FtpCapability.REST);
				}
				else if (featName.StartsWith("SIZE")) {
					m_capabilities.AddOnce(FtpCapability.SIZE);
				}
				else if (featName.StartsWith("UTF8")) {
					m_capabilities.AddOnce(FtpCapability.UTF8);
				}
				else if (featName.StartsWith("PRET")) {
					m_capabilities.AddOnce(FtpCapability.PRET);
				}
				else if (featName.StartsWith("MFMT")) {
					m_capabilities.AddOnce(FtpCapability.MFMT);
				}
				else if (featName.StartsWith("MFCT")) {
					m_capabilities.AddOnce(FtpCapability.MFCT);
				}
				else if (featName.StartsWith("MFF")) {
					m_capabilities.AddOnce(FtpCapability.MFF);
				}
				else if (featName.StartsWith("MMD5")) {
					m_capabilities.AddOnce(FtpCapability.MMD5);
				}
				else if (featName.StartsWith("XMD5")) {
					m_capabilities.AddOnce(FtpCapability.XMD5);
				}
				else if (featName.StartsWith("XCRC")) {
					m_capabilities.AddOnce(FtpCapability.XCRC);
				}
				else if (featName.StartsWith("XSHA1")) {
					m_capabilities.AddOnce(FtpCapability.XSHA1);
				}
				else if (featName.StartsWith("XSHA256")) {
					m_capabilities.AddOnce(FtpCapability.XSHA256);
				}
				else if (featName.StartsWith("XSHA512")) {
					m_capabilities.AddOnce(FtpCapability.XSHA512);
				}
				else if (featName.StartsWith("EPSV")) {
					m_capabilities.AddOnce(FtpCapability.EPSV);
				}
				else if (featName.StartsWith("CPSV")) {
					m_capabilities.AddOnce(FtpCapability.CPSV);
				}
				else if (featName.StartsWith("NOOP")) {
					m_capabilities.AddOnce(FtpCapability.NOOP);
				}
				else if (featName.StartsWith("CLNT")) {
					m_capabilities.AddOnce(FtpCapability.CLNT);
				}
				else if (featName.StartsWith("SSCN")) {
					m_capabilities.AddOnce(FtpCapability.SSCN);
				}
				else if (featName.StartsWith("SITE MKDIR")) {
					m_capabilities.AddOnce(FtpCapability.SITE_MKDIR);
				}
				else if (featName.StartsWith("SITE RMDIR")) {
					m_capabilities.AddOnce(FtpCapability.SITE_RMDIR);
				}
				else if (featName.StartsWith("SITE UTIME")) {
					m_capabilities.AddOnce(FtpCapability.SITE_UTIME);
				}
				else if (featName.StartsWith("SITE SYMLINK")) {
					m_capabilities.AddOnce(FtpCapability.SITE_SYMLINK);
				}
				else if (featName.StartsWith("AVBL")) {
					m_capabilities.AddOnce(FtpCapability.AVBL);
				}
				else if (featName.StartsWith("THMB")) {
					m_capabilities.AddOnce(FtpCapability.THMB);
				}
				else if (featName.StartsWith("RMDA")) {
					m_capabilities.AddOnce(FtpCapability.RMDA);
				}
				else if (featName.StartsWith("DSIZ")) {
					m_capabilities.AddOnce(FtpCapability.DSIZ);
				}
				else if (featName.StartsWith("HOST")) {
					m_capabilities.AddOnce(FtpCapability.HOST);
				}
				else if (featName.StartsWith("CCC")) {
					m_capabilities.AddOnce(FtpCapability.CCC);
				}
				else if (featName.StartsWith("MODE Z")) {
					m_capabilities.AddOnce(FtpCapability.MODE_Z);
				}
				else if (featName.StartsWith("LANG")) {
					m_capabilities.AddOnce(FtpCapability.LANG);
				}
				else if (featName.StartsWith("HASH")) {
					Match m;

					m_capabilities.AddOnce(FtpCapability.HASH);

					if ((m = Regex.Match(featName, @"^HASH\s+(?<types>.*)$")).Success) {
						foreach (var type in m.Groups["types"].Value.Split(';')) {
							switch (type.ToUpper().Trim()) {
								case "SHA-1":
								case "SHA-1*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA1;
									break;

								case "SHA-256":
								case "SHA-256*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA256;
									break;

								case "SHA-512":
								case "SHA-512*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA512;
									break;

								case "MD5":
								case "MD5*":
									m_hashAlgorithms |= FtpHashAlgorithm.MD5;
									break;

								case "CRC":
								case "CRC*":
									m_hashAlgorithms |= FtpHashAlgorithm.CRC;
									break;
							}
						}
					}
				}
			}
		}

		#endregion

		#region Detect Recursive List

		/// <summary>
		/// Detect if your FTP server supports the recursive LIST command (LIST -R).
		/// If you know for sure that this is supported, return true here.
		/// </summary>
		public static bool SupportsRecursiveList(FtpClient client) {

			// Has support, per https://download.pureftpd.org/pub/pure-ftpd/doc/README
			if (client.ServerType == FtpServer.PureFTPd) {
				return true;
			}

			// Has support, per: http://www.proftpd.org/docs/howto/ListOptions.html
			if (client.ServerType == FtpServer.ProFTPD) {
				return true;
			}

			// Has support, but OFF by default, per: https://linux.die.net/man/5/vsftpd.conf
			if (client.ServerType == FtpServer.VsFTPd) {
				return false; // impossible to detect on a server-by-server basis
			}

			// No support, per: https://trac.filezilla-project.org/ticket/1848
			if (client.ServerType == FtpServer.FileZilla) {
				return false;
			}

			// No support, per: http://wu-ftpd.therockgarden.ca/man/ftpd.html
			if (client.ServerType == FtpServer.WuFTPd) {
				return false;
			}

			// Unknown, so assume server does not support recursive listing
			return false;
		}

		#endregion

		#region Assume Capabilities

		/// <summary>
		/// Assume the FTP Server's capabilities if it does not support the FEAT command.
		/// </summary>
		public static void AssumeCapabilities(FtpClient client, List<FtpCapability> m_capabilities, ref FtpHashAlgorithm m_hashAlgorithms) {

			// HP-UX version of wu-ftpd 2.6.1
			// http://nixdoc.net/man-pages/HP-UX/ftpd.1m.html
			if (client.ServerType == FtpServer.WuFTPd) {
				// assume the basic features supported
				GetFeatures(client, m_capabilities, ref m_hashAlgorithms, new[] { "ABOR", "ACCT", "ALLO", "APPE", "CDUP", "CWD", "DELE", "EPSV", "EPRT", "HELP", "LIST", "LPRT", "LPSV", "MKD", "MDTM", "MODE", "NLST", "NOOP", "PASS", "PASV", "PORT", "PWD", "QUIT", "REST", "RETR", "RMD", "RNFR", "RNTO", "SITE", "SIZE", "STAT", "STOR", "STOU", "STRU", "SYST", "TYPE" });
			}

			// OpenVMS HGFTP
			// https://gist.github.com/robinrodricks/9631f9fad3c0fc4c667adfd09bd98762
			if (client.ServerType == FtpServer.OpenVMS) {
				// assume the basic features supported
				GetFeatures(client, m_capabilities, ref m_hashAlgorithms, new[] { "CWD", "DELE", "LIST", "NLST", "MKD", "MDTM", "PASV", "PORT", "PWD", "QUIT", "RNFR", "RNTO", "SITE", "STOR", "STRU", "TYPE" });
			}

		}

		#endregion

		#region Absolute Path

		/// <summary>
		/// Checks for server-specific absolute paths
		/// </summary>
		public static bool IsAbsolutePath(FtpClient client, string path) {
			// FIX : #380 for OpenVMS absolute paths are "SYS$SYSDEVICE:[USERS.mylogin]"
			// FIX : #402 for OpenVMS absolute paths are "SYSDEVICE:[USERS.mylogin]"
			// FIX : #424 for OpenVMS absolute paths are "FTP_DEFAULT:[WAGN_IN]"
			// FIX : #454 for OpenVMS absolute paths are "TOPAS$ROOT:[000000.TUIL.YR_20.SUBLIS]"
			if (client.ServerType == FtpServer.OpenVMS) {
				if (new Regex("[A-Za-z$._]*:\\[[A-Za-z0-9$_.]*\\]").Match(path).Success) {
					return true;
				}
			}

			return false;
		}

		#endregion

		#region File Listing Parser

		public static FtpParser GetParserByServerType(FtpClient client) {

			if (client.ServerType == FtpServer.WindowsServerIIS || client.ServerType == FtpServer.WindowsCE) {
				return FtpParser.Windows;
			}

			if (client.ServerType == FtpServer.NonStopTandem) {
				return FtpParser.NonStop;
			}

			if (client.ServerType == FtpServer.OpenVMS) {
				return FtpParser.VMS;
			}

			return FtpParser.Unix;
		}

		#endregion

		#region Delete Directory

		public static bool ServerDeleteDirectory(FtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options) {

			// Support #378 - Support RMDIR command for ProFTPd
			if (deleteContents && client.HasFeature(FtpCapability.SITE_RMDIR)) {
				if ((client.Execute("SITE RMDIR " + ftppath)).Success) {
					client.LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE RMDIR command to quickly delete directory: " + ftppath);
					return true;
				}
				else {
					client.LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE RMDIR command to quickly delete directory: " + ftppath);
				}
			}

			// Support #88 - Support RMDA command for Serv-U
			if (deleteContents && client.HasFeature(FtpCapability.RMDA)) {
				if ((client.Execute("RMDA " + ftppath)).Success) {
					client.LogStatus(FtpTraceLevel.Verbose, "Used the server-specific RMDA command to quickly delete directory: " + ftppath);
					return true;
				}
				else {
					client.LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific RMDA command to quickly delete directory: " + ftppath);
				}
			}

			return false;
		}

#if ASYNC
		public static async Task<bool> ServerDeleteDirectoryAsync(FtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options, CancellationToken token) {

			// Support #378 - Support RMDIR command for ProFTPd
			if (deleteContents && client.ServerType == FtpServer.ProFTPD && client.HasFeature(FtpCapability.SITE_RMDIR)) {
				if ((await client.ExecuteAsync("SITE RMDIR " + ftppath, token)).Success) {
					client.LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE RMDIR command to quickly delete: " + ftppath);
					return true;
				}
				else {
					client.LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE RMDIR command to quickly delete: " + ftppath);
				}
			}

			return false;
		}
#endif

		#endregion

		#region Create Directory

		public static bool ServerCreateDirectory(FtpClient client, string path, string ftppath, bool force) {

			// Support #378 - Support MKDIR command for ProFTPd
			if (client.ServerType == FtpServer.ProFTPD && client.HasFeature(FtpCapability.SITE_MKDIR)) {
				if ((client.Execute("SITE MKDIR " + ftppath)).Success) {
					client.LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE MKDIR command to quickly create: " + ftppath);
					return true;
				}
				else {
					client.LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE MKDIR command to quickly create: " + ftppath);
				}
			}

			return false;
		}

#if ASYNC
		public static async Task<bool> ServerCreateDirectoryAsync(FtpClient client, string path, string ftppath, bool force, CancellationToken token) {

			// Support #378 - Support MKDIR command for ProFTPd
			if (client.ServerType == FtpServer.ProFTPD && client.HasFeature(FtpCapability.SITE_MKDIR)) {
				if ((await client.ExecuteAsync("SITE MKDIR " + ftppath, token)).Success) {
					client.LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE MKDIR command to quickly create: " + ftppath);
					return true;
				}
				else {
					client.LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE MKDIR command to quickly create: " + ftppath);
				}
			}

			return false;
		}
#endif

		#endregion

	}
}
