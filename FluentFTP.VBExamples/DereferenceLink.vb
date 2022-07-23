Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module DereferenceLink
		Sub DereferenceLinkExample()
			Using conn = New FtpClient("127.0.0.1", "ftptest", "ftptest")
				conn.Connect()
				conn.MaximumDereferenceCount = 20

				For Each item In conn.GetListing(Nothing, FtpListOption.ForceList Or FtpListOption.Modify)
					Console.WriteLine(item)

					If item.Type = FtpObjectType.Link AndAlso item.LinkTarget IsNot Nothing Then
						item.LinkObject = conn.DereferenceLink(item)

						If item.LinkObject IsNot Nothing Then
							Console.WriteLine(item.LinkObject)
						End If
					End If
				Next

				For Each item In conn.GetListing(Nothing, FtpListOption.ForceList Or FtpListOption.Modify Or FtpListOption.DerefLinks)
					Console.WriteLine(item)

					If item.Type = FtpObjectType.Link AndAlso item.LinkObject IsNot Nothing Then
						Console.WriteLine(item.LinkObject)
					End If
				Next
			End Using
		End Sub
	End Module
End Namespace
