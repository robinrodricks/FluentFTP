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

			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				Await conn.ConnectAsync(token)

				' get a recursive listing of the files & folders in a specific folder
				For Each item In Await conn.GetListingAsync("/htdocs", FtpListOption.Recursive, token)

					Select Case item.Type

						Case FtpObjectType.Directory
							Console.WriteLine("Directory!  " & item.FullName)
							Console.WriteLine("Modified date:  " & Await conn.GetModifiedTimeAsync(item.FullName, token))

						Case FtpObjectType.File
							Console.WriteLine("File!  " & item.FullName)
							Console.WriteLine("File size:  " & Await conn.GetFileSizeAsync(item.FullName, -1, token))
							Console.WriteLine("Modified date:  " & Await conn.GetModifiedTimeAsync(item.FullName, token))
							Console.WriteLine("Chmod:  " & Await conn.GetChmodAsync(item.FullName, token))

						Case FtpObjectType.Link
					End Select
				Next
			End Using
		End Function
	End Module
End Namespace
