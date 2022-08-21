using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class ParserTests {

		private static void TestParsing(FtpListParser parser, string path, string[] testValues, FtpListItem[] expectedValues) {

			// code prefix
			if (expectedValues == null) {
				Debug.WriteLine("var expected = new FtpListItem[]{");
			}

			// per test value
			for (int i = 0; i < testValues.Length; i++) {
				var test = testValues[i];

				// parse it
				FtpListItem item = null;
				try {
					item = parser.ParseSingleLine(path, test, new List<FtpCapability>(), false);
				}
				catch (Exception) { }


				// if no expected values known
				if (expectedValues == null) {

					// print parsed value to build test expectation
					if (item == null) {
						Debug.WriteLine("null,");
					}
					else {
						Debug.WriteLine(item.ToCode() + ",");
					}
				}
				else {

					// test if correct
					if (item != null) {
						Assert.Equal(item.ToString(), expectedValues[i].ToString());
					}
				}

			}

			// code postfix
			if (expectedValues == null) {
				Debug.WriteLine("};");
			}
		}


		[Fact]
		public void Machine() {

			var client = new FtpClient();
			client.SetFeatures(new List<FtpCapability> { FtpCapability.MLSD });
			var parser = new FtpListParser(client);
			parser.Init(FtpOperatingSystem.Unix, FtpParser.Machine);

			var sample = new[] {
				"Type=file; File1-whitespace trailing\t ",
				"Type=file; \t File2-whitespace leading",
				"modify=20130426135501;perm=;size=14718921;type=file;unique=802U1066013B;UNIX.group=1179;UNIX.mode=00;UNIX.owner=1179; File 3 Word Doc",
				"type=OS.unix=slink:/anything; Link Apple 1",
				"type=OS.UNIX=symlink; Link Tomato 2",
				"type=OS.unix=slink; Link Strawberry 3",
				"Type=file;Size=25730;Modify=19940728095854;Perm=; capmux.tar.z",
				"Type=file;Size=1830;Modify=19940916055648;Perm=r; hatch.c",
				"Type=file;Size=25624;Modify=19951003165342;Perm=r; MacIP-02.txt",
				"Type=file;Size=2154;Modify=19950501105033;Perm=r; uar.netbsd.patch",
				"Type=file;Size=54757;Modify=19951105101754;Perm=r; iptnnladev.1.0.sit.hqx",
				"Type=file;Size=226546;Modify=19970515023901;Perm=r; melbcs.tif",
				"Type=file;Size=12927;Modify=19961025135602;Perm=r; tardis.1.6.sit.hqx",
				"Type=file;Size=17867;Modify=19961025135602;Perm=r; timelord.1.4.sit.hqx",
				"Type=file;Size=224907;Modify=19980615100045;Perm=r; uar.1.2.3.sit.hqx",
				"Type=file;Size=1024990;Modify=19980130010322;Perm=r; cap60.pl198.tar.gz",
				"Type=OS.unix=slink:/foobar;Perm=;Unique=keVO1+4G4; foobar",
				"Type=OS.unix=chr-13/29;Perm=;Unique=keVO1+5G4; device",
				"Type=OS.unix=blk-11/108;Perm=;Unique=keVO1+6G4; block",
				"Type=file;Perm=awr;Unique=keVO1+8G4; writable",
				"Type=file;Perm=r;Unique=keVO1+EG4; two words",
				"Type=file;Perm=r;Unique=keVO1+IH4; leading space",
				"Type=file;Perm=r;Unique=keVO1+1G4; file1",
				"Type=dir;Perm=cpmel;Unique=keVO1+7G4; incoming",
				"Type=file;Perm=r;Unique=keVO1+1G4; file2",
				"Type=file;Perm=r;Unique=keVO1+1G4; file3",
				"Type=file;Perm=r;Unique=keVO1+1G4; file4",
				"Type=file;Perm=awdrf;Unique=keVO1+EH4; bar",
				"Type=file;Perm=awdrf;Unique=keVO1+LH4;",
				"Type=file;Perm=rf;Unique=keVO1+1G4; file5",
				"Type=file;Perm=rf;Unique=keVO1+1G4; file6",
				"Type=dir;Perm=cpmdelf;Unique=keVO1+!s2; empty",
				"Type=file;Size=44242;Modify=19990217230400; character-sets",
				"Type=file;Size=1947;Modify=19990209215600; operating-system-names",
				"Type=file;Size=30249;Modify=19990218032700; media-types",
				"Type=file;Size=1234;Modify=19980903020400; windows-1251",
				"Type=file;Size=4557;Modify=19980922001400; tis-620",
				"Type=file;Size=801;Modify=19970324130000; ibm775",
				"Type=file;Size=552;Modify=19970320130000; ibm866",
				"Type=file;Size=922;Modify=19960505140000; windows-1258",
				"Type=file;Size=2391;Modify=19980309130000; default",
				"Type=file;Size=943;Modify=19980309130000; tags",
				"Type=file;Size=870;Modify=19971026130000; navajo",
				"Type=file;Size=699;Modify=19950911140000; no-bok",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+Bd8; FILE2",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+aG8; file3",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+ag8; FILE3",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+bD8; file1",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+bD8; file2",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+Ag8; File3",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+bD8; File1",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+Bd8; File2",
				"Type=file;Size=4096;Modify=19990929011440;Perm=r;Unique=keVO1+bd8; FILE1",
			};

			var expected = new FtpListItem[]{
				new FtpListItem("File1-whitespace trailing\t ", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("\t File2-whitespace leading", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("File 3 Word Doc", 14718921, FtpObjectType.File, new DateTime(2013, 4, 26, 13, 55, 1, 0)),
				null,
				new FtpListItem("Link Tomato 2", -1, FtpObjectType.Link, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("Link Strawberry 3", -1, FtpObjectType.Link, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("capmux.tar.z", 25730, FtpObjectType.File, new DateTime(1994, 7, 28, 9, 58, 54, 0)),
				new FtpListItem("hatch.c", 1830, FtpObjectType.File, new DateTime(1994, 9, 16, 5, 56, 48, 0)),
				new FtpListItem("MacIP-02.txt", 25624, FtpObjectType.File, new DateTime(1995, 10, 3, 16, 53, 42, 0)),
				new FtpListItem("uar.netbsd.patch", 2154, FtpObjectType.File, new DateTime(1995, 5, 1, 10, 50, 33, 0)),
				new FtpListItem("iptnnladev.1.0.sit.hqx", 54757, FtpObjectType.File, new DateTime(1995, 11, 5, 10, 17, 54, 0)),
				new FtpListItem("melbcs.tif", 226546, FtpObjectType.File, new DateTime(1997, 5, 15, 2, 39, 1, 0)),
				new FtpListItem("tardis.1.6.sit.hqx", 12927, FtpObjectType.File, new DateTime(1996, 10, 25, 13, 56, 2, 0)),
				new FtpListItem("timelord.1.4.sit.hqx", 17867, FtpObjectType.File, new DateTime(1996, 10, 25, 13, 56, 2, 0)),
				new FtpListItem("uar.1.2.3.sit.hqx", 224907, FtpObjectType.File, new DateTime(1998, 6, 15, 10, 0, 45, 0)),
				new FtpListItem("cap60.pl198.tar.gz", 1024990, FtpObjectType.File, new DateTime(1998, 1, 30, 1, 3, 22, 0)),
				null,
				null,
				null,
				new FtpListItem("writable", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("two words", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("leading space", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file1", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("incoming", -1, FtpObjectType.Directory, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file2", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file3", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file4", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("bar", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				null,
				new FtpListItem("file5", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file6", -1, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("empty", -1, FtpObjectType.Directory, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("character-sets", 44242, FtpObjectType.File, new DateTime(1999, 2, 17, 23, 4, 0, 0)),
				new FtpListItem("operating-system-names", 1947, FtpObjectType.File, new DateTime(1999, 2, 9, 21, 56, 0, 0)),
				new FtpListItem("media-types", 30249, FtpObjectType.File, new DateTime(1999, 2, 18, 3, 27, 0, 0)),
				new FtpListItem("windows-1251", 1234, FtpObjectType.File, new DateTime(1998, 9, 3, 2, 4, 0, 0)),
				new FtpListItem("tis-620", 4557, FtpObjectType.File, new DateTime(1998, 9, 22, 0, 14, 0, 0)),
				new FtpListItem("ibm775", 801, FtpObjectType.File, new DateTime(1997, 3, 24, 13, 0, 0, 0)),
				new FtpListItem("ibm866", 552, FtpObjectType.File, new DateTime(1997, 3, 20, 13, 0, 0, 0)),
				new FtpListItem("windows-1258", 922, FtpObjectType.File, new DateTime(1996, 5, 5, 14, 0, 0, 0)),
				new FtpListItem("default", 2391, FtpObjectType.File, new DateTime(1998, 3, 9, 13, 0, 0, 0)),
				new FtpListItem("tags", 943, FtpObjectType.File, new DateTime(1998, 3, 9, 13, 0, 0, 0)),
				new FtpListItem("navajo", 870, FtpObjectType.File, new DateTime(1997, 10, 26, 13, 0, 0, 0)),
				new FtpListItem("no-bok", 699, FtpObjectType.File, new DateTime(1995, 9, 11, 14, 0, 0, 0)),
				new FtpListItem("FILE2", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("file3", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("FILE3", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("file1", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("file2", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("File3", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("File1", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("File2", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
				new FtpListItem("FILE1", 4096, FtpObjectType.File, new DateTime(1999, 9, 29, 1, 14, 40, 0)),
			};

			TestParsing(parser, "/", sample, expected);

		}

		[Fact]
		public void Unix() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.Unix);

			var sample = new[] {

				// OK
				"drwxr-xr-x   7  user1 user1       512 Sep 27  2011 .",
				"drwxr-xr-x  31 user1  user1      1024 Sep 27  2011 ..",
				"lrwxrwxrwx   1 user1  user1      9 Sep 27  2011 data.0000 -> data.6460",
				"drwxr-xr-x  10 user1  user1      512 Jun 29  2012 data.6460",
				"lrwxrwxrwx   1 user1 user1       8 Sep 27  2011 sys.0000 -> sys.6460",
				"drwxr-xr-x 133 user1  user1     4096 Jun 25 16:26 sys.6460",
				"dr-xr-xr-x   2 root     other        512 Apr  8  1994 File001.xml dir",
				"dr-xr-xr-x   2 root                  512 Apr  8  1994 File003.xml dir",
				"lrwxrwxrwx   1 root     other          7 Jan 25 00:17 File004.xml link -> usr/bin",
				"d [R----F--] john            512       Jan 16 18:53    Folder-Videos dir",
				"- [R----F--] jacob             214059       Oct 20 15:27    File-2.txt file",
				"-------r--         326  1391972  1392298 Nov 22  1995 File-3.txt file",
				"drwxrwxr-x               folder        2 May 10  1996 Folder-Audio dir",
				"-rw-r--r--   1 group domain user 531 Jan 29 03:26 File-9.txt file",
				
				// BROKEN - name and date incorrect
				"-rw-r--r--   1 root     other        531 3 29 03:26 File002.xml file",
				"-rw-r--r--   1 root     other        531 09-26 2000 File005.xml file",
				"-rw-r--r--   1 root     other        531 09-26 13:45 File006.xml file",
				"-rw-r--r--   1 root     other        531 2005-06-07 21:22 File007.xml file",
				"-rw-r--r--   1 root     other  33.5k Oct 5 21:22 File-8.txt file",
				"+i8388621.48594,m825718503,r,s280,up755\tFile-10.txt file",
				"+i8388621.50690,m824255907,/,\tFile-11.txt dir",
				//"0100644   500  101   12345    123456789       numerical file",
				"dr-xr-xr-x   2 root     other      2235 26. Juli, 20:10 NonEnglish-1.mp4 dir",
				"dr-xr-xr-x   2 root     other      2235 szept 26 20:10 NonEnglish-2.mp4 dir",
				"-r-xr-xr-x   2 root     other      2235 2.   Okt.  2003 NonEnglish-3.mp4 file",
				"-r-xr-xr-x   2 root     other      2235 1999/10/12 17:12 NonEnglish-4.mp4 file",
				"-r-xr-xr-x   2 root     other      2235 24-04-2003 17:12 NonEnglish-5.mp4 file",
				//"-rw-r--r--   1 root       bob           8473  4\x8c\x8e 18\x93\xfa 2003\x94\x4e NonEnglish-6.mp4 file",
				//"-rwxrwxrwx   1 root     alice          0 2003   3\xed\xef 20 NonEnglish-7.mp4 file",
				//"-r--r--r-- 1 root root 2096 8\xed 17 08:52 NonEnglish-8.mp4 file",
				"-r-xr-xr-x   2 root  root  96 2004.07.15   NonEnglish-9.mp4 file",
			};

			var expected = new FtpListItem[]{

				// OK
				new FtpListItem(".", 512, FtpObjectType.Directory, new DateTime(2011, 9, 27, 0, 0, 0, 0)),
				new FtpListItem("..", 1024, FtpObjectType.Directory, new DateTime(2011, 9, 27, 0, 0, 0, 0)),
				new FtpListItem("data.0000", 9, FtpObjectType.Link, new DateTime(2011, 9, 27, 0, 0, 0, 0)),
				new FtpListItem("data.6460", 512, FtpObjectType.Directory, new DateTime(2012, 6, 29, 0, 0, 0, 0)),
				new FtpListItem("sys.0000", 8, FtpObjectType.Link, new DateTime(2011, 9, 27, 0, 0, 0, 0)),
				new FtpListItem("sys.6460", 4096, FtpObjectType.Directory, new DateTime(2022, 6, 25, 16, 26, 0, 0)),
				new FtpListItem("File001.xml dir", 512, FtpObjectType.Directory, new DateTime(1994, 4, 8, 0, 0, 0, 0)),
				new FtpListItem("File003.xml dir", 512, FtpObjectType.Directory, new DateTime(1994, 4, 8, 0, 0, 0, 0)),
				new FtpListItem("File004.xml link", 7, FtpObjectType.Link, new DateTime(2022, 1, 25, 0, 17, 0, 0)),
				new FtpListItem("Folder-Videos dir", 512, FtpObjectType.Directory, new DateTime(2022, 1, 16, 18, 53, 0, 0)),
				new FtpListItem("File-2.txt file", 214059, FtpObjectType.File, new DateTime(2021, 10, 20, 15, 27, 0, 0)),
				new FtpListItem("File-3.txt file", 1392298, FtpObjectType.File, new DateTime(1995, 11, 22, 0, 0, 0, 0)),
				new FtpListItem("Folder-Audio dir", 2, FtpObjectType.Directory, new DateTime(1996, 5, 10, 0, 0, 0, 0)),
				new FtpListItem("Jan 29 03:26 File-9.txt file", 0, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),

				// BROKEN - name and date incorrect
				new FtpListItem("file", 531, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file", 531, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file", 531, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file", 531, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("5 21:22 File-8.txt file", 0, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				null,
				null,
				new FtpListItem("dir", 2235, FtpObjectType.Directory, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("NonEnglish-2.mp4 dir", 2235, FtpObjectType.Directory, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file", 2235, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file", 2235, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("file", 2235, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("NonEnglish-9.mp4 file", 0, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
			};

			TestParsing(parser, "/", sample, expected);

		}

		[Fact]
		public void WindowsIIS() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.Windows);

			var sample = new[] {
				"03-07-13  10:02AM                  901 File01.xml",
				"03-07-13  10:03AM                  921 File02.xml",
				"03-07-13  10:04AM                  904 File03.xml",
				"03-07-13  10:04AM                  912 File04.xml",
				"03-08-13  11:10AM                  912 File05.xml",
				"03-15-13  02:38PM                  912 File06.xml",
				"03-07-13  10:16AM                  909 File07.xml",
				"03-07-13  10:16AM                  899 File08.xml",
				"03-08-13  10:22AM                  904 File09.xml",
				"03-25-13  07:27AM                  895 File10.xml",
				"03-08-13  10:22AM                 6199 File11.txt",
				"03-25-13  07:22AM                31444 File12.txt",
				"03-25-13  07:24AM                24537 File13.txt",
				"04-27-00  12:09PM       <DIR>          Folder14",
				"04-06-00  03:47PM                  589 File15",
				"2013-09-02  18:48       <DIR>          Folder16",
				"2013-09-02  19:06                9,730 File17",
			};
			var expected = new FtpListItem[]{
				new FtpListItem("File01.xml", 901, FtpObjectType.File, new DateTime(2013, 3, 7, 10, 2, 0, 0)),
				new FtpListItem("File02.xml", 921, FtpObjectType.File, new DateTime(2013, 3, 7, 10, 3, 0, 0)),
				new FtpListItem("File03.xml", 904, FtpObjectType.File, new DateTime(2013, 3, 7, 10, 4, 0, 0)),
				new FtpListItem("File04.xml", 912, FtpObjectType.File, new DateTime(2013, 3, 7, 10, 4, 0, 0)),
				new FtpListItem("File05.xml", 912, FtpObjectType.File, new DateTime(2013, 3, 8, 11, 10, 0, 0)),
				new FtpListItem("File06.xml", 912, FtpObjectType.File, new DateTime(2013, 3, 15, 14, 38, 0, 0)),
				new FtpListItem("File07.xml", 909, FtpObjectType.File, new DateTime(2013, 3, 7, 10, 16, 0, 0)),
				new FtpListItem("File08.xml", 899, FtpObjectType.File, new DateTime(2013, 3, 7, 10, 16, 0, 0)),
				new FtpListItem("File09.xml", 904, FtpObjectType.File, new DateTime(2013, 3, 8, 10, 22, 0, 0)),
				new FtpListItem("File10.xml", 895, FtpObjectType.File, new DateTime(2013, 3, 25, 7, 27, 0, 0)),
				new FtpListItem("File11.txt", 6199, FtpObjectType.File, new DateTime(2013, 3, 8, 10, 22, 0, 0)),
				new FtpListItem("File12.txt", 31444, FtpObjectType.File, new DateTime(2013, 3, 25, 7, 22, 0, 0)),
				new FtpListItem("File13.txt", 24537, FtpObjectType.File, new DateTime(2013, 3, 25, 7, 24, 0, 0)),
				new FtpListItem("Folder14", 0, FtpObjectType.Directory, new DateTime(2000, 4, 27, 12, 9, 0, 0)),
				new FtpListItem("File15", 589, FtpObjectType.File, new DateTime(2000, 4, 6, 15, 47, 0, 0)),
				new FtpListItem("Folder16", 0, FtpObjectType.Directory, new DateTime(2013, 9, 2, 18, 48, 0, 0)),
				new FtpListItem("File17", 9730, FtpObjectType.File, new DateTime(2013, 9, 2, 19, 6, 0, 0)),
			};

			TestParsing(parser, "/", sample, expected);
		}

		[Fact]
		public void OpenVMS() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.VMS, FtpParser.VMS);

			var sample = new[] {
				"411_4114.TXT;1             11  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CC_NAME_4114.TXT;1    30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CC_NUM_4114.TXT;1     30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CELL_NAME_4114.TXT;1 113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CELL_NUM_4114.TXT;1  113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"AGCY_BUDG_4114.TXT;1      63  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"CELL_SUMM_4114.TXT;1     125  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"CELL_SUMM_CHART_4114.PDF;2 95  21-MAR-2012 10:58 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114.TXT;1          17472  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_000.TXT;1        777  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_001.TXT;1        254  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_003.TXT;1         21  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_006.TXT;1         22  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_101.TXT;1        431  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_121.TXT;1       2459  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_124.TXT;1       4610  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DAT_4114_200.TXT;1        936  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"TEL_4114.TXT;1           1178  21-MAR-2012 15:19 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"pdf-dir.DIR;1  1 19-NOV-2001 21:41 [root,root] (RWE,RWE,RE,RE)",
				"pdf-file;1       155   2-JUL-2003 10:30:13.64",
				"pdf-notime-file;1    2/8    7-JAN-2000    [IV2_XXX]   (RWED,RWED,RE,)",
				"pdf-notime-file;1    6/8    15-JUI-2002    PRONAS   (RWED,RWED,RE,)",
				"pdf-multiline-file;1\r\n170774/170775     24-APR-2003 08:16:15  [FTP_CLIENT,SCOT]      (RWED,RWED,RE,)",
				"pdf-multiline-file;1\r\n10     2-JUL-2003 10:30:08.59  [FTP_CLIENT,SCOT]      (RWED,RWED,RE,)",
				//"junk-file;1   [SUMMARY]    1/3     2-AUG-2006 13:05  (RWE,RWE,RE,)",
				//"junk-file;1       17-JUN-1994 17:25:37     6308/13     (RWED,RWED,R,)",
			};

			var expected = new FtpListItem[]{
				new FtpListItem("411_4114.TXT", 11, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("ACT_CC_NAME_4114.TXT", 30, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("ACT_CC_NUM_4114.TXT", 30, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("ACT_CELL_NAME_4114.TXT", 113, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("ACT_CELL_NUM_4114.TXT", 113, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("AGCY_BUDG_4114.TXT", 63, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("CELL_SUMM_4114.TXT", 125, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("CELL_SUMM_CHART_4114.PDF", 95, FtpObjectType.File, new DateTime(2012, 3, 21, 10, 58, 0, 0)),
				new FtpListItem("DAT_4114.TXT", 17472, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 17, 0, 0)),
				new FtpListItem("DAT_4114_000.TXT", 777, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("DAT_4114_001.TXT", 254, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("DAT_4114_003.TXT", 21, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("DAT_4114_006.TXT", 22, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("DAT_4114_101.TXT", 431, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("DAT_4114_121.TXT", 2459, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("DAT_4114_124.TXT", 4610, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("DAT_4114_200.TXT", 936, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 18, 0, 0)),
				new FtpListItem("TEL_4114.TXT", 1178, FtpObjectType.File, new DateTime(2012, 3, 21, 15, 19, 0, 0)),
				new FtpListItem("pdf-dir", 1, FtpObjectType.Directory, new DateTime(2001, 11, 19, 21, 41, 0, 0)),
				new FtpListItem("pdf-file", 155, FtpObjectType.File, new DateTime(2003, 7, 2, 10, 30, 13, 640)),
				new FtpListItem("pdf-notime-file", 1048576, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("pdf-notime-file", 3145728, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("pdf-multiline-file", 89534758912, FtpObjectType.File, new DateTime(2003, 4, 24, 8, 16, 15, 0)),
				new FtpListItem("pdf-multiline-file", 10, FtpObjectType.File, new DateTime(2003, 7, 2, 10, 30, 8, 590)),
			};

			TestParsing(parser, "disk$user520:[4114.2012.Jan]", sample, expected);

		}
		[Fact]
		public void IBMOS400() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.IBMOS400, FtpParser.IBMOS400);

			var sample = new[] {
				"CFT             45056 04/12/14 14:19:31 *FILE ANTHONY1.FILE",
				"CFT                                     *MEM ANTHONY1.FILE/ANTHONY1.MBR",
				"CFT             36864 28/11/15 15:19:30 *FILE AMANDA3.FILE",
				"CFT                                     *MEM AMANDA3.FILE/AMANDA3.MBR",
				"CFT             45056 04/12/16 14:19:37 *FILE ASKET7.FILE",
				"CFT                                     *MEM  ASKET7.FILE/ASKET7.MBR",
				"QSYSOPR         28672 01/12/17 20:08:04 *FILE FPKI45POK5.FILE",
				"QSYSOPR                                 *MEM FPKI45POK5.FILE/FPKI45POK5.MBR",
			};

			var expected = new FtpListItem[]{
				new FtpListItem("ANTHONY1.FILE", 45056, FtpObjectType.File, new DateTime(2014, 12, 4, 14, 19, 31, 0)),
				new FtpListItem("ANTHONY1.FILE/ANTHONY1.MBR", 0, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("AMANDA3.FILE", 36864, FtpObjectType.File, new DateTime(2015, 11, 28, 15, 19, 30, 0)),
				new FtpListItem("AMANDA3.FILE/AMANDA3.MBR", 0, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("ASKET7.FILE", 45056, FtpObjectType.File, new DateTime(2016, 12, 4, 14, 19, 37, 0)),
				new FtpListItem("ASKET7.FILE/ASKET7.MBR", 0, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
				new FtpListItem("FPKI45POK5.FILE", 28672, FtpObjectType.File, new DateTime(2017, 12, 1, 20, 8, 4, 0)),
				new FtpListItem("FPKI45POK5.FILE/FPKI45POK5.MBR", 0, FtpObjectType.File, new DateTime(1, 1, 1, 0, 0, 0, 0)),
			};

			TestParsing(parser, "/", sample, expected);

		}
		[Fact]
		public void NonStop() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.Unix, FtpParser.NonStop);

			var sample = new[] {

				// listings will always begin with this item
				"File         Code             EOF  Last Modification    Owner  RWEP", 

				// file listing is after the header
				"FILE1 101 528 12-Jan-14 14:21:18 255, 0 \"extra1\"",
				"FILE2 101 528 13-Feb-14 14:21:18 255,255 \"extra2\"",
				"FILE3        101            16354 14-Mar-14 15:09:12 244, 10 \"extra3\"",
				"FILE4      101            16384 15-Aug-14 11:44:56 244, 10 \"extra4\"",
			};

			var expected = new FtpListItem[]{
				null,
				new FtpListItem("FILE1", 528, FtpObjectType.File, new DateTime(635251332780000000)),
				new FtpListItem("FILE2", 528, FtpObjectType.File, new DateTime(635278980780000000)),
				new FtpListItem("FILE3", 16354, FtpObjectType.File, new DateTime(635304065520000000)),
				new FtpListItem("FILE4", 16384, FtpObjectType.File, new DateTime(635436998960000000)),
			};

			TestParsing(parser, "/", sample, expected);

		}

		[Fact]
		public void IBMzOSMVS_HFS()
		{
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.IBMzOS, FtpParser.IBMzOS);

			var sample = new[] {
				"total 17904",
				"drwxrwxr-x   2 OMVSKERN SYS1        8192 Oct 19  2015 downloads",
				"drwxrwxr-x   4 OMVSKERN SYS1        8192 Oct 19  2015 p5zipfile",
				"-rw-rw----   1 YNSAS    SYS1     2723828 Dec  6  2021 t.out",
				"-rw-r-----   1 YNSAS    SYS1      132480 Jan  2  2022 test.bin",
				"-rw-rw----   1 YNSAS    SYS1     6209406 May 29  2021 test.tst",
				"-rw-rw----   1 YNSAS    SYS1       47227 Jun  7  2021 test.txt",
			};

			TestParsing(parser, "/", sample, null);

		}

		[Fact]
		public void IBMzOSMVS_PSPO()
		{
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.IBMzOS, FtpParser.IBMzOS);

			var sample = new[] {
				"Volume Unit    Referred Ext Used Recfm Lrecl BlkSz Dsorg Dsname",
				"YNSABG 3390   2020/01/03  1   15  VB   32756 32760  PS  $.ADATA.XAA",
				"YNSABH 3390   2022/02/18  1+++++  VBS  32767 27966  PS  $.BDATA.XBB",
			};

			TestParsing(parser, "/", sample, null);

		}

		[Fact]
		public void IBMzOSMVS_Member()
		{
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.IBMzOS, FtpParser.IBMzOS);

			var sample = new[] {
				" Name     VV.MM   Created       Changed      Size  Init   Mod   Id",
				"$2CPF1    01.01 2001/10/18 2001/10/18 11:58    29    29     0 QFX3076",
			};

			TestParsing(parser, "/", sample, null);

		}

		[Fact]
		public void IBMzOSMVS_MemberU()
		{
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.IBMzOS, FtpParser.IBMzOS);

			var sample = new[] {
				" Name      Size     TTR   Alias-of AC --------- Attributes --------- Amode Rmode",
				"EAGKCPT   000058   000009          00 FO             RN RU            31    ANY",
				"EAGRTPRC  005F48   000011 EAGRTALT 00 FO             RN RU            31    ANY",
			};

			TestParsing(parser, "/", sample, null);

		}

		/*[Fact]
		public void IBMOS2() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.IBMOS400);

			var sample = new[] {
				"36611      A    04-23-103  10:57  24-File2 file",
				" 1123      A    07-14-99   12:37  25-File4 file",
				"    0 DIR       02-11-103  16:15  26-Dir6 dir",
				" 1123 DIR  A    10-05-100  23:38  27-Dir8 dir",
			};

			TestParsing(parser, "/", sample, null);
		}*/
	}
}