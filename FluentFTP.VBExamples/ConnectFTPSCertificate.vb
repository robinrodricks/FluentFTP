Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module ConnectFTPSCertificateExample
		Sub ConnectFTPSCertificate()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.EncryptionMode = FtpEncryptionMode.Explicit
				AddHandler conn.ValidateCertificate, New FtpSslValidation(AddressOf OnValidateCertificate)
				conn.Connect()
			End Using
		End Sub

		Async Function ConnectFTPSCertificateAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.EncryptionMode = FtpEncryptionMode.Explicit
				AddHandler conn.ValidateCertificate, New FtpSslValidation(AddressOf OnValidateCertificate)
				Await conn.ConnectAsync(token)
			End Using
		End Function

		Private Sub OnValidateCertificate(ByVal control As FtpClient, ByVal e As FtpSslValidationEventArgs)
			If e.PolicyErrors <> System.Net.Security.SslPolicyErrors.None Then
				' invalid cert, do you want to accept it?
				' e.Accept = True
			Else
				e.Accept = True
			End If
		End Sub
	End Module
End Namespace
