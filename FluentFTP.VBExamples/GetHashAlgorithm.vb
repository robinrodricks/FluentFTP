Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module GetHashAlgorithmExample
		Sub GetHashAlgorithm()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				Console.WriteLine("The server is using the following algorithm for computing hashes: " & conn.GetHashAlgorithm())
			End Using
		End Sub

		Async Function GetHashAlgorithmAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)
				Console.WriteLine("The server is using the following algorithm for computing hashes: " & Await conn.GetHashAlgorithmAsync())
			End Using
		End Function
	End Module
End Namespace
