using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FluentFTP.BouncyCastle {
	internal static class ServerCertificateValidation {
		private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
		private const string SubjectAlternativeNameOid = "2.5.29.17";
		private const string CommonNameOid = "2.5.4.3";

		internal static string NormalizeHost(string host) {
			if (string.IsNullOrWhiteSpace(host)) {
				throw new ArgumentException("A TLS target host is required.", nameof(host));
			}
			if (IPAddress.TryParse(host, out var address)) {
				return address.ToString();
			}
			var dns = new IdnMapping().GetAscii(host.TrimEnd('.'));
			if (Uri.CheckHostName(dns) != UriHostNameType.Dns || host.EndsWith("..", StringComparison.Ordinal)) {
				throw new ArgumentException("The TLS target host must be a DNS name or IP address.", nameof(host));
			}
			return dns;
		}

		internal static string Validate(X509Certificate2 certificate, X509Chain chain, string host, bool checkRevocation) {
			chain.ChainPolicy.RevocationMode = checkRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
			chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
			chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);
			chain.ChainPolicy.ApplicationPolicy.Add(new Oid(ServerAuthenticationOid));
			var errors = new List<string>();
			if (!chain.Build(certificate)) {
				errors.Add("Server certificate chain validation failed: " +
					string.Join("; ", chain.ChainStatus.Select(status => status.Status.ToString())));
			}
			if (!MatchesHost(certificate, host)) {
				errors.Add("Server certificate does not match the TLS target host.");
			}
			return string.Join("; ", errors);
		}

		// One implementation across .NET 6-10. IP addresses require an IP SAN; DNS SANs
		// take precedence over CN. Wildcards match exactly one complete DNS label.
		internal static bool MatchesHost(X509Certificate2 certificate, string host) {
			var normalized = NormalizeHost(host);
			var isIpAddress = IPAddress.TryParse(normalized, out var address);
			try {
				var alternatives = certificate.Extensions.Cast<X509Extension>()
					.Where(extension => extension.Oid?.Value == SubjectAlternativeNameOid).ToArray();
				if (alternatives.Length > 1) {
					return false;
				}
				var hasDnsName = false;
				var matches = false;
				if (alternatives.Length == 1 &&
					!TryMatchAlternativeNames(alternatives[0], normalized, address, out hasDnsName, out matches)) {
					return false;
				}
				return isIpAddress || hasDnsName ? matches : MatchesCommonName(certificate, normalized);
			}
			catch (AsnContentException) {
				return false;
			}
		}

		private static bool TryMatchAlternativeNames(X509Extension extension, string host, IPAddress? address,
			out bool hasDnsName, out bool matches) {
			hasDnsName = false;
			matches = false;
			var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER);
			var names = reader.ReadSequence();
			reader.ThrowIfNotEmpty();
			while (names.HasData) {
				var tag = names.PeekTag();
				if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2))) {
					hasDnsName = true;
					var dns = names.ReadCharacterString(UniversalTagNumber.IA5String, tag);
					matches |= address == null && MatchesDns(host, dns);
				}
				else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 7))) {
					var bytes = names.ReadOctetString(tag);
					if (bytes.Length != 4 && bytes.Length != 16) {
						return false;
					}
					matches |= address != null && address.Equals(new IPAddress(bytes));
				}
				else {
					names.ReadEncodedValue();
				}
			}
			return true;
		}

		private static bool MatchesDns(string host, string name) {
			if (name.EndsWith("..", StringComparison.Ordinal)) {
				return false;
			}
			name = name.TrimEnd('.');
			if (name.StartsWith("*.", StringComparison.Ordinal)) {
				var dot = host.IndexOf('.');
				return dot > 0 && host[(dot + 1)..].Equals(name[2..], StringComparison.OrdinalIgnoreCase);
			}
			return host.Equals(name, StringComparison.OrdinalIgnoreCase);
		}

		private static bool MatchesCommonName(X509Certificate2 certificate, string host) {
			var reader = new AsnReader(certificate.SubjectName.RawData, AsnEncodingRules.DER);
			var subject = reader.ReadSequence();
			reader.ThrowIfNotEmpty();
			string? commonName = null;
			while (subject.HasData) {
				var attributes = subject.ReadSetOf(skipSortOrderValidation: true);
				var count = 0;
				var containsCommonName = false;
				while (attributes.HasData) {
					count++;
					var attribute = attributes.ReadSequence();
					if (attribute.ReadObjectIdentifier() == CommonNameOid) {
						if (!TryReadCommonNameValue(attribute, ref commonName)) {
							return false;
						}
						containsCommonName = true;
					}
					else {
						attribute.ReadEncodedValue();
					}
					attribute.ThrowIfNotEmpty();
				}
				if (containsCommonName && count != 1) {
					return false;
				}
			}
			return host.Equals(commonName, StringComparison.OrdinalIgnoreCase);
		}

		private static bool TryReadCommonNameValue(AsnReader attribute, ref string? commonName) {
			if (commonName != null) {
				return false;
			}
			var tag = attribute.PeekTag();
			if (tag.TagClass != TagClass.Universal) {
				return false;
			}
			commonName = attribute.ReadCharacterString((UniversalTagNumber)tag.TagValue);
			return true;
		}
	}
}
