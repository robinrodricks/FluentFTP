#if SHIM_XUNIT
using System;

namespace Xunit
{
	public sealed class FactAttribute : Attribute { }
	public sealed class TheoryAttribute : Attribute { }
	public sealed class InlineDataAttribute : Attribute
	{
		public InlineDataAttribute(params object[] data) { }
	}
	public sealed class TraitAttribute : Attribute
	{
		public TraitAttribute(string name, string value) { }
	}

	public static class Assert
	{
		public static T Throws<T>(Action testCode) where T : Exception => default(T);
	}
}
#endif
