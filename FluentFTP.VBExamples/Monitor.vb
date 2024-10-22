Imports System.IO
Imports System.Threading
Imports FluentFTP
Imports FluentFTP.Monitors

Namespace Examples
	Friend Module MonitorExample 

		Async Function DownloadStablePdfFilesAsync(token As CancellationToken) As Task
			Dim conn As New AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")

			Using monitor As new AsyncFtpMonitor(conn, "path/to/folder")

				monitor.PollInterval = TimeSpan.FromMinutes(5)
				monitor.WaitTillFileFullyUploaded = True
				monitor.UnstablePollInterval = TimeSpan.FromSeconds(10)
	
				monitor.SetHandler(Async Function(source, e ) 
					For Each file In e.Added _
					                  .Where(Function(x) x.Type = FtpObjectType.File) _
					                  .Where(Function(x) Path.GetExtension(x.Name) = ".pdf")
						Dim localFilePath = Path.Combine("C:\LocalFolder", file.Name)
						Await e.FtpClient.DownloadFile(localFilePath, file.FullName)
						Await e.FtpClient.DeleteFile(file.FullName)
					Next
				End Function)

				Await conn.Connect()
				Await monitor.Start(token)
			End Using
		End Function
	End Module
End Namespace
