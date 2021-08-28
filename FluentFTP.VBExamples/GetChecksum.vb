Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Module GetChecksumExample

		'-----------------------------------------------------------------------------------------
		' NOTE! GetChecksum automatically uses the first available hash algorithm on the server,
		'		And it should be used as far as possible instead of GetHash, GetMD5, GetSHA256...
		'-----------------------------------------------------------------------------------------

		Sub GetChecksum()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' Get a hash checksum for the file
				Dim hash As FtpHash = conn.GetChecksum("/path/to/remote/file")

				' Make sure it returned a valid hash object
				If hash.IsValid Then
					If hash.Verify("/some/local/file") Then
						Console.WriteLine("The checksum's match!")
					End If
				End If

			End Using
		End Sub

		Async Function GetChecksumAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				' Get a hash checksum for the file
				Dim hash As FtpHash = Await conn.GetChecksumAsync("/path/to/remote/file", FtpHashAlgorithm.NONE, token)

				' Make sure it returned a valid hash object
				If hash.IsValid Then
					If hash.Verify("/some/local/file") Then
						Console.WriteLine("The checksum's match!")
					End If
				End If

			End Using
		End Function
	End Module
End Namespace
