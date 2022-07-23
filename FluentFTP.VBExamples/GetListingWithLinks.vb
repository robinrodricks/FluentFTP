Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module GetListingWithLinksExample
		Sub GetListing()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' get a listing of the files with their modified date & size
				For Each item In conn.GetListing(conn.GetWorkingDirectory(), FtpListOption.Modify Or FtpListOption.Size)

					Select Case item.Type

						Case FtpObjectType.Directory
						Case FtpObjectType.File
						Case FtpObjectType.Link

							' manually dereference symbolic links
							If item.LinkTarget IsNot Nothing Then
								item.LinkObject = conn.DereferenceLink(item)

								If item.LinkObject IsNot Nothing Then
								End If
							End If

					End Select
				Next

				' get a listing of the files with their modified date & size, and automatically dereference links
				For Each item In conn.GetListing(conn.GetWorkingDirectory(), FtpListOption.Modify Or FtpListOption.Size Or FtpListOption.DerefLinks)

					' do something
					Select Case item.Type

						Case FtpObjectType.Directory
						Case FtpObjectType.File
						Case FtpObjectType.Link

					End Select
				Next
			End Using
		End Sub

		Async Function GetListingAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				' get a listing of the files with their modified date & size
				For Each item In Await conn.GetListingAsync(conn.GetWorkingDirectory(), FtpListOption.Modify Or FtpListOption.Size, token)

					Select Case item.Type

						Case FtpObjectType.Directory
						Case FtpObjectType.File
						Case FtpObjectType.Link

							' manually dereference symbolic links
							If item.LinkTarget IsNot Nothing Then
								item.LinkObject = conn.DereferenceLink(item)

								If item.LinkObject IsNot Nothing Then
								End If
							End If

					End Select
				Next

				' get a listing of the files with their modified date & size, and automatically dereference links
				For Each item In Await conn.GetListingAsync(conn.GetWorkingDirectory(), FtpListOption.Modify Or FtpListOption.Size Or FtpListOption.DerefLinks, token)

					' do something
					Select Case item.Type

						Case FtpObjectType.Directory
						Case FtpObjectType.File
						Case FtpObjectType.Link

					End Select
				Next
			End Using
		End Function
	End Module
End Namespace
