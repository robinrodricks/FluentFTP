Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module GetWorkingDirectoryExample
		Sub GetWorkingDirectory()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				Console.WriteLine("The working directory is: " & conn.GetWorkingDirectory())
			End Using
		End Sub

		Async Function GetWorkingDirectoryAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)
				Console.WriteLine("The working directory is: " & Await conn.GetWorkingDirectory(token))
			End Using
		End Function
	End Module
End Namespace
