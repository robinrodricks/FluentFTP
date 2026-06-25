using System.Collections;
using System.Text;

namespace FluentFTP.Helpers {
	public static class ValuePrinter {

		public static string ObjectToString(this object obj) {
			if (obj == null) {
				// print null
				return "null";
			}

			return ValueToString(obj);
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
