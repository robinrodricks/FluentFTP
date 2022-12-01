using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class Strings {


		/// <summary>
		/// Checks if every character in the string is whitespace, or the string is null.
		/// </summary>
		public static bool IsNullOrWhiteSpace(string value) {
			if (value == null) {
				return true;
			}

			for (var i = 0; i < value.Length; i++) {
				if (!char.IsWhiteSpace(value[i])) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if the string is null or 0 length.
		/// </summary>
		public static bool IsBlank(this string value) {
			return value == null || value.Length == 0;
		}

		/// <summary>
		/// Join the given strings by a delimiter.
		/// </summary>
		public static string Join(this string[] values, string delimiter) {
			return string.Join(delimiter, values);
		}

		/// <summary>
		/// Join the given strings by a delimiter.
		/// </summary>
		public static string Join(this List<string> values, string delimiter) {
			return string.Join(delimiter, values);
		}

		/// <summary>
		/// Adds a prefix to the given strings, returns a new array.
		/// </summary>
		public static string[] AddPrefix(this string[] values, string prefix, bool trim = false) {
			var results = new List<string>();
			foreach (var v in values) {
				var txt = prefix + (trim ? v.Trim() : v);
				results.Add(txt);
			}

			return results.ToArray();
		}

		/// <summary>
		/// Adds a prefix to the given strings, returns a new array.
		/// </summary>
		public static List<string> AddPrefix(this List<string> values, string prefix, bool trim = false) {
			var results = new List<string>();
			foreach (var v in values) {
				var txt = prefix + (trim ? v.Trim() : v);
				results.Add(txt);
			}

			return results;
		}

		/// <summary>
		/// Ensure a string has the given prefix
		/// </summary>
		public static string EnsurePrefix(this string text, string prefix) {
			if (!text.StartsWith(prefix)) {
				return prefix + text;
			}

			return text;
		}

		/// <summary>
		/// Ensure a string has the given postfix
		/// </summary>
		public static string EnsurePostfix(this string text, string postfix) {
			if (!text.EndsWith(postfix)) {
				return text + postfix;
			}

			return text;
		}

		/// <summary>
		/// Remove a prefix from a string, only if it has the given prefix
		/// </summary>
		public static string RemovePrefix(this string text, string prefix) {
			if (text.StartsWith(prefix)) {
				return text.Substring(prefix.Length).Trim();
			}
			return text;
		}

		/// <summary>
		/// Remove a postfix from a string, only if it has the given postfix
		/// </summary>
		public static string RemovePostfix(this string text, string postfix) {
			if (text.EndsWith(postfix)) {
				return text.Substring(0, text.Length - postfix.Length);
			}
			return text;
		}


		/// <summary>
		/// Escape a string into a valid C# string literal.
		/// Implementation from StackOverflow - https://stackoverflow.com/a/14087738
		/// </summary>
		public static string EscapeStringLiteral(this string input) {
			var literal = new StringBuilder(input.Length + 2);
			literal.Append("\"");
			foreach (var c in input) {
				switch (c) {
					case '\'':
						literal.Append(@"\'");
						break;

					case '\"':
						literal.Append("\\\"");
						break;

					case '\\':
						literal.Append(@"\\");
						break;

					case '\0':
						literal.Append(@"\0");
						break;

					case '\a':
						literal.Append(@"\a");
						break;

					case '\b':
						literal.Append(@"\b");
						break;

					case '\f':
						literal.Append(@"\f");
						break;

					case '\n':
						literal.Append(@"\n");
						break;

					case '\r':
						literal.Append(@"\r");
						break;

					case '\t':
						literal.Append(@"\t");
						break;

					case '\v':
						literal.Append(@"\v");
						break;

					default:

						// ASCII printable character
						if (c >= 0x20 && c <= 0x7e) {
							literal.Append(c);
						}
						else {
							// As UTF16 escaped character
							literal.Append(@"\u");
							literal.Append(((int)c).ToString("x4"));
						}

						break;
				}
			}

			literal.Append("\"");
			return literal.ToString();
		}


		/// <summary>
		/// Split into fields by splitting on tokens
		/// </summary>
		public static string[] SplitString(this string str) {
			var allTokens = new List<string>(str.Split(null));
			for (var i = allTokens.Count - 1; i >= 0; i--) {
				if (allTokens[i].Trim().Length == 0) {
					allTokens.RemoveAt(i);
				}
			}

			return allTokens.ToArray();
		}

		/// <summary>
		/// Checks if all the characters in this string are digits or dots
		/// </summary>
		public static bool IsNumeric(this string field) {
			field = field.Replace(".", ""); // strip dots
			for (var i = 0; i < field.Length; i++) {
				if (!char.IsDigit(field[i])) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if the string contains any of the given values
		/// </summary>
		public static bool ContainsAny(this string field, string[] values, int afterChar = -1) {
			foreach (var value in values) {
				if (field.IndexOf(value, StringComparison.Ordinal) > afterChar) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if RexEx Pattern is valid
		/// </summary>
		public static bool IsValidRegEx(this string pattern) {
			bool isValid = true;

			if ((pattern != null) && (pattern.Trim().Length > 0)) {
				try {
					Regex.Match("", pattern);
				}
				catch (ArgumentException) {
					// BAD PATTERN: Syntax error
					isValid = false;
				}
			}
			else {
				//BAD PATTERN: Pattern is null or blank
				isValid = false;
			}

			return (isValid);
		}

		/// <summary>
		/// Checks if the reply contains any of the known error strings, by checking in case-insensitive manner.
		/// </summary>
		public static bool ContainsAnyCI(this string reply, string[] strings) {

			// FIX: absorb cases where the reply is null (see issue #631)
			if (reply == null) {
				return false;
			}

			reply = reply.ToLower();
			foreach (var msg in strings) {
				if (reply.Contains(msg)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if the string equals any of these values, by checking in case-sensitive manner.
		/// </summary>
		public static bool EqualsAny(this string text, string[] strings) {

			if (text == null) {
				return false;
			}

			foreach (var str in strings) {
				if (text.Equals(str, StringComparison.Ordinal)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if the string contains the given substring in a case-insensitive manner.
		/// </summary>
		public static bool ContainsCI(this string value, string substring) {
			if (value == null || value.Length == 0 || value.Length < substring.Length) {
				return false;
			}
			return value.IndexOf(substring, StringComparison.OrdinalIgnoreCase) > -1;
		}

		/// <summary>
		/// Checks if the string starts with the given substring in a case-insensitive manner.
		/// </summary>
		public static bool StartsWithCI(this string value, string substring) {
			if (value == null || value.Length == 0 || value.Length < substring.Length) {
				return false;
			}
			return value.StartsWith(substring, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Checks if the string ends with the given substring in a case-insensitive manner.
		/// </summary>
		public static bool EndsWithCI(this string value, string substring) {
			if (value == null || value.Length == 0 || value.Length < substring.Length) {
				return false;
			}
			return value.EndsWith(substring, StringComparison.OrdinalIgnoreCase);
		}


	}
}