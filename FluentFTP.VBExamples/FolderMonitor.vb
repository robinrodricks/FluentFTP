Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports FluentFTP
Imports FluentFTP.Monitors

Namespace Examples
	Friend Module FolderMonitorExample
		Async Function DownloadStablePdfFilesAsync(ByVal token As CancellationToken) As Task
			Dim conn = New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")
			Await conn.Connect(token)

			Using monitor = New BlockingAsyncFtpMonitor(conn, New List(Of String) From {
				"path/to/folder"
			})
				monitor.PollInterval = TimeSpan.FromMinutes(5)
				monitor.WaitForUpload = True

				monitor.ChangeDetected =
					Async Function(e)
						If True Then
							For Each file In e.Added.Where(Function(x) Path.GetExtension(x) = ".pdf")
								Dim localFilePath = Path.Combine("C:\LocalFolder", Path.GetFileName(file))
								Await e.AsyncFtpClient.DownloadFile(localFilePath, file, token:=e.CancellationToken)
								Await e.AsyncFtpClient.DeleteFile(file)
							Next
						End If
					End Function

				Await monitor.Start(token)
			End Using
		End Function

	End Module
End Namespace

