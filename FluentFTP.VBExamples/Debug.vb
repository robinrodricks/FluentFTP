Imports System
Imports System.Diagnostics
Imports FluentFTP
Imports FluentFTP.Helpers

Namespace Examples
	Friend Module DebugExample
		Private Sub LogToConsole()
			FtpTrace.AddListener(New ConsoleTraceListener())
		End Sub

		Private Sub LogToFile()
			FtpTrace.AddListener(New TextWriterTraceListener("log_file.txt"))
		End Sub

		Private Class CustomTraceListener
			Inherits TraceListener

			Public Overrides Sub Write(ByVal message As String)
				Console.Write(message)
			End Sub

			Public Overrides Sub WriteLine(ByVal message As String)
				Console.WriteLine(message)
			End Sub
		End Class

		Private Sub LogToCustomListener()
			FtpTrace.AddListener(New CustomTraceListener())
		End Sub
	End Module
End Namespace
