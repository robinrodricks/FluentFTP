Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module GetModifiedTimeExample
		Sub GetModifiedTime()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				Console.WriteLine("The modified type is: " & conn.GetModifiedTime("/full/or/relative/path/to/file"))
			End Using
		End Sub

		Async Function GetModifiedTimeAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)
				Console.WriteLine("The modified type is: " & Await conn.GetModifiedTime("/full/or/relative/path/to/file", token))
			End Using
		End Function
	End Module
End Namespace
