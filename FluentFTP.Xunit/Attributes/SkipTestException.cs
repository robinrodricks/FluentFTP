
namespace FluentFTP.Xunit.Attributes {
	public class SkipTestException : Exception {
		public SkipTestException(string reason)
			: base(reason) { }
	}
}