Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module RenameExample
		Sub Rename()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' renaming a directory Is dependent on the server! if you attempt it
				' And it fails it's not because FluentFTP has a bug!
				conn.Rename("/full/or/relative/path/to/src", "/full/or/relative/path/to/dest")
			End Using
		End Sub

		Async Function RenameAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)

				' renaming a directory Is dependent on the server! if you attempt it
				' And it fails it's not because FluentFTP has a bug!
				Await conn.Rename("/full/or/relative/path/to/src", "/full/or/relative/path/to/dest", token)
			End Using
		End Function
	End Module
End Namespace
