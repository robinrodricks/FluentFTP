Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module DeleteDirectoryExample
		Sub DeleteDirectory()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' Remove the directory And all files And subdirectories inside it
				conn.DeleteDirectory("/path/to/directory")

			End Using
		End Sub

		Async Function DeleteDirectoryAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)

				' Remove the directory And all files And subdirectories inside it
				Await conn.DeleteDirectory("/path/to/directory", token)

			End Using
		End Function
	End Module
End Namespace
