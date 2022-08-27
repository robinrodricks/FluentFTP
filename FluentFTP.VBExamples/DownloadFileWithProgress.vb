Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module DownloadFileWithProgressExample
		Sub DownloadFile()
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

				' download a file with progress tracking
				ftp.DownloadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Overwrite, FtpVerify.None, progress)

			End Using
		End Sub

		Async Function DownloadFileAsync() As Task
			Dim token = New CancellationToken()

			Using ftp = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await ftp.Connect(token)

				' define the progress tracking callback
				Dim progress As Progress(Of FtpProgress) = New Progress(Of FtpProgress)(
					Sub(p)
						If p.Progress = 1 Then
							' all done!
						Else
							' percent done = (p.Progress * 100)
						End If
					End Sub)

				' download a file with progress tracking
				Await ftp.DownloadFile("D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Resume, FtpVerify.None, progress, token)

			End Using
		End Function
	End Module
End Namespace
