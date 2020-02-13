Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module SetHashAlgorithmExample
		Sub SetHashAlgorithm()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				If conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5) Then
					conn.SetHashAlgorithm(FtpHashAlgorithm.MD5)
				End If
			End Using
		End Sub

		Async Function SetHashAlgorithmAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				If conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5) Then
					Await conn.SetHashAlgorithmAsync(FtpHashAlgorithm.MD5)
				End If
			End Using
		End Function
	End Module
End Namespace
