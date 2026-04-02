using FluentFTP.Client.BaseClient;
using FluentFTP.Helpers;
using Xunit;

namespace FluentFTP.Tests.Unit {
	/// <summary>
	/// Test cases for FTP path sanitization to ensure the sanitizer is disabled when config settings are changed.
	/// Trimming, leading-slash and trailing-slash removal will still be enabled.
	/// </summary>
	public class PathInsecureTests {

		private static BaseFtpClient _insecureClient;

		private static BaseFtpClient GetInsecureClient() {
			if (_insecureClient == null) {
				_insecureClient = new FtpClient();
				var conf = _insecureClient.Config;
				conf.SanitizeControlChars = false;
				conf.SanitizeMultiline = false;
				conf.SanitizeTraversal = false;
				conf.SanitizeUnicodeSpoofing = false;
				conf.SanitizeUrlEncoding = false;
			}
			return _insecureClient;
		}

		/// <summary>Null should return root</summary>
		[Fact]
		public void Null_ReturnsRoot() {
			string input = null;
			Assert.Equal("/", SanitizerModule.SanitizePath(GetInsecureClient(), input));
		}

		/// <summary>Empty should return root</summary>
		[Fact]
		public void Empty_ReturnsRoot() {
			Assert.Equal("/", SanitizerModule.SanitizePath(GetInsecureClient(), ""));
		}

		/// <summary>Backslashes normalized to forward slashes</summary>
		[Fact]
		public void Backslashes_Normalized() {
			Assert.Equal("/a/b/c", SanitizerModule.SanitizePath(GetInsecureClient(), "\\a\\b\\c"));
		}

		/// <summary>Multiple slashes collapsed</summary>
		[Fact]
		public void MultipleSlashes_Collapsed() {
			Assert.Equal("/a/b", SanitizerModule.SanitizePath(GetInsecureClient(), "///a////b"));
		}

		/// <summary>Trailing slash removed</summary>
		[Fact]
		public void TrailingSlash_Removed() {
			Assert.Equal("/a/b", SanitizerModule.SanitizePath(GetInsecureClient(), "/a/b/"));
		}

		/// <summary>CRLF injection removed</summary>
		[Fact]
		public void CRLF_Removed() {
			var path = "/safe\r\nDELE file";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>CR-only injection removed</summary>
		[Fact]
		public void CR_Only_Removed() {
			var path = "/safe\rDELE file";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>LF-only injection removed</summary>
		[Fact]
		public void LF_Only_Removed() {
			var path = "/safe\nDELE file";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Leading newline results in root</summary>
		[Fact]
		public void LeadingNewline_ReturnsRoot() {
			var path = "\r\n/safe";
			Assert.Equal("/safe", SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Only slashes normalize to root</summary>
		[Fact]
		public void OnlySlashes_ReturnRoot() {
			Assert.Equal("/", SanitizerModule.SanitizePath(GetInsecureClient(), "////"));
		}

		/// <summary>Dot traversal must be removed</summary>
		[Fact]
		public void Traversal_Removed() {
			var path = "/../../etc/passwd";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Encoded traversal must be decoded and removed</summary>
		[Fact]
		public void EncodedTraversal_Removed() {
			var path = "/%2e%2e/%2e%2e/etc/passwd";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Double encoded traversal must be resolved</summary>
		[Fact]
		public void DoubleEncodedTraversal_Removed() {
			var path = "/%252e%252e/%252e%252e/etc/passwd";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Null bytes must be stripped, along with the payload after them</summary>
		[Fact]
		public void NullByte_Removed() {
			var path = "/file.txt\0.jpg";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Control characters must be stripped, along with the payload after them</summary>
		[Fact]
		public void ControlChars_Removed() {
			var path = "/file\t\n\r.txt";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Command chaining characters must be removed, along with the payload after them</summary>
		[Fact]
		public void CommandChars_Removed() {
			var path = "/file.txt;rm -rf /";
			Assert.Equal("/file.txt;rm -rf", SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Pipe injection must be removed, along with the payload after them</summary>
		[Fact]
		public void Pipe_Removed() {
			var path = "/file.txt|whoami";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Whitespace should be trimmed</summary>
		[Fact]
		public void Whitespace_Trimmed1() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(GetInsecureClient(), "   /file.txt   "));
		}

		/// <summary>Whitespace should be trimmed</summary>
		[Fact]
		public void Whitespace_Trimmed2() {
			Assert.Equal("/file.txt", SanitizerModule.SanitizePath(GetInsecureClient(), "/file.txt   "));
		}

		/// <summary>Whitespace in filenames should be preserved</summary>
		[Fact]
		public void Whitespace_Preserved1() {
			Assert.Equal("/file name.txt", SanitizerModule.SanitizePath(GetInsecureClient(), "/file name.txt"));
		}

		/// <summary>Whitespace in filenames should be preserved</summary>
		[Fact]
		public void Whitespace_Preserved2() {
			Assert.Equal("/file name.txt", SanitizerModule.SanitizePath(GetInsecureClient(), "  /file name.txt "));
		}

		/// <summary>Unicode spoofing characters should be removed in-place</summary>
		[Fact]
		public void UnicodeControl_Removed() {
			var path = "/safe/\u202Etxt.exe";
			Assert.Equal(path, SanitizerModule.SanitizePath(GetInsecureClient(), path));
		}

		/// <summary>Leading newline should result in empty -> normalized to root</summary>
		[Fact]
		public void Multiline_LeadingNewlineNormalizesoRoot() {
			string input = "\r\n/safe/path";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal("/safe/path", result);
		}

		/// <summary>CRLF injection should strip everything after first line</summary>
		[Fact]
		public void MultilineInjection_CRLF1() {
			string input = "/safe/path\r\nDELE file.txt";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>Basic CRLF injection should only keep first line</summary>
		[Fact]
		public void MultilineInjection_CRLF2() {
			string input = "/safe\r\nMALICIOUS";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>Multiple commands chained across many lines</summary>
		[Fact]
		public void MultilineInjection_CRLF3() {
			string input = "/safe\r\nDELE a\r\nDELE b\r\nQUIT";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>LF-only injection should strip everything after first line</summary>
		[Fact]
		public void MultilineInjection_LF() {
			string input = "/safe/path\nDELE file.txt";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>CRLF command injection with multiple commands should be neutralized</summary>
		[Fact]
		public void MultilineInjection_CRLF4() {
			string input = "/safe/path\r\nDELE file\r\nSTOR hack.txt\r\nQUIT";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>CR-only injection should strip everything after first line</summary>
		[Fact]
		public void MultilineInjection_CR() {
			string input = "/safe/path\rDELE file.txt";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>Mixed newline chaos should still strip after first line</summary>
		[Fact]
		public void MultilineInjection_MixedNewlines() {
			string input = "/safe/path\r\n\n\rDELETE";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>Multiple newline types mixed should still truncate correctly</summary>
		[Fact]
		public void MultilineInjection_MixedNewlineTypesTruncate() {
			string input = "/safe/path\n\r\nSTOR hack.txt";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

		/// <summary>Encoded CRLF should be decoded and stripped</summary>
		[Fact]
		public void MultilineInjection_EncodedCRLF() {
			string input = "/safe%0D%0ADELE file";
			string result = SanitizerModule.SanitizePath(GetInsecureClient(), input);
			Assert.Equal(input, result);
		}

	}
}