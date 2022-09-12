Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module GetListingExample
		Sub GetListing()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()

				' get a recursive listing of the files & folders in a specific folder
				For Each item In conn.GetListing("/htdocs", FtpListOption.Recursive)

					Select Case item.Type

						Case FtpObjectType.Directory
							Console.WriteLine("Directory!  " & item.FullName)
							Console.WriteLine("Modified date:  " & conn.GetModifiedTime(item.FullName))

						Case FtpObjectType.File
							Console.WriteLine("File!  " & item.FullName)
							Console.WriteLine("File size:  " & conn.GetFileSize(item.FullName))
							Console.WriteLine("Modified date:  " & conn.GetModifiedTime(item.FullName))
							Console.WriteLine("Chmod:  " & conn.GetChmod(item.FullName))

						Case FtpObjectType.Link
					End Select
				Next
			End Using
		End Sub

		Async Function GetListingAsync() As Task
			Dim token = New CancellationToken()

			Using conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.Connect(token)

				' get a recursive listing of the files & folders in a specific folder
				For Each item In Await conn.GetListing("/htdocs", FtpListOption.Recursive, token)

					Select Case item.Type

						Case FtpObjectType.Directory
							Console.WriteLine("Directory!  " & item.FullName)
							Console.WriteLine("Modified date:  " & Await conn.GetModifiedTime(item.FullName, token))

						Case FtpObjectType.File
							Console.WriteLine("File!  " & item.FullName)
							Console.WriteLine("File size:  " & Await conn.GetFileSize(item.FullName, -1, token))
							Console.WriteLine("Modified date:  " & Await conn.GetModifiedTime(item.FullName, token))
							Console.WriteLine("Chmod:  " & Await conn.GetChmod(item.FullName, token))

						Case FtpObjectType.Link
					End Select
				Next
			End Using
		End Function
	End Module
End Namespace
