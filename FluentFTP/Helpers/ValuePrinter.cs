using System.Collections;
using System.Reflection;
using System.Text;

namespace FluentFTP.Helpers {
	public static class ValuePrinter {

		public static string ObjectToString(this object obj) {
			if (obj == null) {
				// print null
				return "null";
			}

#if NET5_0_OR_GREATER
			// AOT-safe fallback for modern .NET.
			// Relies on classes overriding ToString() (like FtpAutoDetectConfig)
			// instead of using reflection to read properties dynamically.
			return obj.ToString();
#else
			var type = obj.GetType();
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

			if (properties.Length == 0) {
				return obj.ToString();
			}

			var lastProp = properties[properties.Length - 1];

			// print list
			StringBuilder result = new StringBuilder();
			foreach (var property in properties) {
				string p = property.Name;
				object v = property.GetValue(obj);

				result.Append(p);
				result.Append(" = ");
				result.Append(ValueToString(v));

				if (property != lastProp) {
					result.Append(", ");
				}
			}

			return result.ToString();
#endif
		}

		internal static string ValueToString(object v) {
			string txt;
			if (v == null) {
				// print null
				txt = "null";
			}
			else if (v is string) {
				// print string
				txt = "\"" + v + "\"";
			}
			else if (v is IList list) {
				// print list
				var vals = new StringBuilder();
				vals.Append('[');
				for (int i = 0; i < list.Count; i++) {
					vals.Append(ValueToString(list[i]));
					if (i != (list.Count - 1)) {
						vals.Append(", ");
					}
				}
				vals.Append(']');
				txt = vals.ToString();
			}
			else {
				// print any
				txt = v.ToString();
			}

			return txt;
		}

	}
}
