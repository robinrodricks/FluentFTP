using System;
using System.Collections.Generic;
using System.Linq;
using FluentFTP.Rules;
using FluentFTP.Helpers;


namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Make a list of subdirectories to transfer
		/// </summary>
		/// <param name="sourceFolder"></param>
		/// <param name="remoteFolder"></param>
		/// <param name="rules"></param>
		/// <param name="results"></param>
		/// <param name="dirListing"></param>
		/// <returns></returns>
		protected List<FtpResult> GetSubDirectoriesToTransfer(string sourceFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, string[] dirListing) {

			var dirsToTransfer = new List<FtpResult>();

			foreach (var sourceFile in dirListing) {

				// calculate the local path
				var relativePath = sourceFile.Replace(sourceFolder, "").EnsurePostfix("/");
				var remoteFile = remoteFolder + relativePath;

				// create the result object
				var result = new FtpResult {
					Type = FtpObjectType.Directory,
					Size = 0,
					Name = sourceFile.GetFtpDirectoryName(),
					RemotePath = remoteFile,
					LocalPath = sourceFile,
					IsDownload = false,
				};

				// record the folder
				results.Add(result);

				// skip transferring the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}

				dirsToTransfer.Add(result);
			}

			return dirsToTransfer;
		}

	}
}