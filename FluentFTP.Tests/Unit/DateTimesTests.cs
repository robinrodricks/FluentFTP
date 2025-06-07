using System;
using System.Collections.Generic;
using System.Linq;
using FluentFTP.Client.BaseClient;
using FluentFTP.Helpers;
using Xunit;

namespace FluentFTP.Tests.Unit;

public class DateTimesTests {

	[MemberData(nameof(SupportedDateFormats))]
	[Theory]
	public void ParseFtpDate_Built_In_Formats((string DateString, DateTime ExpectedDate) input) {

		var client = new BaseFtpClient(new FtpConfig());

		var actual = DateTimes.ParseFtpDate(input.DateString, client);

		Assert.Equal(input.ExpectedDate, actual);
	}

	[Fact]
	public void ParseFtpDate_Unsupported_Format() {

		var client = new BaseFtpClient(new FtpConfig());
		var logger = new TestLogger();
		client.Logger = logger;

		var unsupportedDateString = "30 May 1985";
		var actual = DateTimes.ParseFtpDate(unsupportedDateString, client);
		Assert.Equal(DateTime.MinValue, actual);

		Assert.Single(logger.LogEntries);
		var entry = logger.LogEntries.Single();
		Assert.Equal(FtpTraceLevel.Error, entry.Severity);
		Assert.Equal($"Failed to parse date string '{unsupportedDateString}'", entry.Message);
	}

	[Fact]
	public void ParseFtpDate_Custom_Format() {
		
		var client = new BaseFtpClient(new FtpConfig());

		var expected = new DateTime(1985, 5, 30);

		var actual = DateTimes.ParseFtpDate("30 May 1985", client, new[] { "dd MMM yyyy" });
		Assert.Equal(expected, actual);
	}

	public static TheoryData<(string DateString, DateTime ExpectedDate)> SupportedDateFormats {
		get {
			var expected = new DateTime(1985, 5, 30, 6, 8, 25);
			var nowY = DateTime.Now.Year;
			return new () {
				("19850530060825", expected), // yyyyMMddHHmmss
				("19850530060825.1", expected.AddMilliseconds(100)), // yyyyMMddHHmmss'.'f
				("19850530060825.12", expected.AddMilliseconds(120)), // yyyyMMddHHmmss'.'ff
				("19850530060825.123", expected.AddMilliseconds(123)), // yyyyMMddHHmmss'.'fff
				("May 30  1985", new DateTime(1985, 5, 30)), // MMM dd  yyyy
				("May  30  1985", new DateTime(1985, 5, 30)), // MMM  d  yyyy
				("May 30 06:08", new DateTime(nowY, 5, 30, 6, 8, 0)), // MMM dd HH:mm
				("May  30 06:08", new DateTime(nowY, 5, 30, 6, 8, 0)) // MMM  d HH:mm
			};
		}
	}

	private class TestLogger : IFtpLogger {

		public IList<FtpLogEntry> LogEntries { get; } = new List<FtpLogEntry>();

		public void Log(FtpLogEntry entry) {
			LogEntries.Add(entry);
		}
	}
}