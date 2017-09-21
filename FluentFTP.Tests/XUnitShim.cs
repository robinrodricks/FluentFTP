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
}
#endif
