Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP
Imports FluentFTP.Rules

Namespace Examples
	Friend Module DownloadDirectoryWithRulesExample
		Sub DownloadDirectoryWithRules()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()


				' download only PDF files under 1 GB from a folder, by using the rule engine
				Dim rules = New List(Of FtpRule) From {
					New FtpFileExtensionRule(True, New List(Of String) From { ' only allow PDF files
						"pdf"
					}),
					New FtpSizeRule(FtpOperator.LessThan, 1000000000) ' only allow files <1 GB
				}
				ftp.DownloadDirectory("C:\website\attachments\", "/public_html/attachments", FtpFolderSyncMode.Update, FtpLocalExists.Skip, FtpVerify.None, rules)


				' download all files from a folder, but skip the sub-directories named `.git`, `.svn`, `node_modules` etc
				Dim rules2 = New List(Of FtpRule) From {
					New FtpFolderNameRule(False, FtpFolderNameRule.CommonBlacklistedFolders)
				}
				ftp.DownloadDirectory("C:\project\src\", "/project/src", FtpFolderSyncMode.Update, FtpLocalExists.Skip, FtpVerify.None, rules2)


			End Using
		End Sub

		Async Function DownloadDirectoryWithRulesAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.Connect(token)


				' download only PDF files under 1 GB from a folder, by using the rule engine
				Dim rules = New List(Of FtpRule) From {
					New FtpFileExtensionRule(True, New List(Of String) From {' only allow PDF files
						"pdf"
					}),
					New FtpSizeRule(FtpOperator.LessThan, 1000000000) ' only allow files <1 GB
				}
				Await ftp.DownloadDirectory("C:\website\attachments\", "/public_html/attachments", FtpFolderSyncMode.Update, FtpLocalExists.Skip, FtpVerify.None, rules)


				' download all files from a folder, but skip the sub-directories named `.git`, `.svn`, `node_modules` etc
				Dim rules2 = New List(Of FtpRule) From {
					New FtpFolderNameRule(False, FtpFolderNameRule.CommonBlacklistedFolders)
				}
				Await ftp.DownloadDirectory("C:\project\src\", "/project/src", FtpFolderSyncMode.Update, FtpLocalExists.Skip, FtpVerify.None, rules2)


			End Using
		End Function
	End Module
End Namespace
