Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module CreateDirectoryExample
		Sub CreateDirectory()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				conn.CreateDirectory("/test/path/that/should/be/created", True)
			End Using
		End Sub

		Async Function CreateDirectoryAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)
				Await conn.CreateDirectoryAsync("/test/path/that/should/be/created", True, token)
			End Using
		End Function
	End Module
End Namespace
