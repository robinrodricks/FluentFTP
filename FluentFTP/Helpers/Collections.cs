using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	internal static class Collections {

		/// <summary>
		/// Checks if the array is null or 0 length.
		/// </summary>
		public static bool IsBlank(this IList value) {
			return value == null || value.Count == 0;
		}

		/// <summary>
		/// Checks if the array is null or 0 length.
		/// </summary>
		public static bool IsBlank(this IEnumerable value) {
			if (value == null) {
				return true;
			}

			if (value is IList list) {
				return list.Count == 0;
			}

			if (value is byte[] bytes) {
				return bytes.Length == 0;
			}

			return false;
		}

		/// <summary>
		/// Converts the arguments to an array of strings.
		/// </summary>
		public static List<string> ItemsToString(this object[] args) {
			var results = new List<string>();
			if (args == null) {
				return results;
			}

			foreach (var v in args) {
				string txt;
				if (v == null) {
					txt = "null";
				}
				else if (v is string) {
					txt = "\"" + v + "\"";
				}
				else {
					txt = v.ToString();
				}

				results.Add(txt);
			}

			return results;
		}


		/// <summary>
		/// Ensures the given item is only added once. If it was not present true is returned, else false is returned.
		/// </summary>
		public static bool AddOnce<T>(this List<T> items, T item) {
			if (!items.Contains(item)) {
				items.Add(item);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Shallow clones the list by copying each item to a new list.
		/// </summary>
		public static List<T> ShallowClone<T>(this List<T> list) {
			if (list == null) {
				return null;
			}
			var newList = new List<T>();
			newList.AddRange(list);
			return newList;
		}


	}
}
