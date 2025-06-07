namespace FluentFTP.Xunit.Docker {
	public static class DockerFtpConfig {

		/// <summary>
		/// Detect if running inside GitHub Actions CI pipeline.
		/// </summary>
		public static bool IsCI = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

		public static string FtpUser = "fluentuser";

		public static string FtpPass = "fluentpass";

	}
}
