Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module UploadFilesExample
		Sub UploadFiles()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()

				' upload many files, skip if they already exist on server
				ftp.UploadFiles({
					"D:\Drivers\test\file0.exe",
					"D:\Drivers\test\file1.exe",
					"D:\Drivers\test\file2.exe",
					"D:\Drivers\test\file3.exe",
					"D:\Drivers\test\file4.exe"
				}, "/public_html/temp/", FtpRemoteExists.Skip)

			End Using
		End Sub

		Async Function UploadFilesAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.ConnectAsync(token)

				' upload many files, skip if they already exist on server
				Await ftp.UploadFilesAsync({
					"D:\Drivers\test\file0.exe",
					"D:\Drivers\test\file1.exe",
					"D:\Drivers\test\file2.exe",
					"D:\Drivers\test\file3.exe",
					"D:\Drivers\test\file4.exe"
				}, "/public_html/temp/", FtpRemoteExists.Skip)

			End Using
		End Function
	End Module
End Namespace
