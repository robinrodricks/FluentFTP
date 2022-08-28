using FluentFTP.Helpers;
using System.Text.RegularExpressions;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Gets the current working directory
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		string IInternalFtpClient.GetWorkingDirectoryInternal() {

			// this case occurs immediately after connection and after the working dir has changed
			if (Status.LastWorkingDir == null) {
				ReadCurrentWorkingDirectory();
			}

			return Status.LastWorkingDir;
		}


		protected FtpReply ReadCurrentWorkingDirectory() {
			FtpReply reply;

			lock (m_lock) {
				// read the absolute path of the current working dir
				if (!(reply = ((IInternalFtpClient)this).ExecuteInternal("PWD")).Success) {
					throw new FtpCommandException(reply);
				}
			}

			// cache the last working dir
			Status.LastWorkingDir = ParseWorkingDirectory(reply);
			return reply;
		}

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