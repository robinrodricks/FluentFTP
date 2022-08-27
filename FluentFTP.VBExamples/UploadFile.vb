Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module UploadFileExample
		Sub UploadFile()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()

				' upload a file to an existing FTP directory
				ftp.UploadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md")

				' upload a file And ensure the FTP directory Is created on the server
				ftp.UploadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, True)

				' upload a file And ensure the FTP directory Is created on the server, verify the file after upload
				ftp.UploadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, True, FtpVerify.Retry)

			End Using
		End Sub

		Async Function UploadFileAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.Connect(token)

				' upload a file to an existing FTP directory
				Await ftp.UploadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md")

				' upload a file And ensure the FTP directory Is created on the server
				Await ftp.UploadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, True)

				' upload a file And ensure the FTP directory Is created on the server, verify the file after upload
				Await ftp.UploadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, True, FtpVerify.Retry)

			End Using
		End Function
	End Module
End Namespace
