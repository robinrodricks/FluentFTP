Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module DownloadFilesExample
		Sub DownloadFiles()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()

				' download many files, skip if they already exist on disk
				ftp.DownloadFiles("D:\Drivers\test\", {
					"/public_html/temp/file0.exe",
					"/public_html/temp/file1.exe",
					"/public_html/temp/file2.exe",
					"/public_html/temp/file3.exe",
					"/public_html/temp/file4.exe"
				}, FtpLocalExists.Skip)

			End Using
		End Sub

		Async Function DownloadFilesAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.Connect(token)

				' download many files, skip if they already exist on disk
				Await ftp.DownloadFiles("D:\Drivers\test\", {
					"/public_html/temp/file0.exe",
					"/public_html/temp/file1.exe",
					"/public_html/temp/file2.exe",
					"/public_html/temp/file3.exe",
					"/public_html/temp/file4.exe"
				}, FtpLocalExists.Skip)

			End Using
		End Function
	End Module
End Namespace
