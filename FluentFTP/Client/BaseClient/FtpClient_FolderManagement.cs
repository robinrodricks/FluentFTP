using System;
using System.Text.RegularExpressions;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using FluentFTP.Client;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		protected string ParseWorkingDirectory(FtpReply reply) {
			Match m;

			if ((m = Regex.Match(reply.Message, "\"(?<pwd>.*)\"")).Success) {
				return m.Groups["pwd"].Value.GetFtpPath();
			}

			// check for MODCOMP ftp path mentioned in forums: https://netftp.codeplex.com/discussions/444461
			if ((m = Regex.Match(reply.Message, "PWD = (?<pwd>.*)")).Success) {
				return m.Groups["pwd"].Value.GetFtpPath();
			}

			LogStatus(FtpTraceLevel.Warn, "Failed to parse working directory from: " + reply.Message);

			return "/";
		}

	}
}