Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module OpenWriteExample
		Sub OpenWrite()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' open an write-only stream to the file
				Using ostream = conn.OpenWrite("/full/or/relative/path/to/file")

					Try
						' ostream.Position Is incremented accordingly to the writes you perform
					Finally
						ostream.Close()
					End Try
				End Using
			End Using
		End Sub

		Async Function OpenWriteAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				' open an write-only stream to the file
				Using ostream = Await conn.OpenWriteAsync("/full/or/relative/path/to/file", token)

					Try
						' ostream.Position Is incremented accordingly to the writes you perform
					Finally
						ostream.Close()
					End Try
				End Using
			End Using
		End Function
	End Module
End Namespace
