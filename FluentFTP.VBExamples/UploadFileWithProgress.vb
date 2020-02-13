Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module UploadFileWithProgressExample
		Sub UploadFile()
			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				ftp.Connect()

				' define the progress tracking callback
				Dim progress As Action(Of FtpProgress) =
					Sub(ByVal p As FtpProgress)
						If p.Progress = 1 Then
							' all done!
						Else
							' percent done = (p.Progress * 100)
						End If
					End Sub

				' upload a file with progress tracking
				ftp.UploadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, False, FtpVerify.None, progress)

			End Using
		End Sub

		Async Function UploadFileAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.ConnectAsync(token)

				' define the progress tracking callback
				Dim progress As Progress(Of FtpProgress) = New Progress(Of FtpProgress)(
					Sub(p)
						If p.Progress = 1 Then
							' all done!
						Else
							' percent done = (p.Progress * 100)
						End If
					End Sub)

				' upload a file with progress tracking
				Await ftp.UploadFileAsync("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, False, FtpVerify.None, progress, token)

			End Using
		End Function
	End Module
End Namespace
