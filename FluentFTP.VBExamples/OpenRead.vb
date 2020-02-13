Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module OpenReadExample
		Sub OpenRead()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' open an read-only stream to the file
				Using istream = conn.OpenRead("/full/or/relative/path/to/file")

					Try
						' istream.Position Is incremented accordingly to the reads you perform
						' istream.Length == file size if the server supports getting the file size
						' also note that file size for the same file can vary between ASCII And Binary
						' modes And some servers won't even give a file size for ASCII files! It is
						' recommended that you stick with Binary And worry about character encodings
						' on your end of the connection.
					Finally
						Console.WriteLine()
						istream.Close()
					End Try
				End Using
			End Using
		End Sub

		Async Function OpenReadAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				' open an read-only stream to the file
				Using istream = Await conn.OpenReadAsync("/full/or/relative/path/to/file", token)

					Try
						' istream.Position Is incremented accordingly to the reads you perform
						' istream.Length == file size if the server supports getting the file size
						' also note that file size for the same file can vary between ASCII And Binary
						' modes And some servers won't even give a file size for ASCII files! It is
						' recommended that you stick with Binary And worry about character encodings
						' on your end of the connection.
					Finally
						Console.WriteLine()
						istream.Close()
					End Try
				End Using
			End Using
		End Function
	End Module
End Namespace
