using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;

namespace System.Net.FtpClient {
    /// <summary>
    /// Map's regex group index's to the appropriate
    /// fields in the parser results.
    /// </summary>
    public class FtpListFormatParser : IDisposable {
        FtpObjectType _type = FtpObjectType.Unknown;
        /// <summary>
        /// The type of objec this parser is for (File or Directory)
        /// </summary>
        public FtpObjectType ObjectType {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// The name of the object. A null value is returned when this information is not available.
        /// </summary>
        public string Name {
            get {
                if (this.NameIndex > 0 && this.Match != null && this.Match.Success && this.Match.Groups.Count > this.NameIndex) {
                    return this.Match.Groups[this.NameIndex].Value;
                }

                return null;
            }
        }

        int _nameIndex = 0;
        /// <summary>
        /// The index in the match group collection where the object name can be found after
        /// a successfull parse. Setting a less than 1 value indicates that this field is not available.
        /// </summary>
        public int NameIndex {
            get { return _nameIndex; }
            set { _nameIndex = value; }
        }

        /// <summary>
        /// The size of the object. 0 is returned when this information is not available.
        /// </summary>
        public long Size {
            get {
                if (this.SizeIndex > 0 && this.Match != null && this.Match.Groups.Count > this.SizeIndex) {
                    long size = 0;

                    if (long.TryParse(this.Match.Groups[this.SizeIndex].Value, out size)) {
                        return size;
                    }
                }

                return 0;
            }
        }

        int _sizeIndex = 0;
        /// <summary>
        /// The index in the match group collection where the object name can be found after
        /// a successfull parse. Setting a less than 1 value indicates that this field is not available.
        /// </summary>
        public int SizeIndex {
            get { return _sizeIndex; }
            set { _sizeIndex = value; }
        }

        /// <summary>
        /// The last write time of the object. DateTime.MinValue is returned when this information
        /// is not available.
        /// </summary>
        public DateTime Modify {
            get {
                if (this.ModifyIndex > 0 && this.Match != null && this.Match.Groups.Count > this.ModifyIndex) {
                    DateTime date;

                    string[] formats = new string[] { "MMM dd HH:mm", "MMM dd  yyyy" };
                    if (DateTime.TryParseExact(this.Match.Groups[this.ModifyIndex].Value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
                    {
                        return date;
                    }
                }

                return DateTime.MinValue;
            }
        }

        int _modifyIndex = 0;
        /// <summary>
        /// The index in the match group collection where the object name can be found after
        /// a successfull parse. Setting a less than 1 value indicates that this field is not available.
        /// </summary>
        public int ModifyIndex {
            get { return _modifyIndex; }
            set { _modifyIndex = value; }
        }

        /// <summary>
        /// The mode of the object. null is return when this information is not available.
        /// </summary>
        public string Mode {
            get {
                if (this.ModeIndex > 0 && this.Match != null && this.Match.Groups.Count > this.ModeIndex)
                    return this.Match.Groups[this.ModeIndex].Value;
                return null;
            }
        }

        int _modeIndex = 0;
        /// <summary>
        /// The index of the match group collection where the object mode can be found after
        /// a successful parse. Setting a less than 1 value indicates that this field is not available.
        /// </summary>
        public int ModeIndex {
            get { return _modeIndex; }
            set { _modeIndex = value; }

        }

        /// <summary>
        /// The owner of the object. null is return when this information is not available.
        /// </summary>
        public string Owner {
            get {
                if (this.OwnerIndex > 0 && this.Match != null && this.Match.Groups.Count > this.OwnerIndex)
                    return this.Match.Groups[this.OwnerIndex].Value;
                return null;
            }
        }

        int _ownerIndex = 0;
        /// <summary>
        /// The index of the match group collection where the object owner can be found after
        /// a successful parse. Setting a less than 1 value indicates that this field is not available.
        /// </summary>
        public int OwnerIndex {
            get { return _ownerIndex; }
            set { _ownerIndex = value; }
        }

        /// <summary>
        /// The group of the object. null is return when this information is not available.
        /// </summary>
        public string Group {
            get {
                if (this.GroupIndex > 0 && this.Match != null && this.Match.Groups.Count > this.GroupIndex)
                    return this.Match.Groups[this.GroupIndex].Value;
                return null;
            }
        }

        int _groupIndex = 0;
        /// <summary>
        /// The index of the match group collection where the object group can be found after
        /// a successful parse. Setting a less than 1 value indicates that this field is not available.
        /// </summary>
        public int GroupIndex {
            get { return _groupIndex; }
            set { _groupIndex = value; }
        }

        /// <summary>
        /// The link path of the object in case it is a symlink. null is return when this information is not available.
        /// </summary>
        public string LinkPath {
            get {
                if (this.LinkPathIndex > 0 && this.Match != null && this.Match.Groups.Count > this.LinkPathIndex)
                    return this.Match.Groups[this.LinkPathIndex].Value;
                return null;
            }
        }

        int _linkPathIndex = 0;
        /// <summary>
        /// The index of the match group collection where the object link path can be found afet
        /// a successful parse. Setting a less than 1 value indicates that this field is not available.
        /// </summary>
        public int LinkPathIndex {
            get { return _linkPathIndex; }
            set { _linkPathIndex = value; }
        }

        Match _m = null;
        /// <summary>
        /// The match result after calling the Parse() method.
        /// </summary>
        Match Match {
            get { return _m; }
            set { _m = value; }
        }

        Regex _re = null;
        /// <summary>
        /// The regex used to parse the input string.
        /// </summary>
        public Regex Regex {
            get { return _re; }
            set { _re = value; }
        }

        /// <summary>
        /// Parse the given string
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Returns true on success, false on failure</returns>
        public bool Parse(string input) {
            this.Match = this.Regex.Match(input);
            return this.Match.Success;
        }

        /// <summary>
        /// Creates a new instance of the FtpListParser object and sets
        /// the given index locations as specified.
        /// </summary>
        /// <param name="re"></param>
        /// <param name="nameIndex"></param>
        /// <param name="sizeIndex"></param>
        /// <param name="modifyIndex"></param>
        /// <param name="modeIndex"></param>
        /// <param name="ownerIndex"></param>
        /// <param name="groupIndex"></param>
        /// <param name="type"></param>
        public FtpListFormatParser(Regex re, int nameIndex, int sizeIndex, int modifyIndex, int modeIndex, int ownerIndex, int groupIndex, int linkPathIndex, FtpObjectType type) {
            this.Regex = re;
            this.NameIndex = nameIndex;
            this.SizeIndex = sizeIndex;
            this.ModifyIndex = modifyIndex;
            this.ModeIndex = modeIndex;
            this.OwnerIndex = ownerIndex;
            this.GroupIndex = groupIndex;
            this.LinkPathIndex = linkPathIndex;
            this.ObjectType = type;
        }

        /// <summary>
        /// Creates a new instance of the FtpListParser object and sets
        /// the given index locations as sepcified.
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="nameIndex"></param>
        /// <param name="sizeIndex"></param>
        /// <param name="modifyIndex"></param>
        /// <param name="modeIndex"></param>
        /// <param name="ownerIndex"></param>
        /// <param name="groupIndex"></param>
        /// <param name="type"></param>
        public FtpListFormatParser(string regex, int nameIndex, int sizeIndex, int modifyIndex, int modeIndex, int ownerIndex, int groupIndex, int linkPathIndex, FtpObjectType type)
            : this(new Regex(regex), nameIndex, sizeIndex, modifyIndex, modeIndex, ownerIndex, groupIndex, linkPathIndex, type) {
        }

        /// <summary>
        /// Cleans up this format parser
        /// </summary>
        public void Dispose() {
            this.Regex = null;
            this.Match = null;
            this.NameIndex = 0;
            this.SizeIndex = 0;
            this.ModifyIndex = 0;
            this.ModeIndex = 0;
            this.GroupIndex = 0;
            this.OwnerIndex = 0;
            this.LinkPathIndex = 0;
        }

        static List<FtpListFormatParser> _listParsers = null;
        /// <summary>
        /// Gets a collection of FtpListParser objects for parsing various
        /// listing formats. 
        /// </summary>
        public static List<FtpListFormatParser> Parsers {
            get {
                if (_listParsers == null) {
                    _listParsers = new List<FtpListFormatParser>();

                    // initalize the default set of parsers

                    // DOS format directory
                    _listParsers.Add(new FtpListFormatParser(
                        @"(\d+-\d+-\d+\s+\d+:\d+\w+)\s+<DIR>\s+(.*)",
                        2, -1, 1, -1, -1, -1, -1, FtpObjectType.Directory));

                    // DOS format file
                    _listParsers.Add(new FtpListFormatParser(
                        @"(\d+-\d+-\d+\s+\d+:\d+\w+)\s+(\d+)\s+(.*)",
                        3, 2, 1, -1, -1, -1, -1, FtpObjectType.File));

                    // UNIX format directory
                    _listParsers.Add(new FtpListFormatParser(
                        @"(d[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+\d+\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                        5, -1, 4, 1, 2, 3, -1, FtpObjectType.Directory));

                    // UNIX format file
                    _listParsers.Add(new FtpListFormatParser(
                        @"(-[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                        6, 4, 5, 1, 2, 3, -1, FtpObjectType.File));

                    // UNIX format link
                    _listParsers.Add(new FtpListFormatParser(
                        @"(l[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*) ->\s+(.*)",
                        6, 4, 5, 1, 2, 3, 7, FtpObjectType.Link));
                    
                    // UNIX format device
                    _listParsers.Add(new FtpListFormatParser(
                        @"(c[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                        6, 4, 5, 1, 2, 3, -1, FtpObjectType.Device));

                    _listParsers.Add(new FtpListFormatParser(
                        @"(b[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                        6, 4, 5, 1, 2, 3, -1, FtpObjectType.Device));

                    //
                    // see work item 349 in the issue tracker for the bug report
                    // indicating the format being parsed here.
                    //
                    // other format directory
                    _listParsers.Add(new FtpListFormatParser(
                        @"d[\w-]+\s\d+\s[\d\w]+\s\d+\s+\w+\s+\d+\s+\d+:?\d+\s+(.*)",
                        1, 0, 0, 0, 0, 0, 0, FtpObjectType.Directory));

                    // other format file
                    _listParsers.Add(new FtpListFormatParser(
                        @"-[\w-]+\s+\d+\s+[\w\d]+\s+(\d+)\s+\w+\s+\d+\s+\d+:?\d+\s+(.*)",
                        2, 1, 0, 0, 0, 0, 0, FtpObjectType.File));
                }

                return _listParsers;
            }
        }
    }
}
