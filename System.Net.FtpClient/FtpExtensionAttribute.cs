using System;
using System.Collections.Generic;
using System.Text;

#if NET2
///////
// support for extension methods the 2.0 framework
///////
namespace System.Runtime.CompilerServices {
    public sealed class ExtensionAttribute : Attribute { }
}
#endif
