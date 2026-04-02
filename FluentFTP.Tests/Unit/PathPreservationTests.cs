using FluentFTP.Helpers;
using FluentFTP.Model.Functions;
using System;
using System.Text;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class PathPreservationTests {

		/// <summary>VMS style path should be preserved</summary>
		[Fact]
		public void VMS_Path_Basic() {
			string input = "DISK:[DIR.SUBDIR]FILE.TXT";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("DISK:[DIR.SUBDIR]FILE.TXT", result);
		}

		/// <summary>VMS path with nested directories</summary>
		[Fact]
		public void VMS_Path_Nested() {
			string input = "DISK:[DIR.SUB1.SUB2]FILE.DAT";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("DISK:[DIR.SUB1.SUB2]FILE.DAT", result);
		}

		/// <summary>IBM z/OS dataset path should be preserved</summary>
		[Fact]
		public void ZOS_Dataset_Path() {
			string input = "HLQ.DATA.SET";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("HLQ.DATA.SET", result);
		}

		/// <summary>IBM z/OS dataset member should be preserved</summary>
		[Fact]
		public void ZOS_Dataset_Member() {
			string input = "HLQ.DATA.SET(MEMBER)";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("HLQ.DATA.SET(MEMBER)", result);
		}

		/// <summary>IBM i (OS/400) library/file.member format</summary>
		[Fact]
		public void OS400_Library_Path() {
			string input = "LIBRARY/FILE.MEMBER";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("LIBRARY/FILE.MEMBER", result);
		}

		/// <summary>Mixed UNIX + dataset style should be preserved</summary>
		[Fact]
		public void Mixed_Unix_ZOS_Path() {
			string input = "/root/HLQ.DATA.SET(MEMBER)";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/root/HLQ.DATA.SET(MEMBER)", result);
		}

		/// <summary>Dots in filenames should NOT be altered</summary>
		[Fact]
		public void Multiple_Dots_FileName() {
			string input = "/dir/file.name.with.dots.txt";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/dir/file.name.with.dots.txt", result);
		}

		/// <summary>Brackets should be preserved (VMS compatibility)</summary>
		[Fact]
		public void Brackets() {
			string input = "/[DIR.SUB]FILE.TXT";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/[DIR.SUB]FILE.TXT", result);
		}

		/// <summary>Parentheses should be preserved (z/OS members)</summary>
		[Fact]
		public void Parentheses() {
			string input = "/DATA.SET(MEMBER)";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/DATA.SET(MEMBER)", result);
		}

		/// <summary>Underscores and hyphens should be preserved</summary>
		[Fact]
		public void UnderscoreAndHyphen() {
			string input = "/dir_name/file-name_01.txt";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/dir_name/file-name_01.txt", result);
		}

	}
}
