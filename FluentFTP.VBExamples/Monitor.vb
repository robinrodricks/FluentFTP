Imports System.IO
Imports System.Threading
Imports FluentFTP
Imports FluentFTP.Monitors

Namespace Examples
	Friend Module MonitorExample 

		' Downloads all PDF files from a folder on an FTP server
		' when they are fully uploaded (stable)
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
						Await e.FtpClient.DownloadFile(localFilePath, file.FullName, token := e.CancellationToken)
						Await e.FtpClient.DeleteFile(file.FullName) ' don't cancel this operation
					Next
				End Function)

				Await conn.Connect(token)
				Await monitor.Start(token)
			End Using
		End Function

		' How to use the monitor in a console application
		Public Async Function MainAsync() As Task
			Dim tokenSource = New CancellationTokenSource()
			AddHandler Console.CancelKeyPress, Sub (source, e)
				e.Cancel = True ' keep running until monitor is stopped
				tokenSource.Cancel() ' stop the monitor
			End Sub

			Await DownloadStablePdfFilesAsync(tokenSource.Token)
		End Function
	End Module
End Namespace
