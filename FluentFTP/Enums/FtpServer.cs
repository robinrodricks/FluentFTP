using System;

namespace FluentFTP {
	/// <summary>
	/// Defines the type of the FTP server software.
	/// Add constants here as you add detection scripts for individual server types.
	/// </summary>
	public enum FtpServer {
		/// <summary>
		/// Unknown FTP server software
		/// </summary>
		Unknown,

		/// <summary>
		/// Definitely Apache FTP server
		/// </summary>
		Apache,

		/// <summary>
		/// Definitely BFTPd server
		/// </summary>
		BFTPd,

		/// <summary>
		/// Definitely Cerberus FTP server
		/// </summary>
		Cerberus,

		/// <summary>
		/// Definitely CrushFTP server
		/// </summary>
		CrushFTP,

		/// <summary>
		/// Definitely D-Link FTP server
		/// </summary>
		DLink,

		/// <summary>
		/// Definitely FileZilla server
		/// </summary>
		FileZilla,

		/// <summary>
		/// Definitely FritzBox FTP server
		/// </summary>
		FritzBox,

		/// <summary>
		/// Definitely FTP2S3 gateway server
		/// </summary>
		FTP2S3Gateway,

		/// <summary>
		/// Definitely glFTPd server
		/// </summary>
		glFTPd,

		/// <summary>
		/// Definitely GlobalScape EFT server
		/// </summary>
		GlobalScapeEFT,

		/// <summary>
		/// Definitely Homegate FTP server
		/// </summary>
		HomegateFTP,

		/// <summary>
		/// Definitely Huawei Technologies HG5xxx series FTP server
		/// </summary>
		Huawei,

		/// <summary>
		/// Definitely IBM z/OS FTP server
		/// </summary>
		IBMzOSFTP,

		/// <summary>
		/// Definitely IBM OS/400 FTP server
		/// </summary>
		IBMOS400FTP,

		/// <summary>
		/// Definitely ABB IDAL FTP server
		/// </summary>
		IDALFTP,

		/// <summary>
		/// Definitely MikroTik RouterOS FTP server
		/// </summary>
		MikroTik,

		/// <summary>
		/// Definitely HP NonStop/Tandem server
		/// </summary>
		NonStopTandem,

		/// <summary>
		/// Definitely OpenVMS server
		/// </summary>
		OpenVMS,

		/// <summary>
		/// Definitely ProFTPD server
		/// </summary>
		ProFTPD,

		/// <summary>
		/// Definitely PureFTPd server
		/// </summary>
		PureFTPd,

		/// <summary>
		/// Definitely PyFtpdLib server
		/// </summary>
		PyFtpdLib,

		/// <summary>
		/// Definitely Rumpus server
		/// </summary>
		Rumpus,

		/// <summary>
		/// Definitely Serv-U server
		/// </summary>
		ServU,

		/// <summary>
		/// Definitely Sun OS Solaris FTP server
		/// </summary>
		SolarisFTP,

		/// <summary>
		/// Definitely Titan FTP server
		/// </summary>
		TitanFTP,

		/// <summary>
		/// Definitely TP-LINK FTP server
		/// </summary>
		TPLink,

		/// <summary>
		/// Definitely VsFTPd server
		/// </summary>
		VsFTPd,

		/// <summary>
		/// Definitely Windows CE FTP server
		/// </summary>
		WindowsCE,

		/// <summary>
		/// Definitely Windows Server/IIS FTP server
		/// </summary>
		WindowsServerIIS,

		/// <summary>
		/// Definitely WS_FTP server
		/// </summary>
		WSFTP,

		/// <summary>
		/// Definitely WuFTPd server
		/// </summary>
		WuFTPd,

		/// <summary>
		/// Definitely XLight FTP server
		/// </summary>
		XLight,
	}
}
