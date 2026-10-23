using System.Collections.Generic;
using FluentFTP.Helpers;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class ListingIncludeSelfTests {

		private class TestClient : AsyncFtpClient {
			public TestClient() {
				CurrentListParser.Init(FtpOperatingSystem.Unix, FtpParser.Unix);
			}

			public bool Load(string line, bool isIncludeSelf, List<FtpListItem> lst) {
				var path = "/dir/";
				FtpListItem? item = null;
				int i = 0;
				return LoadBasicListingInfo(ref path, ref item!, lst, new List<string> { line }, ref i, "LIST", line, false, isIncludeSelf, false);
			}
		}

		private const string SelfLine = "drwxr-xr-x    2 user     group        4096 Jan 01 12:00 .";
		private const string ParentLine = "drwxr-xr-x    3 user     group        4096 Jan 01 12:00 ..";
		private const string FileLine = "-rw-r--r--    1 user     group          10 Jan 01 12:00 file.txt";

		[Theory]
		[InlineData(SelfLine)]
		[InlineData(ParentLine)]
		public void SelfAndParentAreSkippedWithoutIncludeSelf(string line) {
			var lst = new List<FtpListItem>();
			Assert.False(new TestClient().Load(line, false, lst));
			Assert.Empty(lst);
		}

		[Theory]
		[InlineData(SelfLine)]
		[InlineData(ParentLine)]
		public void SelfAndParentAreKeptWithIncludeSelf(string line) {
			var lst = new List<FtpListItem>();
			Assert.True(new TestClient().Load(line, true, lst));
			Assert.Single(lst);
		}

		[Fact]
		public void RegularItemIsKept() {
			var lst = new List<FtpListItem>();
			Assert.True(new TestClient().Load(FileLine, false, lst));
			Assert.Single(lst);
		}
	}
}
