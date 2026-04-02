using FluentFTP.Helpers;
using FluentFTP.Model.Functions;
using System;
using System.Text;
using Xunit;

namespace FluentFTP.Tests.Unit {
	/// <summary>
	/// Test cases for FTP path sanitization related to whitespace, control char handling, and multi-line handling.
	/// </summary>
	public class PathSanitizerTests {

		/// <summary>Null should return root</summary>
		[Fact]
		public void Null_ReturnsRoot() {
			string input = null;
			Assert.Equal("/", SanitizerModule.SanitizePath(null, input));
		}

		/// <summary>Empty should return root</summary>
		[Fact]
		public void Empty_ReturnsRoot() {
			Assert.Equal("/", SanitizerModule.SanitizePath(null, ""));
		}

		/// <summary>Backslashes normalized to forward slashes</summary>
		[Fact]
		public void Backslashes_Normalized() {
			Assert.Equal("/a/b/c", SanitizerModule.SanitizePath(null, "\\a\\b\\c"));
		}

		/// <summary>Multiple slashes collapsed</summary>
		[Fact]
		public void MultipleSlashes_Collapsed() {
			Assert.Equal("/a/b", SanitizerModule.SanitizePath(null, "///a////b"));
		}

		/// <summary>Trailing slash removed</summary>
		[Fact]
		public void TrailingSlash_Removed() {
			Assert.Equal("/a/b", SanitizerModule.SanitizePath(null, "/a/b/"));
		}

		/// <summary>CRLF injection removed</summary>
		[Fact]
		public void CRLF_Removed() {
			Assert.Equal("/safe", SanitizerModule.SanitizePath(null, "/safe\r\nDELE file"));
		}

		/// <summary>CR-only injection removed</summary>
		[Fact]
		public void CR_Only_Removed() {
			Assert.Equal("/safe", SanitizerModule.SanitizePath(null, "/safe\rDELE file"));
		}

		/// <summary>LF-only injection removed</summary>
		[Fact]
		public void LF_Only_Removed() {
			Assert.Equal("/safe", SanitizerModule.SanitizePath(null, "/safe\nDELE file"));
		}

		/// <summary>Leading newline results in root</summary>
		[Fact]
		public void LeadingNewline_ReturnsRoot() {
			Assert.Equal("/safe", SanitizerModule.SanitizePath(null, "\r\n/safe"));
		}

		/// <summary>Only slashes normalize to root</summary>
		[Fact]
		public void OnlySlashes_ReturnRoot() {
			Assert.Equal("/", SanitizerModule.SanitizePath(null, "////"));
		}

		/// <summary>Dot traversal must be removed</summary>
		[Fact]
		public void Traversal_Removed() {
			Assert.Equal("/etc/passwd", SanitizerModule.SanitizePath(null, "/../../etc/passwd"));
		}

		/// <summary>Encoded traversal must be decoded and removed</summary>
		[Fact]
		public void EncodedTraversal_Removed() {
			Assert.Equal("/etc/passwd", SanitizerModule.SanitizePath(null, "/%2e%2e/%2e%2e/etc/passwd"));
		}

		/// <summary>Double encoded traversal must be resolved</summary>
		[Fact]
		public void DoubleEncodedTraversal_Removed() {
			Assert.Equal("/etc/passwd", SanitizerModule.SanitizePath(null, "/%252e%252e/%252e%252e/etc/passwd"));
		}

		/// <summary>Null bytes must be stripped, along with the payload after them</summary>
		[Fact]
		public void NullByte_Removed() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(null, "/file.txt\0.jpg"));
		}

		/// <summary>Control characters must be stripped, along with the payload after them</summary>
		[Fact]
		public void ControlChars_Removed() {
			Assert.Equal("/file", SanitizerModule.SanitizePath(null, "/file\t\n\r.txt"));
		}

		/// <summary>Command chaining characters must be removed, along with the payload after them</summary>
		[Fact]
		public void CommandChars_Removed() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(null, "/file.txt;rm -rf /"));
		}

		/// <summary>Pipe injection must be removed, along with the payload after them</summary>
		[Fact]
		public void Pipe_Removed() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(null, "/file.txt|whoami"));
		}

		/// <summary>Logical operators must be removed, along with the payload after them</summary>
		/*[Fact]
		public void LogicalOperators_Removed() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(null, "/file.txt && rm -rf /"));
		}*/

		/// <summary>Whitespace should be trimmed</summary>
		[Fact]
		public void Whitespace_Trimmed1() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(null, "   /file.txt   "));
		}

		/// <summary>Whitespace should be trimmed</summary>
		[Fact]
		public void Whitespace_Trimmed2() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(null, "/file.txt   "));
		}

		/// <summary>Whitespace in filenames should be preserved</summary>
		[Fact]
		public void Whitespace_Preserved1() {
			Assert.Equal("/file name.txt", SanitizerModule.SanitizePath(null, "/file name.txt"));
		}

		/// <summary>Whitespace in filenames should be preserved</summary>
		[Fact]
		public void Whitespace_Preserved2() {
			Assert.Equal("/file name.txt", SanitizerModule.SanitizePath(null, "  /file name.txt "));
		}

		/// <summary>Unicode spoofing characters should be removed in-place</summary>
		[Fact]
		public void UnicodeControl_Removed() {
			Assert.Equal("/safe/txt.exe", SanitizerModule.SanitizePath(null, "/safe/\u202Etxt.exe"));
		}

		/// <summary>Leading newline should result in empty -> normalized to root</summary>
		[Fact]
		public void Multiline_LeadingNewlineNormalizesoRoot() {
			string input = "\r\n/safe/path";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>CRLF injection should strip everything after first line</summary>
		[Fact]
		public void MultilineInjection_CRLF1() {
			string input = "/safe/path\r\nDELE file.txt";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>Basic CRLF injection should only keep first line</summary>
		[Fact]
		public void MultilineInjection_CRLF2() {
			string input = "/safe\r\nMALICIOUS";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe", result);
		}

		/// <summary>Multiple commands chained across many lines</summary>
		[Fact]
		public void MultilineInjection_CRLF3() {
			string input = "/safe\r\nDELE a\r\nDELE b\r\nQUIT";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe", result);
		}

		/// <summary>LF-only injection should strip everything after first line</summary>
		[Fact]
		public void MultilineInjection_LF() {
			string input = "/safe/path\nDELE file.txt";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>CRLF command injection with multiple commands should be neutralized</summary>
		[Fact]
		public void MultilineInjection_CRLF4() {
			string input = "/safe/path\r\nDELE file\r\nSTOR hack.txt\r\nQUIT";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>CR-only injection should strip everything after first line</summary>
		[Fact]
		public void MultilineInjection_CR() {
			string input = "/safe/path\rDELE file.txt";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>Mixed newline chaos should still strip after first line</summary>
		[Fact]
		public void MultilineInjection_MixedNewlines() {
			string input = "/safe/path\r\n\n\rDELETE";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>Multiple newline types mixed should still truncate correctly</summary>
		[Fact]
		public void MultilineInjection_MixedNewlineTypesTruncate() {
			string input = "/safe/path\n\r\nSTOR hack.txt";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>Encoded CRLF should be decoded and stripped</summary>
		[Fact]
		public void MultilineInjection_EncodedCRLF() {
			string input = "/safe%0D%0ADELE file";
			string result = SanitizerModule.SanitizePath(null, input);
			Assert.Equal("/safe", result);
		}

	}
}