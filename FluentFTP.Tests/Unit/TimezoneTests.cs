using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class TimezoneTests {

		[Fact]
		public void SingaporeToUTC() {

			using (var client = new FtpClient("localhost", "user", "pass")) {

				// input date = 19/Feb/2020 20:30 (8:30 PM) in Singapore
				// output date = 19/Feb/2020 12:30 (12:30 PM) in UTC

				// convert to UTC (Singapore to UTC)
				AssertConvertedDateTime(client, FtpDate.UTC, "Singapore Standard Time", "Asia/Singapore", "20200219203000", "2020-02-19T12:30:00.0000000");
			}
		}
		[Fact]
		public void SingaporeToLocal() {
			using (var client = new FtpClient("localhost", "user", "pass")) {

				// input date = 19/Feb/2020 20:30 (8:30 PM) in Singapore
				// output date = 19/Feb/2020 18:00 (6:00 PM) in Mumbai

				// convert to local time (Singapore to Mumbai)
				AssertConvertedDateTime(client, FtpDate.LocalTime, "Singapore Standard Time", "Asia/Singapore", "20200219203000", "2020-02-19T18:00:00.0000000");

			}
		}

		[Fact]
		public void TokyoToUTC() {

			using (var client = new FtpClient("localhost", "user", "pass")) {

				// input date = 19/Feb/2020 00:00 (12 AM) in Tokyo
				// output date = 19/Feb/2020 15:00 (3 PM) in UTC

				// convert to UTC (Tokyo to UTC)
				AssertConvertedDateTime(client, FtpDate.UTC, "Tokyo Standard Time", "Asia/Tokyo", "20200219000000", "2020-02-18T15:00:00.0000000");
			}
		}
		[Fact]
		public void TokyoToLocal() {
			using (var client = new FtpClient("localhost", "user", "pass")) {

				// input date = 19/Feb/2020 00:00 (12 AM) in Tokyo
				// output date = 19/Feb/2020 20:30 (8:30 PM) in Mumbai

				// convert to local time (Tokyo to Mumbai)
				AssertConvertedDateTime(client, FtpDate.LocalTime, "Tokyo Standard Time", "Asia/Tokyo", "20200219000000", "2020-02-18T20:30:00.0000000");

			}
		}

		private static void AssertConvertedDateTime(FtpClient client, FtpDate conversion, string winTZ, string unixTZ, string input, string expected) {
			client.Config.TimeConversion = conversion;

			// set server TZ as per args
			client.Config.SetServerTimeZone(winTZ, unixTZ);

			// set client TZ to Mumbai
			client.Config.SetClientTimeZone("India Standard Time", "Asia/Kolkata");

			var result = client.ConvertDate(input.ParseFtpDate(client));
			var expected2 = DateTime.ParseExact(expected, "yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);

			Assert.Equal(expected2, result);
		}
	}
}