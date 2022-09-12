Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module ConnectExample
		Sub Connect()
			Using conn = New FtpClient()
				conn.Host = "localhost"
				conn.Credentials = New NetworkCredential("ftptest", "ftptest")
				conn.Connect()
			End Using
		End Sub

		Sub ConnectAlt()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
			End Using
		End Sub

		Async Function ConnectAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient()
				conn.Host = "localhost"
				conn.Credentials = New NetworkCredential("ftptest", "ftptest")
				Await conn.Connect(token)
			End Using
		End Function

		Async Function ConnectAsyncAlt() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)
			End Using
		End Function
	End Module
End Namespace
