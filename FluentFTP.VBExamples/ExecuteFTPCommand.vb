Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP
Imports FluentFTP.Exceptions

Namespace Examples
	Friend Module ExecuteFTPCommandExample
		Sub Execute()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				Dim reply As FtpReply

				If Not (CSharpImpl.__Assign(reply, conn.Execute("SITE CHMOD 640 FOO.TXT"))).Success Then
					Throw New FtpCommandException(reply)
				End If
			End Using
		End Sub

		Async Function ExecuteAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)
				Dim reply As FtpReply

				If Not (CSharpImpl.__Assign(reply, Await conn.Execute("SITE CHMOD 640 FOO.TXT", token))).Success Then
					Throw New FtpCommandException(reply)
				End If
			End Using
		End Function

		Private Class CSharpImpl
			<Obsolete("Please refactor calling code to use normal Visual Basic assignment")>
			Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
				target = value
				Return value
			End Function
		End Class
	End Module
End Namespace
