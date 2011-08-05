using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace System.Net.FtpClient {
	/// <summary>
	/// Parses MLST/MLSD and LIST formats
	/// </summary>
	public class FtpListItem {
		FtpObjectType _objectType = FtpObjectType.Unknown;
		/// <summary>
		/// Gets the type of object (File/Directory/Unknown)
		/// </summary>
		public FtpObjectType Type {
			get { return _objectType; }
			private set { _objectType = value; }
		}

		string _name = null;
		/// <summary>
		/// The file/directory name from the listing
		/// </summary>
		public string Name {
			get { return _name; }
			private set { _name = value; }
		}

		long _size = -1;
		/// <summary>
		/// The file size from the listing, default -1
		/// </summary>
		public long Size {
			get { return _size; }
			private set { _size = value; }
		}

		DateTime _modify = DateTime.MinValue;
		/// <summary>
		/// The last write time from the listing
		/// </summary>
		public DateTime Modify {
			get { return _modify; }
			private set { _modify = value; }
		}

		#region LIST parsing regular expressions
		/// <summary>
		/// Regular expression used for matching IIS DOS style file listings
		/// </summary>
		Regex RegexDosFile {
			get {
				return new Regex(@"\d+-\d+-\d+\s+\d+:\d+\w+\s+(\d+)\s+(.*)");
			}
		}

		/// <summary>
		/// Regular expression used for matching IIS DOS style directory listings
		/// </summary>
		Regex RegexDosDirectory {
			get {
				return new Regex(@"\d+-\d+-\d+\s+\d+:\d+\w+\s+<DIR>\s+(.*)");
			}
		}

		/// <summary>
		/// Regular expression used for matching UNIX directory listings
		/// </summary>
		Regex RegexUnixDirectory {
			get {
				return new Regex(@"d[\w-]{9}\s+\d+\s+[\w\d]+\s+[\w\d]+\s+\d+\s+\w+\s+\d+\s+\d+:?\d+\s+(.*)");
			}
		}

		/// <summary>
		/// Regular expression used for matching UNIX file listings
		/// </summary>
		Regex RegexUnixFile {
			get {
				return new Regex(@"-[\w-]{9}\s+\d+\s+[\w\d]+\s+[\w\d]+\s+(\d+)\s+\w+\s+\d+\s+\d+:?\d+\s+(.*)");
			}
		}

		/// <summary>
		/// Regular expression used for matching UNIX symbolic link listings
		/// </summary>
		Regex RegexUnixLink {
			get {
				return new Regex(@"l[\w-]{9}\s+\d+\s+[\w\d]+\s+[\w\d]+\s+(\d+)\s+\w+\s+\d+\s+\d+:?\d+\s+.*->\s+(.*)");
			}
		}
		#endregion

		/// <summary>
		/// Parses DOS and UNIX LIST style listings
		/// </summary>
		/// <param name="listing"></param>
		private void ParseListListing(string listing) {
			Match m;

			m = this.RegexDosDirectory.Match(listing);
			if (m.Success) {
				this.Type = FtpObjectType.Directory;
				this.Name = m.Groups[1].Value;
			}

			m = this.RegexDosFile.Match(listing);
			if (m.Success) {
				this.Type = FtpObjectType.File;
				this.Size = long.Parse(m.Groups[1].Value);
				this.Name = m.Groups[2].Value;
			}

			m = this.RegexUnixDirectory.Match(listing);
			if (m.Success) {
				this.Type = FtpObjectType.Directory;
				this.Name = m.Groups[1].Value;
			}

			m = this.RegexUnixFile.Match(listing);
			if (m.Success) {
				this.Type = FtpObjectType.File;
				this.Size = long.Parse(m.Groups[1].Value);
				this.Name = m.Groups[2].Value;
			}

			m = this.RegexUnixLink.Match(listing);
			if (m.Success) {
				this.Type = FtpObjectType.File;
				this.Size = long.Parse(m.Groups[1].Value);
				this.Name = m.Groups[2].Value;
			}
		}

		#region MLS* Parsing
		/// <summary>
		/// Parses MLST and MLSD formats
		/// </summary>
		/// <param name="listing"></param>
		private void ParseMachineListing(string listing) {
			string type = null, modify = null, size = null, name = null;
			string attribute = "";

			// remove any spaces from the beginning. this should
			// not happen unless the output we're parsing is from
			// MLST as opposed to MLSD.
			listing = listing.TrimStart(' ');

			for (int i = 0; i < listing.Length; i++) {
				if (listing[i] == ';') {
					// end of attribute
					string[] parts = attribute.Split('=');

					if (parts.Length == 2) {
						switch (parts[0].ToLower()) {
							case "type":
								type = parts[1].ToLower();
								break;
							case "modify":
								modify = parts[1];
								break;
							case "size":
								size = parts[1];
								break;
						}
					}

					attribute = "";
				}
				else if (listing[i] == ' ') {
					// end of attribute list, next character
					// starts file name
					if (i + 1 < listing.Length) {
						name = listing.Substring(i + 1, listing.Length - (i + 1));
						// the file name is the last thing we should encounter.
						// end the loop
						break;
					}
				}
				else {
					attribute += listing[i];
				}
			}

			if (type == "file" || type =="dir") {
				this.Type = (type == "file") ? FtpObjectType.File : FtpObjectType.Directory;
				this.Name = name;

				if (size != null) {
					this.Size = long.Parse(size);
				}

				if (modify != null) {
					DateTime tmodify;
					string[] formats = new string[] { "yyyyMMddHHmmss", "yyyyMMddHHmmss.fff" };

					if (DateTime.TryParseExact(modify, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out tmodify)) {
						this.Modify = tmodify;
					}
				}
			}
		}
		#endregion

		/// <summary>
		/// Parses a given listing
		/// </summary>
		/// <param name="listing">The single line that needs to be parsed</param>
		/// <param name="type">The command that generated the line to be parsed</param>
		public bool Parse(string listing, FtpListType type) {
			if (type == FtpListType.MLSD || type == FtpListType.MLST) {
				this.ParseMachineListing(listing);
			}
			else if (type == FtpListType.LIST) {
				this.ParseListListing(listing);
			}
			else {
				throw new NotImplementedException(string.Format("{0} style formats are not supported.", type.ToString()));
			}

			return this.Type != FtpObjectType.Unknown;
		}

		/// <summary>
		/// Initializes an empty parser
		/// </summary>
		public FtpListItem() {

		}

		/// <summary>
		/// Parses a given listing
		/// </summary>
		/// <param name="listing">The single line that needs to be parsed</param>
		/// <param name="type">The command that generated the line to be parsed</param>
		public FtpListItem(string listing, FtpListType type)
			: this() {
			this.Parse(listing, type);
		}

		/// <summary>
		/// Parses an array of list results
		/// </summary>
		/// <param name="items">Array of list results</param>
		/// <param name="type">The command that generated the list being parsed</param>
		/// <returns></returns>
		public static FtpListItem[] ParseList(string[] items, FtpListType type) {
			List<FtpListItem> lst = new List<FtpListItem>();

			foreach (string s in items) {
				FtpListItem i = new FtpListItem(s, type);

				if (i.Type != FtpObjectType.Unknown) {
					lst.Add(i);
				}
			}

			return lst.ToArray();
		}
	}
}
