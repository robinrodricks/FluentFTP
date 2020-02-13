Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module OpenAppendExample
		Sub OpenAppend()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' open an append-only stream to the file
				Using ostream = conn.OpenAppend("/full/or/relative/path/to/file")

					Try
						' be sure to seek your output stream to the appropriate location, i.e., istream.Position
						' istream.Position Is incremented accordingly to the writes you perform
						' istream.Position == file size if the server supports getting the file size
						' also note that file size for the same file can vary between ASCII And Binary
						' modes And some servers won't even give a file size for ASCII files! It is
						' recommended that you stick with Binary And worry about character encodings
						' on your end of the connection.
					Finally
						ostream.Close()
					End Try
				End Using
			End Using
		End Sub

		Async Function OpenAppendAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				' open an append-only stream to the file
				Using ostream = Await conn.OpenAppendAsync("/full/or/relative/path/to/file", token)

					Try
						' be sure to seek your output stream to the appropriate location, i.e., istream.Position
						' istream.Position Is incremented accordingly to the writes you perform
						' istream.Position == file size if the server supports getting the file size
						' also note that file size for the same file can vary between ASCII And Binary
						' modes And some servers won't even give a file size for ASCII files! It is
						' recommended that you stick with Binary And worry about character encodings
						' on your end of the connection.
					Finally
						ostream.Close()
					End Try
				End Using
			End Using
		End Function
	End Module
End Namespace
