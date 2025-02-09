using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.Client.Modules {
	internal static class ServerStringModule {

		#region File Exists

		/// <summary>
		/// Error messages returned by various servers when a file does not exist.
		/// Instead of throwing an error, we use these to detect and handle the file detection properly.
		/// MUST BE LOWER CASE!
		/// </summary>
		public static string[] fileNotFound = new[] {
			"can't find file",
			"can't check for file existence",
			"does not exist",
			"failed to open file",
			"not found",
			"no such file",
			"couldn't open the file",
			"cannot find the file",
			"cannot find",
			"can't get file",
			"could not get file",
			"could not get file size",
			"cannot get file",
			"not a regular file",
			"file unavailable",
			"file is unavailable",
			"file not unavailable",
			"file is not available",
			"no files found",
			"no file found",
			"datei oder verzeichnis nicht gefunden",
			"can't find the path",
			"cannot find the path",
			"could not find the path",
			"file doesnot exist",
			"couldn't open the file or directory"
		};

		#endregion

		#region File Transfer

		/// <summary>
		/// Error messages returned by various servers when a file transfer temporarily failed.
		/// MUST BE LOWER CASE!
		/// </summary>
		public static string[] unexpectedEOF = new[] {
			"unexpected eof for remote file",
			"received an unexpected eof",
			"unexpected eof"
		};

		#endregion

		#region Create Directory

		/// <summary>
		/// Error messages returned by various servers when a folder already exists.
		/// Instead of throwing an error, we use these to detect and handle the folder creation properly.
		/// MUST BE LOWER CASE!
		/// </summary>
		public static string[] folderExists = new[] {
			"exist on server",
			"exists on server",
			"file exist",
			"directory exist",
			"folder exist",
			"file already exist",
			"directory already exist",
			"folder already exist",
		};

		#endregion

		#region Critical commands
		public static string[] criticalSingleCommands = new[] {
			"QUIT",
		};

		public static string[] criticalStartingCommands = new[] {
			"EPRT",
			"EPSV",
			"LPSV",
			"PASV",
			"SPSV",
			"PORT",
			"LPRT",
			"RNFR",
		};

		public static string[] criticalTerminatingCommands = new[] {
			"ABOR",
			"LIST",
			"NLST",
			"MLSD",
			"MLST",
			"STOR",
			"STOU",
			"APPE",
			"REST",
			"RETR",
			"THMB",
			"RNTO",
		};

		#endregion

	}
}