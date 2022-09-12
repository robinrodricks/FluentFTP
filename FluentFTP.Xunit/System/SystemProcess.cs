using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Xunit.System {
	public static class SystemProcess {

		/// <summary>
		/// Execute command-line system commands.
		/// Source: https://www.codeproject.com/Articles/25983/How-to-Execute-a-Command-in-C
		/// </summary>
		public static void Execute(string command) {
			try {
				// create the ProcessStartInfo using "cmd" as the program to be run,
				// and "/c " as the parameters.
				// Incidentally, /c tells cmd that we want it to execute the command that follows,
				// and then exit.
				var procStartInfo = new ProcessStartInfo("cmd", "/c " + command);

				// The following commands are needed to redirect the standard output.
				// This means that it will be redirected to the Process.StandardOutput StreamReader.
				procStartInfo.RedirectStandardOutput = true;
				procStartInfo.UseShellExecute = false;

				// Do not create the black window.
				procStartInfo.CreateNoWindow = true;

				// Now we create a process, assign its ProcessStartInfo and start it
				var proc = new Process();
				proc.StartInfo = procStartInfo;
				proc.Start();

				// Get the output into a string
				string result = proc.StandardOutput.ReadToEnd();

				// Display the command output.
				Console.WriteLine(result);
			}
			catch (Exception e) {
				Debug.WriteLine(e.ToString());
			}
		}
	}
}
