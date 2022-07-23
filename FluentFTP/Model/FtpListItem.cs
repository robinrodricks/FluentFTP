using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using FluentFTP.Helpers;

namespace FluentFTP {
	/// <summary>
	/// Represents a file system object on the server
	/// </summary>
	public class FtpListItem {
		/// <summary>
		/// Blank constructor, you will need to fill arguments manually.
		/// 
		/// NOTE TO USER : You should not need to construct this class manually except in advanced cases. Typically constructed by GetListing().
		/// </summary>
		public FtpListItem() {
		}

		/// <summary>
		/// Constructor with mandatory arguments filled.
		/// 
		/// NOTE TO USER : You should not need to construct this class manually except in advanced cases. Typically constructed by GetListing().
		/// </summary>
		public FtpListItem(string record, string name, long size, bool isDir, DateTime lastModifiedTime) {
			this.Input = record;
			this.Name = name;
			this.Size = size;
			this.Type = isDir ? FtpObjectType.Directory : FtpObjectType.File;
			this.Modified = lastModifiedTime;
		}

		/// <summary>
		/// Constructor with mandatory arguments filled.
		/// 
		/// NOTE TO USER : You should not need to construct this class manually except in advanced cases. Typically constructed by GetListing().
		/// </summary>
		public FtpListItem(string name, long size, FtpObjectType type, DateTime lastModifiedTime) {
			this.Name = name;
			this.Size = size;
			this.Type = type;
			this.Modified = lastModifiedTime;
		}


		/// <summary>
		/// Gets the type of file system object.
		/// </summary>
		public FtpObjectType Type;

		/// <summary>
		/// Gets the sub type of file system object.
		/// </summary>
		public FtpObjectSubType SubType;

		/// <summary>
		/// Gets the full path name to the file or folder.
		/// </summary>
		public string FullName;

		private string m_name = null;

		/// <summary>
		/// Gets the name of the file or folder. Does not include the full path.
		/// </summary>
		public string Name {
			get {
				if (m_name == null && FullName != null) {
					return FullName.GetFtpFileName();
				}
				return m_name;
			}
			set => m_name = value;
		}

		/// <summary>
		/// Gets the target a symbolic link points to.
		/// </summary>
		public string LinkTarget;

		/// <summary>
		/// Gets the number of links pointing to this file. Only supplied by Unix servers.
		/// </summary>
		public int LinkCount;

		/// <summary>
		/// Gets the object that the LinkTarget points to. This property is null unless you pass the
		/// <see cref="FtpListOption.DerefLinks"/> flag in which case GetListing() will try to resolve
		/// the target itself.
		/// </summary>
		public FtpListItem LinkObject;

		/// <summary>
		/// Gets the last write time of the object after timezone conversion (if enabled).
		/// </summary>
		public DateTime Modified = DateTime.MinValue;

		/// <summary>
		/// Gets the created date of the object after timezone conversion (if enabled).
		/// </summary>
		public DateTime Created = DateTime.MinValue;

		/// <summary>
		/// Gets the last write time of the object before any timezone conversion.
		/// </summary>
		public DateTime RawModified = DateTime.MinValue;

		/// <summary>
		/// Gets the created date of the object before any timezone conversion.
		/// </summary>
		public DateTime RawCreated = DateTime.MinValue;

		/// <summary>
		/// Gets the size of the object.
		/// </summary>
		public long Size = -1;

		/// <summary>
		/// Gets special UNIX permissions such as Sticky, SUID and SGID.
		/// </summary>
		public FtpSpecialPermissions SpecialPermissions = FtpSpecialPermissions.None;

		/// <summary>
		/// Gets the owner permissions.
		/// </summary>
		public FtpPermission OwnerPermissions = FtpPermission.None;

		/// <summary>
		/// Gets the group permissions.
		/// </summary>
		public FtpPermission GroupPermissions = FtpPermission.None;

		/// <summary>
		/// Gets the others permissions.
		/// </summary>
		public FtpPermission OthersPermissions = FtpPermission.None;

		/// <summary>
		/// Gets the raw string received for the file permissions.
		/// Use this if the other properties are blank/invalid.
		/// </summary>
		public string RawPermissions;

		/// <summary>
		/// Gets the file permissions in the CHMOD format.
		/// </summary>
		public int Chmod;

		/// <summary>
		/// Gets the raw string received for the file's GROUP permissions.
		/// Use this if the other properties are blank/invalid.
		/// </summary>
		public string RawGroup = null;

		/// <summary>
		/// Gets the raw string received for the file's OWNER permissions.
		/// Use this if the other properties are blank/invalid.
		/// </summary>
		public string RawOwner = null;


		/// <summary>
		/// Gets the input string that was parsed to generate the
		/// values in this object.
		/// </summary>
		public string Input;

		/// <summary>
		/// Returns a string representation of this object and its properties
		/// </summary>
		/// <returns>A string representing this object</returns>
		public override string ToString() {
			var sb = new StringBuilder();
			if (Type == FtpObjectType.File) {
				sb.Append("FILE");
			}
			else if (Type == FtpObjectType.Directory) {
				sb.Append("DIR ");
			}
			else if (Type == FtpObjectType.Link) {
				sb.Append("LINK");
			}

			sb.Append("   ");
			sb.Append(Name);
			if (Type == FtpObjectType.File) {
				sb.Append("      ");
				sb.Append("(");
				sb.Append(Size.FileSizeToString());
				sb.Append(")");
			}

			if (Created != DateTime.MinValue) {
				sb.Append("      ");
				sb.Append("Created : ");
				sb.Append(Created.ToString());
			}

			if (Modified != DateTime.MinValue) {
				sb.Append("      ");
				sb.Append("Modified : ");
				sb.Append(Modified.ToString());
			}

			return sb.ToString();
		}
		/// <summary>
		/// Returns a code representation of this object and its properties
		/// </summary>
		public string ToCode() {
			var sb = new StringBuilder();
			sb.Append("new FtpListItem(");
			sb.Append(Name.EscapeStringLiteral());
			sb.Append(",");
			sb.Append(Size);
			sb.Append(",");
			sb.Append("FtpObjectType.");
			sb.Append(Type.ToString());
			sb.Append(",");
			sb.Append(Modified.ToCode());
			sb.Append(")");
			return sb.ToString();
		}
	}
}