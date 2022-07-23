using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace FluentFTP.Tests {
	public class ParserTests {

		private static void TestParsing(FtpListParser parser, string path, string[] testValues, FtpListItem[] expectedValues) {
			// per test value
			for (int i = 0; i < testValues.Length; i++) {
				var test = testValues[i];

				// parse it
				var item = parser.ParseSingleLine(path, test, new List<FtpCapability>(), false);

				// print parsed value
#if DEBUG
				if (item != null) {
					Debug.WriteLine(item.ToCode() + ",");
				}
#endif

				// test if correct
				Assert.Equal(item.ToString(), expectedValues[i].ToString());

			}
		}

		[Fact]
		public void Unix() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.Unix);

			var sample = new[] {
				"drwxr-xr-x   7  user1 user1       512 Sep 27  2011 .",
				"drwxr-xr-x  31 user1  user1      1024 Sep 27  2011 ..",
				"lrwxrwxrwx   1 user1  user1      9 Sep 27  2011 data.0000 -> data.6460",
				"drwxr-xr-x  10 user1  user1      512 Jun 29  2012 data.6460",
				"lrwxrwxrwx   1 user1 user1       8 Sep 27  2011 sys.0000 -> sys.6460",
				"drwxr-xr-x 133 user1  user1     4096 Jun 25 16:26 sys.6460"
			};

			var expected = new FtpListItem[]{
			new FtpListItem("drwxr-xr-x   7  user1 user1       512 Sep 27  2011 .",".",512,true,new DateTime(634526784000000000)),
			new FtpListItem("drwxr-xr-x  31 user1  user1      1024 Sep 27  2011 ..","..",1024,true,new DateTime(634526784000000000)),
			new FtpListItem("lrwxrwxrwx   1 user1  user1      9 Sep 27  2011 data.0000 -> data.6460","data.0000",9,FtpFileSystemObjectType.Link,new DateTime(634526784000000000)),
			new FtpListItem("drwxr-xr-x  10 user1  user1      512 Jun 29  2012 data.6460","data.6460",512,true,new DateTime(634765248000000000)),
			new FtpListItem("lrwxrwxrwx   1 user1 user1       8 Sep 27  2011 sys.0000 -> sys.6460","sys.0000",8,FtpFileSystemObjectType.Link,new DateTime(634526784000000000)),
			new FtpListItem("drwxr-xr-x 133 user1  user1     4096 Jun 25 16:26 sys.6460","sys.6460",4096,true,new DateTime(637917711600000000)),
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
				"03-25-13  07:24AM                24537 File13.txt"
			};
			var expected = new FtpListItem[]{
			new FtpListItem("03-07-13  10:02AM                  901 File01.xml","File01.xml",901,false,new DateTime(634982473200000000)),
			new FtpListItem("03-07-13  10:03AM                  921 File02.xml","File02.xml",921,false,new DateTime(634982473800000000)),
			new FtpListItem("03-07-13  10:04AM                  904 File03.xml","File03.xml",904,false,new DateTime(634982474400000000)),
			new FtpListItem("03-07-13  10:04AM                  912 File04.xml","File04.xml",912,false,new DateTime(634982474400000000)),
			new FtpListItem("03-08-13  11:10AM                  912 File05.xml","File05.xml",912,false,new DateTime(634983378000000000)),
			new FtpListItem("03-15-13  02:38PM                  912 File06.xml","File06.xml",912,false,new DateTime(634989550800000000)),
			new FtpListItem("03-07-13  10:16AM                  909 File07.xml","File07.xml",909,false,new DateTime(634982481600000000)),
			new FtpListItem("03-07-13  10:16AM                  899 File08.xml","File08.xml",899,false,new DateTime(634982481600000000)),
			new FtpListItem("03-08-13  10:22AM                  904 File09.xml","File09.xml",904,false,new DateTime(634983349200000000)),
			new FtpListItem("03-25-13  07:27AM                  895 File10.xml","File10.xml",895,false,new DateTime(634997932200000000)),
			new FtpListItem("03-08-13  10:22AM                 6199 File11.txt","File11.txt",6199,false,new DateTime(634983349200000000)),
			new FtpListItem("03-25-13  07:22AM                31444 File12.txt","File12.txt",31444,false,new DateTime(634997929200000000)),
			new FtpListItem("03-25-13  07:24AM                24537 File13.txt","File13.txt",24537,false,new DateTime(634997930400000000)),
		};

			TestParsing(parser, "/", sample, expected);
		}

		[Fact]
		public void OpenVMS() {
			var parser = new FtpListParser(new FtpClient());
			parser.Init(FtpOperatingSystem.VMS);

			var sample = new[] {
			"411_4114.TXT;1             11  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"ACT_CC_NAME_4114.TXT;1    30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"ACT_CC_NUM_4114.TXT;1     30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"ACT_CELL_NAME_4114.TXT;1 113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"ACT_CELL_NUM_4114.TXT;1  113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"AGCY_BUDG_4114.TXT;1      63  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"CELL_SUMM_4114.TXT;1     125  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"CELL_SUMM_CHART_4114.PDF;2 95  21-MAR-2012 10:58 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114.TXT;1          17472  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_000.TXT;1        777  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_001.TXT;1        254  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_003.TXT;1         21  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_006.TXT;1         22  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_101.TXT;1        431  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_121.TXT;1       2459  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_124.TXT;1       4610  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"DET_4114_200.TXT;1        936  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
			"TEL_4114.TXT;1           1178  21-MAR-2012 15:19 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)"
		};
			var expected = new FtpListItem[]{
			new FtpListItem("411_4114.TXT;1             11  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","411_4114.TXT",11,false,new DateTime(634679398200000000)),
			new FtpListItem("ACT_CC_NAME_4114.TXT;1    30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","ACT_CC_NAME_4114.TXT",30,false,new DateTime(634679398200000000)),
			new FtpListItem("ACT_CC_NUM_4114.TXT;1     30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","ACT_CC_NUM_4114.TXT",30,false,new DateTime(634679398200000000)),
			new FtpListItem("ACT_CELL_NAME_4114.TXT;1 113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","ACT_CELL_NAME_4114.TXT",113,false,new DateTime(634679398200000000)),
			new FtpListItem("ACT_CELL_NUM_4114.TXT;1  113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","ACT_CELL_NUM_4114.TXT",113,false,new DateTime(634679398200000000)),
			new FtpListItem("AGCY_BUDG_4114.TXT;1      63  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","AGCY_BUDG_4114.TXT",63,false,new DateTime(634679398200000000)),
			new FtpListItem("CELL_SUMM_4114.TXT;1     125  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","CELL_SUMM_4114.TXT",125,false,new DateTime(634679398200000000)),
			new FtpListItem("CELL_SUMM_CHART_4114.PDF;2 95  21-MAR-2012 10:58 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","CELL_SUMM_CHART_4114.PDF",95,false,new DateTime(634679242800000000)),
			new FtpListItem("DET_4114.TXT;1          17472  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114.TXT",17472,false,new DateTime(634679398200000000)),
			new FtpListItem("DET_4114_000.TXT;1        777  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_000.TXT",777,false,new DateTime(634679398800000000)),
			new FtpListItem("DET_4114_001.TXT;1        254  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_001.TXT",254,false,new DateTime(634679398800000000)),
			new FtpListItem("DET_4114_003.TXT;1         21  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_003.TXT",21,false,new DateTime(634679398800000000)),
			new FtpListItem("DET_4114_006.TXT;1         22  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_006.TXT",22,false,new DateTime(634679398800000000)),
			new FtpListItem("DET_4114_101.TXT;1        431  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_101.TXT",431,false,new DateTime(634679398800000000)),
			new FtpListItem("DET_4114_121.TXT;1       2459  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_121.TXT",2459,false,new DateTime(634679398800000000)),
			new FtpListItem("DET_4114_124.TXT;1       4610  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_124.TXT",4610,false,new DateTime(634679398800000000)),
			new FtpListItem("DET_4114_200.TXT;1        936  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","DET_4114_200.TXT",936,false,new DateTime(634679398800000000)),
			new FtpListItem("TEL_4114.TXT;1           1178  21-MAR-2012 15:19 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)","TEL_4114.TXT",1178,false,new DateTime(634679399400000000)),

		};

			TestParsing(parser, "disk$user520:[4114.2012.Jan]", sample, expected);

		}

	}
}