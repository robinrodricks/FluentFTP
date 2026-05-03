using System;
using System.Text;

namespace FluentFTP.Helpers {
	public static class Encodings {

		/// <summary>
		/// Generate valid C# code for the given encoding.
		/// Uses CodePage to reliably match known encodings.
		/// Falls back to `Encoding.GetEncoding` if the encoding is a uncommon one.
		/// </summary>
		public static string ToCode(Encoding enc) {
			if (enc == null) throw new ArgumentNullException(nameof(enc));

			int cp = enc.CodePage;

			switch (cp) {
				case 65001: return "System.Text.Encoding.UTF8";
				case 20127: return "System.Text.Encoding.ASCII";
				case 1200: return "System.Text.Encoding.Unicode";
				case 1201: return "System.Text.Encoding.BigEndianUnicode";
				case 12000: return "System.Text.Encoding.UTF32";
			}

			if (cp == Encoding.Default.CodePage)
				return "System.Text.Encoding.Default";

			// fallback (works for any encoding)
			string webName = enc.WebName.Replace("\"", "\\\"");
			return $"System.Text.Encoding.GetEncoding(\"{webName}\")";
		}

	}
}
