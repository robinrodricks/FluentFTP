Imports System
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP

Namespace Examples
	Friend Module QuickStart
		Sub Example()
			Dim client = New FtpClient("123.123.123.123", "david", "pass123")
			client.AutoConnect()

			For Each item As FtpListItem In client.GetListing("/htdocs")

				If item.Type = FtpObjectType.File Then
					Dim size As Long = client.GetFileSize(item.FullName)
					Dim hash As FtpHash = client.GetChecksum(item.FullName)
				End If

				Dim time As DateTime = client.GetModifiedTime(item.FullName)
			Next

			client.UploadFile("C:\MyVideo.mp4", "/htdocs/MyVideo.mp4")
			client.MoveFile("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4")
			client.DownloadFile("C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4")

			If client.CompareFile("C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4") = FtpCompareResult.Equal Then
			End If

			client.DeleteFile("/htdocs/MyVideo_2.mp4")
			client.UploadDirectory("C:\website\videos\", "/public_html/videos", FtpFolderSyncMode.Update)
			client.UploadDirectory("C:\website\assets\", "/public_html/assets", FtpFolderSyncMode.Mirror)
			client.DownloadDirectory("C:\website\logs\", "/public_html/logs", FtpFolderSyncMode.Update)
			client.DownloadDirectory("C:\website\dailybackup\", "/public_html/", FtpFolderSyncMode.Mirror)
			client.DeleteDirectory("/htdocs/extras/")

			If client.FileExists("/htdocs/big2.txt") Then
			End If

			If client.DirectoryExists("/htdocs/extras/") Then
			End If

			client.Config.RetryAttempts = 3
			client.UploadFile("C:\MyVideo.mp4", "/htdocs/big.txt", FtpRemoteExists.Overwrite, False, FtpVerify.Retry)
			client.Disconnect()
		End Sub

		Async Function ExampleAsync() As Task
			Dim client = New AsyncFtpClient("123.123.123.123", "david", "pass123")
			Await client.AutoConnect()

			For Each item As FtpListItem In Await client.GetListing("/htdocs")

				If item.Type = FtpObjectType.File Then
					Dim size As Long = Await client.GetFileSize(item.FullName)
					Dim hash As FtpHash = Await client.GetChecksum(item.FullName)
				End If

				Dim time As DateTime = Await client.GetModifiedTime(item.FullName)
			Next

			Await client.UploadFile("C:\MyVideo.mp4", "/htdocs/MyVideo.mp4")
			Await client.MoveFile("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4")
			Await client.DownloadFile("C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4")

			If Await client.CompareFile("C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4") = FtpCompareResult.Equal Then
			End If

			Await client.DeleteFile("/htdocs/MyVideo_2.mp4")
			Await client.UploadDirectory("C:\website\videos\", "/public_html/videos", FtpFolderSyncMode.Update)
			Await client.UploadDirectory("C:\website\assets\", "/public_html/assets", FtpFolderSyncMode.Mirror)
			Await client.DownloadDirectory("C:\website\logs\", "/public_html/logs", FtpFolderSyncMode.Update)
			Await client.DownloadDirectory("C:\website\dailybackup\", "/public_html/", FtpFolderSyncMode.Mirror)
			Await client.DeleteDirectory("/htdocs/extras/")

			If Await client.FileExists("/htdocs/big2.txt") Then
			End If

			If Await client.DirectoryExists("/htdocs/extras/") Then
			End If

			client.Config.RetryAttempts = 3
			Await client.UploadFile("C:\MyVideo.mp4", "/htdocs/big.txt", FtpRemoteExists.Overwrite, False, FtpVerify.Retry)
			Await client.Disconnect()
		End Function
	End Module
End Namespace
