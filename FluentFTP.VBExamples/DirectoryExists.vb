Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module DirectoryExistsExample
		Sub DirectoryExists()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				If conn.DirectoryExists("/full/or/relative/path") Then
					' do something
				End If

			End Using
		End Sub

		Async Function DirectoryExistsAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)

				If Await conn.DirectoryExists("/full/or/relative/path") Then
					' do something
				End If

			End Using
		End Function
	End Module
End Namespace
