Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module SetWorkingDirectoryExample
		Sub SetWorkingDirectory()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				conn.SetWorkingDirectory("/full/or/relative/path")
			End Using
		End Sub

		Async Function SetWorkingDirectoryAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)
				Await conn.SetWorkingDirectory("/full/or/relative/path", token)
			End Using
		End Function
	End Module
End Namespace
