using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

namespace FluentFTP {
	/// <summary>
	/// Represents a file system object on the server
	/// </summary>
	/// <example><code source="..\Examples\CustomParser.cs" lang="cs" /></example>
	public class FtpListItem {
		/// <summary>
		/// Blank constructor; Fill args manually.
		/// 
		/// NOTE TO USER : You should not need to construct this class manually except in advanced cases. Typically constructed by GetListing().
		/// </summary>
		public FtpListItem() {
		}

		/// <summary>
		/// Constructor with mandatory args filled.
		/// 
		/// NOTE TO USER : You should not need to construct this class manually except in advanced cases. Typically constructed by GetListing().
		/// </summary>
		public FtpListItem(string record, string name, long size, bool isDir, ref DateTime lastModifiedTime) {
			m_input = record;
			m_name = name;
			m_size = size;
			m_type = isDir ? FtpFileSystemObjectType.Directory : FtpFileSystemObjectType.File;
			m_modified = lastModifiedTime;
		}

		private FtpFileSystemObjectType m_type = 0;

		/// <summary>
		/// Gets the type of file system object.
		/// </summary>
		public FtpFileSystemObjectType Type {
			get => m_type;
			set => m_type = value;
		}

		private string m_path = null;

		/// <summary>
		/// Gets the full path name to the object.
		/// </summary>
		public string FullName {
			get => m_path;
			set => m_path = value;
		}

		private string m_name = null;

		/// <summary>
		/// Gets the name of the object.
		/// </summary>
		public string Name {
			get {
				if (m_name == null && m_path != null) {
					return m_path.GetFtpFileName();
				}

				return m_name;
			}
			set => m_name = value;
		}

		private string m_linkTarget = null;

		/// <summary>
		/// Gets the target a symbolic link points to.
		/// </summary>
		public string LinkTarget {
			get => m_linkTarget;
			set => m_linkTarget = value;
		}

		private int m_linkCount = 0;

		/// <summary>
		/// Gets the number of links pointing to this file. Only supplied by Unix servers.
		/// </summary>
		public int LinkCount {
			get => m_linkCount;
			set => m_linkCount = value;
		}

		private FtpListItem m_linkObject = null;

		/// <summary>
		/// Gets the object that the LinkTarget points to. This property is null unless you pass the
		/// <see cref="FtpListOption.DerefLinks"/> flag in which case GetListing() will try to resolve
		/// the target itself.
		/// </summary>
		public FtpListItem LinkObject {
			get => m_linkObject;
			set => m_linkObject = value;
		}

		private DateTime m_modified = DateTime.MinValue;

		/// <summary>
		/// Gets the last write time of the object.
		/// </summary>
		public DateTime Modified {
			get => m_modified;
			set => m_modified = value;
		}

		private DateTime m_created = DateTime.MinValue;

		/// <summary>
		/// Gets the created date of the object.
		/// </summary>
		public DateTime Created {
			get => m_created;
			set => m_created = value;
		}

		private long m_size = -1;

		/// <summary>
		/// Gets the size of the object.
		/// </summary>
		public long Size {
			get => m_size;
			set => m_size = value;
		}

		private FtpSpecialPermissions m_specialPermissions = FtpSpecialPermissions.None;

		/// <summary>
		/// Gets special UNIX permissions such as Sticky, SUID and SGID.
		/// </summary>
		public FtpSpecialPermissions SpecialPermissions {
			get => m_specialPermissions;
			set => m_specialPermissions = value;
		}

		private FtpPermission m_ownerPermissions = FtpPermission.None;

		/// <summary>
		/// Gets the owner permissions.
		/// </summary>
		public FtpPermission OwnerPermissions {
			get => m_ownerPermissions;
			set => m_ownerPermissions = value;
		}

		private FtpPermission m_groupPermissions = FtpPermission.None;

		/// <summary>
		/// Gets the group permissions.
		/// </summary>
		public FtpPermission GroupPermissions {
			get => m_groupPermissions;
			set => m_groupPermissions = value;
		}

		private FtpPermission m_otherPermissions = FtpPermission.None;

		/// <summary>
		/// Gets the others permissions.
		/// </summary>
		public FtpPermission OthersPermissions {
			get => m_otherPermissions;
			set => m_otherPermissions = value;
		}

		private string m_rawPermissions = null;

		/// <summary>
		/// Gets the raw string received for the file permissions.
		/// Use this if the other properties are blank/invalid.
		/// </summary>
		public string RawPermissions {
			get => m_rawPermissions;
			set => m_rawPermissions = value;
		}

		private int m_chmod = 0;

		/// <summary>
		/// Gets the file permissions in the CHMOD format.
		/// </summary>
		public int Chmod {
			get => m_chmod;
			set => m_chmod = value;
		}

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


		private string m_input = null;

		/// <summary>
		/// Gets the input string that was parsed to generate the
		/// values in this object.
		/// </summary>
		public string Input {
			get => m_input;
			set => m_input = value;
		}

		/// <summary>
		/// Returns a string representation of this object and its properties
		/// </summary>
		/// <returns>A string representing this object</returns>
		public override string ToString() {
			var sb = new StringBuilder();
			if (Type == FtpFileSystemObjectType.File) {
				sb.Append("FILE");
			}
			else if (Type == FtpFileSystemObjectType.Directory) {
				sb.Append("DIR ");
			}
			else if (Type == FtpFileSystemObjectType.Link) {
				sb.Append("LINK");
			}

			sb.Append("   ");
			sb.Append(Name);
			if (Type == FtpFileSystemObjectType.File) {
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
	}
}