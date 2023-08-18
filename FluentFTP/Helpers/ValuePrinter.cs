using System.Collections;
using System.Reflection;
using System.Text;

namespace FluentFTP.Helpers {
	public static class ValuePrinter {

		public static string ObjectToString(this object obj) {
			if (obj == null) {
				// print null
				return "null";
			};

			var type = obj.GetType();
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
		}

		private static string ValueToString(object v) {
			string txt;
			if (v == null) {
				// print null
				txt = "null";
			}
			else if (v is string) {
				// print string
				txt = "\"" + v + "\"";
			}
			else if (v is IList) {
				// print list
				var vals = new StringBuilder();
				vals.Append("[");
				var list = (IList)v;
				for (int i = 0; i < list.Count; i++) {
					vals.Append(ValueToString(list[i]));
					if (i != (list.Count - 1)) {
						vals.Append(", ");
					}
				}
				vals.Append("]");
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
