Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module GetHashExample

		'-----------------------------------------------------------------------------------------
		' NOTE! GetChecksum automatically uses the first available hash algorithm on the server,
		'		And it should be used as far as possible instead of GetHash, GetMD5, GetSHA256...
		'-----------------------------------------------------------------------------------------

		Sub GetHash()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				If conn.HashAlgorithms <> FtpHashAlgorithm.NONE Then
					Dim hash As FtpHash
					hash = conn.GetHash("/path/to/remote/somefile.ext")

					If hash.Verify("/path/to/local/somefile.ext") Then
						Console.WriteLine("The computed hashes match!")
					End If

					If conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5) Then
						conn.SetHashAlgorithm(FtpHashAlgorithm.MD5)
						hash = conn.GetHash("/path/to/remote/somefile.ext")

						If hash.Verify("/path/to/local/somefile.ext") Then
							Console.WriteLine("The computed hashes match!")
						End If
					End If
				End If
			End Using
		End Sub

		Async Function GetHashAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				If conn.HashAlgorithms <> FtpHashAlgorithm.NONE Then
					Dim hash As FtpHash
					hash = Await conn.GetHashAsync("/path/to/remote/somefile.ext", token)

					If hash.Verify("/path/to/local/somefile.ext") Then
						Console.WriteLine("The computed hashes match!")
					End If

					If conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5) Then
						conn.SetHashAlgorithm(FtpHashAlgorithm.MD5)
						hash = Await conn.GetHashAsync("/path/to/remote/somefile.ext", token)

						If hash.Verify("/path/to/local/somefile.ext") Then
							Console.WriteLine("The computed hashes match!")
						End If
					End If
				End If
			End Using
		End Function
	End Module
End Namespace
