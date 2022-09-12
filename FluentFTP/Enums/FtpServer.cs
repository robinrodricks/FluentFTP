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
		/// Definitely PureFTPd server
		/// </summary>
		PureFTPd,

		/// <summary>
		/// Definitely VsFTPd server
		/// </summary>
		VsFTPd,

		/// <summary>
		/// Definitely ProFTPD server
		/// </summary>
		ProFTPD,

		/// <summary>
		/// Definitely FileZilla server
		/// </summary>
		FileZilla,

		/// <summary>
		/// Definitely OpenVMS server
		/// </summary>
		OpenVMS,

		/// <summary>
		/// Definitely Windows CE FTP server
		/// </summary>
		WindowsCE,

		/// <summary>
		/// Definitely WuFTPd server
		/// </summary>
		WuFTPd,

		/// <summary>
		/// Definitely GlobalScape EFT server
		/// </summary>
		GlobalScapeEFT,

		/// <summary>
		/// Definitely HP NonStop/Tandem server
		/// </summary>
		NonStopTandem,

		/// <summary>
		/// Definitely Serv-U server
		/// </summary>
		ServU,

		/// <summary>
		/// Definitely Cerberus FTP server
		/// </summary>
		Cerberus,

		/// <summary>
		/// Definitely Windows Server/IIS FTP server
		/// </summary>
		WindowsServerIIS,

		/// <summary>
		/// Definitely CrushFTP server
		/// </summary>
		CrushFTP,

		/// <summary>
		/// Definitely glFTPd server
		/// </summary>
		glFTPd,

		/// <summary>
		/// Definitely Homegate FTP server
		/// </summary>
		HomegateFTP,

		/// <summary>
		/// Definitely BFTPd server
		/// </summary>
		BFTPd,

		/// <summary>
		/// Definitely FTP2S3 gateway server
		/// </summary>
		FTP2S3Gateway,

		/// <summary>
		/// Definitely XLight FTP server
		/// </summary>
		XLight,

		/// <summary>
		/// Definitely Sun OS Solaris FTP server
		/// </summary>
		SolarisFTP,

		/// <summary>
		/// Definitely IBM z/OS FTP server
		/// </summary>
		IBMzOSFTP,

		/// <summary>
		/// Definitely IBM OS/400 FTP server
		/// </summary>
		IBMOS400FTP,

		/// <summary>
		/// Definitely FritzBox FTP server
		/// </summary>
		FritzBox,

		/// <summary>
		/// Definitely WS_FTP server
		/// </summary>
		WSFTP,

		/// <summary>
		/// Definitely PyFtpdLib server
		/// </summary>
		PyFtpdLib,

		/// <summary>
		/// Definitely Titan FTP server
		/// </summary>
		TitanFTP,

		/// <summary>
		/// Definitely Rumpus server
		/// </summary>
		Rumpus,

		/// <summary>
		/// Definitely ABB IDAL FTP server
		/// </summary>
		IDALFTP,

		/// <summary>
		/// Definitely TP-LINK FTP server
		/// </summary>
		TPLink,

		/// <summary>
		/// Definitely D-Link FTP server
		/// </summary>
		DLink,

		/// <summary>
		/// Definitely Huawei Technologies HG5xxx series FTP server
		/// </summary>
		Huawei,

		/// <summary>
		/// Definitely MikroTik RouterOS FTP server
		/// </summary>
		MikroTik,
	}
}