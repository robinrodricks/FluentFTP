Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module ConnectFTPSExample
		Sub ConnectFTPS()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.EncryptionMode = FtpEncryptionMode.Explicit
				conn.ValidateAnyCertificate = True
				conn.Connect()
			End Using
		End Sub

		Async Function ConnectFTPSAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.EncryptionMode = FtpEncryptionMode.Explicit
				conn.ValidateAnyCertificate = True
				Await conn.ConnectAsync(token)
			End Using
		End Function
	End Module
End Namespace
