using FluentFTP.Helpers;
using FluentFTP.Rules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace FluentFTP.Tests.Unit {

	/// <summary>
	/// Text from the server and from FTP commands must be compared the same way whatever the
	/// client's culture is. In Turkish, "I".ToLower() is "ı" and "i".ToUpper() is "İ".
	/// </summary>
	public class CultureTests {

		private static void InTurkishCulture(Action action) {
			var previousCulture = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
			try {
				action();
			}
			finally {
				CultureInfo.CurrentCulture = previousCulture;
			}
		}

		private class SystClient : FtpClient {
			public SystClient(string systemType) {
				m_systemType = systemType;
			}
		}

		[Fact]
		public void ContainsAnyCIMatchesUpperCaseRepliesInTurkishCulture() {
			InTurkishCulture(() => {
				Assert.True("550 FAILED TO OPEN FILE".ContainsAnyCI(new[] { "failed to open file" }));
			});
		}

		[Fact]
		public void FileExtensionRuleMatchesUpperCaseExtensionsInTurkishCulture() {
			InTurkishCulture(() => {
				var rule = new FtpFileExtensionRule(true, new List<string> { "ini" });
				var item = new FtpListItem { Name = "SETUP.INI", Type = FtpObjectType.File };
				Assert.True(rule.IsAllowed(item));
			});
		}

		[Fact]
		public void WindowsSystIsDetectedInTurkishCulture() {
			var detect = typeof(FtpClient).Assembly
				.GetType("FluentFTP.Client.Modules.ServerModule", true)!
				.GetMethod("DetectFtpOSBySyst", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

			InTurkishCulture(() => {
				var os = (FtpOperatingSystem)detect.Invoke(null, new object[] { new SystClient("Windows_NT") })!;
				Assert.Equal(FtpOperatingSystem.Windows, os);
			});
		}

		[Fact]
		public void LowerCaseFeaturesAreDetectedInTurkishCulture() {
			var detect = typeof(FtpClient).Assembly
				.GetType("FluentFTP.Client.Modules.ServerFeatureModule", true)!
				.GetMethod("Detect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

			InTurkishCulture(() => {
				var capabilities = new List<FtpCapability>();
				var args = new object[] { capabilities, FtpHashAlgorithm.NONE, new[] { "size", "site" } };
				detect.Invoke(null, args);
				Assert.Contains(FtpCapability.SIZE, capabilities);
			});
		}
	}
}
