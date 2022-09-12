Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP
Imports FluentFTP.Rules

Namespace Examples
	Friend Module DownloadDirectoryExample
		Sub DownloadDirectory()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()

				' download a folder And all its files
				ftp.DownloadDirectory("C:\website\logs\", "/public_html/logs", FtpFolderSyncMode.Update)

				' download a folder And all its files, And delete extra files on disk
				ftp.DownloadDirectory("C:\website\dailybackup\", "/public_html/", FtpFolderSyncMode.Mirror)

			End Using
		End Sub

		Async Function DownloadDirectoryAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.Connect(token)

				' download a folder And all its files
				Await ftp.DownloadDirectory("C:\website\logs\", "/public_html/logs", FtpFolderSyncMode.Update)

				' download a folder And all its files, And delete extra files on disk
				Await ftp.DownloadDirectory("C:\website\dailybackup\", "/public_html/", FtpFolderSyncMode.Mirror)

			End Using
		End Function
	End Module
End Namespace
