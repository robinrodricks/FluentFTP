Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP
Imports FluentFTP.Rules

Namespace Examples
	Friend Module UploadDirectoryExample
		Sub UploadDirectory()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()

				' upload a folder and all its files
				ftp.UploadDirectory("C:\website\videos\", "/public_html/videos", FtpFolderSyncMode.Update)

				' upload a folder and all its files, and delete extra files on the server
				ftp.UploadDirectory("C:\website\assets\", "/public_html/assets", FtpFolderSyncMode.Mirror)

			End Using
		End Sub

		Async Function UploadDirectoryAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.Connect(token)

				' upload a folder and all its files
				Await ftp.UploadDirectory("C:\website\videos\", "/public_html/videos", FtpFolderSyncMode.Update)

				' upload a folder and all its files, and delete extra files on the server
				Await ftp.UploadDirectory("C:\website\assets\", "/public_html/assets", FtpFolderSyncMode.Mirror)

			End Using
		End Function
	End Module
End Namespace
