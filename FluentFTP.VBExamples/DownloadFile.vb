Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module DownloadFileExample
		Sub DownloadFile()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()

				' download a file and ensure the local directory is created
				ftp.DownloadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md")

				' download a file and ensure the local directory is created, verify the file after download
				ftp.DownloadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Overwrite, FtpVerify.Retry)

			End Using
		End Sub

		Async Function DownloadFileAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.ConnectAsync(token)

				' download a file and ensure the local directory is created
				Await ftp.DownloadFileAsync("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md")

				' download a file and ensure the local directory is created, verify the file after download
				Await ftp.DownloadFileAsync("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Overwrite, FtpVerify.Retry)

			End Using
		End Function
	End Module
End Namespace
