using FluentFTP.Client.BaseClient;
using FluentFTP.Helpers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for OpenVMS FTP servers
	/// </summary>
	internal class OpenVmsServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.OpenVMS;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect OpenVMS server
			// Welcome message: "220 ftp.bedrock.net FTP-OpenVMS FTPD V5.5-3 (c) 2001 Process Software"
			if (message.Contains("OpenVMS FTPD")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return true if your server is detected by the given SYST response message.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public override bool DetectBySyst(string message) {

			// Detect OpenVMS server
			// SYST type: "VMS OpenVMS V8.4"
			if (message.Contains("OpenVMS")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return your FTP server's default capabilities.
		/// Used if your server does not broadcast its capabilities using the FEAT command.
		/// </summary>
		public override string[] DefaultCapabilities() {

			// OpenVMS HGFTP
			// https://gist.github.com/robinrodricks/9631f9fad3c0fc4c667adfd09bd98762

			// assume the basic features supported
			return new[] { "CWD", "DELE", "LIST", "NLST", "MKD", "MDTM", "PASV", "PORT", "PWD", "QUIT", "RNFR", "RNTO", "SITE", "STOR", "STRU", "TYPE" };

		}

		/// <summary>
		/// Return true if the path is an absolute path according to your server's convention.
		/// </summary>
		public override bool IsAbsolutePath(string path) {

			// FIX : #380 for OpenVMS absolute paths are "SYS$SYSDEVICE:[USERS.mylogin]"
			// FIX : #402 for OpenVMS absolute paths are "SYSDEVICE:[USERS.mylogin]"
			// FIX : #424 for OpenVMS absolute paths are "FTP_DEFAULT:[WAGN_IN]"
			// FIX : #454 for OpenVMS absolute paths are "TOPAS$ROOT:[000000.TUIL.YR_20.SUBLIS]"
			if (Regex.IsMatch(path, @"[A-Za-z$._]*:\\[[A-Za-z0-9$_.]*\\]")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return the default file listing parser to be used with your FTP server.
		/// </summary>
		public override FtpParser GetParser() {
			return FtpParser.VMS;
		}

		public override bool IsCustomCalculateFullFtpPath() {
			return true;
		}

		/// <summary>
		/// Get the full path of a given FTP Listing entry
		/// Return null indicates custom code decided not to handle this
		/// </summary>
		public override bool? CalculateFullFtpPath(BaseFtpClient client, string path, FtpListItem item) {
			if (path == null) {
				// check if the path is absolute
				if (IsAbsolutePath(item.Name)) {
					item.FullName = item.Name;
					item.Name = item.Name.GetFtpFileName();
				}

				return true;
			}

			// if this is a vax/openvms file listing
			// there are no slashes in the path name
			item.FullName = path + item.Name;

			return true;
		}
	}
}
