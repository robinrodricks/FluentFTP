using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.Remoting.Channels;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using FluentFTP;

namespace WinFormsTest
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs eaValidationEventArgs)
		{
			string ValidCert = "<insert contents of cert.txt>";
			if (eaValidationEventArgs.PolicyErrors == SslPolicyErrors.None || eaValidationEventArgs.Certificate.GetRawCertDataString() == ValidCert)
			{
				eaValidationEventArgs.Accept = true;
				
				
			}
			else
			{
				throw new Exception("Invalid certificate : " + eaValidationEventArgs.PolicyErrors);
			}
		}


		//void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
		//{
		//	File.WriteAllText(@"C:\cert.txt", e.Certificate.GetRawCertDataString());
		//}

		//void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs eaValidationEventArgs)
		//{
		//	// add logic to test if certificate is valid here
		//	eaValidationEventArgs.Accept = true;
		//}
		private void button1_Click(object sender, EventArgs e)
		{

			//FtpTrace.AddListener(new ConsoleTraceListener());

			// create an FTP client
			//FtpClient client = new FtpClient("localhost");

			// if you don't specify login credentials, we use the "anonymous" user account
			//client.Credentials = new NetworkCredential("david", "pass123");

			//client.Credentials = new NetworkCredential("anonymous", "pass123");




			//var creds = new NetworkCredential("bruce", "caroline1969");
			//FtpClient client = new FtpClient("localhost",creds);



			//var mypass = Regex.Escape("kEEam5Q?");
			var mypass = "kEEam5Q?";
			var creds = new NetworkCredential("hretail", mypass);
			FtpClient client = new FtpClient("ftp://wwwtest.myvan.descartes.com", creds);
			//client.SocketPollInterval = 1000;
			//client.ConnectTimeout = 2000;
			//client.ReadTimeout = 2000;
			//client.DataConnectionConnectTimeout = 2000;
			//client.DataConnectionReadTimeout = 2000;
			//client.SocketKeepAlive = true;




			// begin connecting to the server
			client.EncryptionMode = FtpEncryptionMode.Implicit;
			client.SslProtocols = SslProtocols.Tls12;
			client.Encoding = Encoding.UTF8;
			client.DataConnectionType = FtpDataConnectionType.PASV;






			client.ValidateCertificate += OnValidateCertificate;
			client.Connect();

		List<string> fileStrings = new List<string>();

			// get a list of files and directories in the "/htdocs" folder
			foreach (FtpListItem item in client.GetListing("/docs"))
			{

				// if this is a file
				if (item.Type == FtpFileSystemObjectType.File)
				{

					// get the file size
					long size = client.GetFileSize(item.FullName);

				}
				fileStrings.Add(item.Name);

				// get modified date/time of the file or folder
				DateTime time = client.GetModifiedTime(item.FullName);

				// calculate a hash for the file on the server side (default algorithm)
				//FtpHash hash = client.GetHash(item.FullName);
				
			}


			//// upload a file
			var filename = @"c:\temp\my file.txt";
		
			var loaded = client.UploadFile(filename, "/docs/500 31.txt");

			
			var bob = loaded;
			//// rename the uploaded file
			//client.Rename("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4");

			//// download the file again
			//client.DownloadFile(@"C:\temp\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4");

			//// delete the file
			//client.DeleteFile("/htdocs/MyVideo_2.mp4");

			//// delete a folder recursively
			//client.DeleteDirectory("/htdocs/extras/");

			//// check if a file exists
			//if (client.FileExists("/htdocs/big2.txt")) { }

			//// check if a folder exists
			//if (client.DirectoryExists("/htdocs/extras/")) { }

			//// upload a file and retry 3 times before giving up
			//client.RetryAttempts = 3;
			//client.UploadFile(@"C:\temp\MyVideo.mp4", "/htdocs/big.txt", FtpExists.Overwrite, false, FtpVerify.Retry);

			// disconnect! good bye!
			client.Disconnect();
		}
	}
}
