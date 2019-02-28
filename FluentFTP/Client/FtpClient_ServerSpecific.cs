using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
#if !CORE
using System.Web;
#endif

#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	
	public partial class FtpClient : IDisposable {

		#region Detect Server

		/// <summary>
		/// Detect the FTP Server based on the welcome message sent by the server after getting the 220 connection command.
		/// Its the primary method.
		/// </summary>
		private void DetectFtpServer() {

			if (HandshakeReply.Success && (HandshakeReply.Message != null || HandshakeReply.InfoMessages != null)) {

				string welcome = (HandshakeReply.Message ?? "") + (HandshakeReply.InfoMessages ?? "");

				// Detect Pure-FTPd server
				// Welcome message: "---------- Welcome to Pure-FTPd [privsep] [TLS] ----------"
				if (welcome.Contains("Pure-FTPd")) {
					m_serverType = FtpServer.PureFTPd;
				}

				// Detect vsFTPd server
				// Welcome message: "(vsFTPd 3.0.3)"
				else if (welcome.Contains("(vsFTPd")) {
					m_serverType = FtpServer.VsFTPd;
				}

				// Detect ProFTPd server
				// Welcome message: "ProFTPD 1.3.5rc3 Server (***) [::ffff:***]"
				else if (welcome.Contains("ProFTPD")) {
					m_serverType = FtpServer.ProFTPD;
				}

				// Detect FileZilla server
				// Welcome message: "FileZilla Server 0.9.60 beta"
				else if (welcome.Contains("FileZilla Server")) {
					m_serverType = FtpServer.FileZilla;
				}

				// Detect WuFTPd server
				// Welcome message: "FTP server (Revision 9.0 Version wuftpd-2.6.1 Mon Jun 30 09:28:28 GMT 2014) ready"
				else if (welcome.Contains(" wuftpd")) {
					m_serverType = FtpServer.WuFTPd;
				}

				// Detect GlobalScape EFT server
				// Welcome message: "EFT Server Enterprise 7.4.5.6"
				else if (welcome.Contains("EFT Server")) {
					m_serverType = FtpServer.GlobalScapeEFT;
				}

				// Detect Cerberus server
				// Welcome message: "220-Cerberus FTP Server Personal Edition"
				else if (welcome.Contains("Cerberus FTP")) {
					m_serverType = FtpServer.Cerberus;
				}

				// Detect Serv-U server
				// Welcome message: "220 Serv-U FTP Server v5.0 for WinSock ready."
				else if (welcome.Contains("Serv-U FTP")) {
					m_serverType = FtpServer.ServU;
				}

				// Detect Windows Server/IIS FTP server
				// Welcome message: "220-Microsoft FTP Service."
				else if (welcome.Contains("Microsoft FTP Service")) {
					m_serverType = FtpServer.WindowsServerIIS;
				}

				// Detect CrushFTP server
				// Welcome message: "220 CrushFTP Server Ready!"
				else if (welcome.Contains("CrushFTP Server")) {
					m_serverType = FtpServer.CrushFTP;
				}

				// Detect glFTPd server
				// Welcome message: "220 W 00 T (glFTPd 2.01 Linux+TLS) ready."
				// Welcome message: "220 <hostname> (glFTPd 2.01 Linux+TLS) ready."
				else if (welcome.Contains("glFTPd ")) {
					m_serverType = FtpServer.glFTPd;
				}

				// Detect OpenVMS server
				// Welcome message: "220 ftp.bedrock.net FTP-OpenVMS FTPD V5.5-3 (c) 2001 Process Software"
				else if (welcome.Contains("OpenVMS FTPD")) {
					m_serverType = FtpServer.OpenVMS;
				}

				// Detect Tandem/NonStop server
				// Welcome message: "220 tdm-QWERTY-fp00.itc.intranet FTP SERVER T9552H02 (Version H02 TANDEM 11SEP2008) ready."
				// Welcome message: "220 FTP SERVER T9552G08 (Version G08 TANDEM 15JAN2008) ready."
				else if (welcome.Contains("FTP SERVER ") && welcome.Contains(" TANDEM ")) {
					m_serverType = FtpServer.NonStopTandem;
				}

				// trace it
				if (m_serverType != FtpServer.Unknown) {
					this.LogLine(FtpTraceLevel.Info, "Status:   Detected FTP server: " + m_serverType.ToString());
				}

			}

		}
		/// <summary>
		/// Detect the FTP Server based on the response to the SYST connection command.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		private void DetectFtpServerBySyst() {



			// detect OS type
			var system = m_systemType.ToUpper();

			if (system.StartsWith("WINDOWS")) {

				// Windows OS
				m_serverOS = FtpOperatingSystem.Windows;

			} else if (system.Contains("UNIX") || system.Contains("AIX")) {

				// Unix OS
				m_serverOS = FtpOperatingSystem.Unix;

			} else if (system.Contains("VMS")) {

				// VMS or OpenVMS
				m_serverOS = FtpOperatingSystem.VMS;

			} else if (system.Contains("OS/400")) {

				// IBM OS/400
				m_serverOS = FtpOperatingSystem.IBMOS400;

			} else {

				// assume Unix OS
				m_serverOS = FtpOperatingSystem.Unknown;
			}



			// detect server type
			if (m_serverType == FtpServer.Unknown) {

				// Detect OpenVMS server
				// SYST type: "VMS OpenVMS V8.4"
				if (m_systemType.Contains("OpenVMS")) {
					m_serverType = FtpServer.OpenVMS;
				}

				// Detect WindowsCE server
				// SYST type: "Windows_CE version 7.0"
				if (m_systemType.Contains("Windows_CE")) {
					m_serverType = FtpServer.WindowsCE;
				}

				// trace it
				if (m_serverType != FtpServer.Unknown) {
					this.LogStatus(FtpTraceLevel.Info, "Detected FTP server: " + m_serverType.ToString());
				}

			}
		}

		#endregion

		#region Assume Capabilities

		/// <summary>
		/// Assume the FTP Server's capabilities if it does not support the FEAT command.
		/// </summary>
		private void AssumeCapabilities() {

			// HP-UX version of wu-ftpd 2.6.1
			// http://nixdoc.net/man-pages/HP-UX/ftpd.1m.html
			if (ServerType == FtpServer.WuFTPd) {

				// assume the basic features supported
				GetFeatures(new string[] { "ABOR", "ACCT", "ALLO", "APPE", "CDUP", "CWD", "DELE", "EPSV", "EPRT", "HELP", "LIST", "LPRT", "LPSV", "MKD", "MDTM", "MODE", "NLST", "NOOP", "PASS", "PASV", "PORT", "PWD", "QUIT", "REST", "RETR", "RMD", "RNFR", "RNTO", "SITE", "SIZE", "STAT", "STOR", "STOU", "STRU", "SYST", "TYPE" });

			}

		}

		#endregion

	}
}