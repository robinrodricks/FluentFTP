using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Tests.Integration.Skippable
{
	internal static class SkippableState
	{
		internal static bool ShouldSkip { get; set; }
	}
}
