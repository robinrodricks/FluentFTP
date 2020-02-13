Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP
Imports FluentFTP.Rules

Namespace Examples
	Friend Module UploadDirectoryWithRulesExample
		Sub UploadDirectoryWithRules()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()


				' upload only PDF files under 1 GB from a folder, by using the rule engine
				Dim rules = New List(Of FtpRule) From {
					New FtpFileExtensionRule(True, New List(Of String) From { ' only allow PDF files
						"pdf"
					}),
					New FtpSizeRule(FtpOperator.LessThan, 1000000000) ' only allow files <1 GB
				}
				ftp.UploadDirectory("C:\website\attachments\", "/public_html/attachments", FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules)


				' upload all files from a folder, but skip the sub-directories named `.git`, `.svn`, `node_modules` etc
				Dim rules2 = New List(Of FtpRule) From {
					New FtpFolderNameRule(False, FtpFolderNameRule.CommonBlacklistedFolders)
				}
				ftp.UploadDirectory("C:\project\src\", "/project/src", FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules2)


			End Using
		End Sub

		Async Function UploadDirectoryWithRulesAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.ConnectAsync(token)


				' upload only PDF files under 1 GB from a folder, by using the rule engine
				Dim rules = New List(Of FtpRule) From {
					New FtpFileExtensionRule(True, New List(Of String) From {' only allow PDF files
						"pdf"
					}),
					New FtpSizeRule(FtpOperator.LessThan, 1000000000)' only allow files <1 GB
				}
				Await ftp.UploadDirectoryAsync("C:\website\attachments\", "/public_html/attachments", FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules)


				' upload all files from a folder, but skip the sub-directories named `.git`, `.svn`, `node_modules` etc
				Dim rules2 = New List(Of FtpRule) From {
					New FtpFolderNameRule(False, FtpFolderNameRule.CommonBlacklistedFolders)
				}
				Await ftp.UploadDirectoryAsync("C:\project\src\", "/project/src", FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, rules2)


			End Using
		End Function
	End Module
End Namespace
