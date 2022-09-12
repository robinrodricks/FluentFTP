using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Rules;

namespace Examples {

	internal static class UploadDirectoryWithRulesExample {

		public static void UploadDirectoryWithRules() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();

				
				// upload only PDF files under 1 GB from a folder, by using the rule engine
				var rules = new List<FtpRule>{
				   new FtpFileExtensionRule(true, new List<string>{ "pdf" }),  // only allow PDF files
				   new FtpSizeRule(FtpOperator.LessThan, 1000000000)           // only allow files <1 GB
				};
				ftp.UploadDirectory(@"C:\website\attachments\", @"/public_html/attachments",
					FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules);


				// upload all files from a folder, but skip the sub-directories named `.git`, `.svn`, `node_modules` etc
				var rules2 = new List<FtpRule>{
				   new FtpFolderNameRule(false, FtpFolderNameRule.CommonBlacklistedFolders),
				};
				ftp.UploadDirectory(@"C:\project\src\", @"/project/src",
					FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules2);

			}
		}

		public static async Task UploadDirectoryWithRulesAsync() {
			var token = new CancellationToken();
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.ConnectAsync(token);

				
				// upload only PDF files under 1 GB from a folder, by using the rule engine
				var rules = new List<FtpRule>{
				   new FtpFileExtensionRule(true, new List<string>{ "pdf" }),  // only allow PDF files
				   new FtpSizeRule(FtpOperator.LessThan, 1000000000)           // only allow files <1 GB
				};
				await ftp.UploadDirectoryAsync(@"C:\website\attachments\", @"/public_html/attachments",
					FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules, token: token);


				// upload all files from a folder, but skip the sub-directories named `.git`, `.svn`, `node_modules` etc
				var rules2 = new List<FtpRule>{
				   new FtpFolderNameRule(false, FtpFolderNameRule.CommonBlacklistedFolders),
				};
				await ftp.UploadDirectoryAsync(@"C:\project\src\", @"/project/src",
					FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules2, token: token);

			}
		}

	}
}