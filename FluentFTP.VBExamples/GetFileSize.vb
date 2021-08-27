Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module GetFileSizeExample
		Sub GetFileSize()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				Console.WriteLine("The file size is: " & conn.GetFileSize("/full/or/relative/path/to/file"))
			End Using
		End Sub

		Async Function GetFileSizeAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)
				Console.WriteLine("The file size is: " & Await conn.GetFileSizeAsync("/full/or/relative/path/to/file", -1, token))
			End Using
		End Function
	End Module
End Namespace
