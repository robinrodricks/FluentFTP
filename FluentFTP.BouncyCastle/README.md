<!-- markdownlint-disable MD043 -->

# FluentFTP.BouncyCastle

`FluentFTP.BouncyCastle` is a fully managed Bouncy Castle TLS stream for
FluentFTP. It allows an FTPS data connection to explicitly resume the TLS 1.2
session established by the control connection.

This is useful with servers that reject data connections unless the TLS session
is reused, commonly with an error such as:

```text
522 SSL connection failed: session reuse required
```

## Usage

```csharp
using FluentFTP;
using FluentFTP.BouncyCastle;

var client = new AsyncFtpClient(host, username, password, 990);
client.Config.EncryptionMode = FtpEncryptionMode.Implicit;
client.Config.CustomStream = typeof(BouncyCastleFtpStream);
client.Config.CustomStreamConfig = new BouncyCastleFtpConfig {
    RequireSessionResumption = true,
};
```

The package keeps FluentFTP 48.0.3 as its minimum API dependency. Reference the
current FluentFTP package explicitly in your application to receive later core
security and bug fixes; the minimum dependency is not a recommendation to stay
on that version.

## Certificate validation

The adapter checks certificate chain trust, server-authentication usage, and
the requested server's identity before calling FluentFTP's validation callback.
DNS Subject Alternative Names (SANs) take precedence over the Common Name (CN).
A DNS wildcard matches one whole label only. CN fallback uses an exact match
when no DNS SAN is present; an IP address always requires a matching IP SAN.
Internationalized hostnames are converted to ASCII for matching and SNI.
SNI is sent for DNS hosts and omitted for IP addresses.

The adapter offers the AES-256-GCM, AES-128-GCM, ChaCha20-Poly1305 and AES-CBC
suites with ECDHE, DHE and RSA key exchange, the same families as `SslStream`.
This matters for vsftpd, whose default configuration accepts nothing but
`ECDHE-RSA-AES256-GCM-SHA384`, a suite Bouncy Castle's own defaults omit.

`client.Config.ValidateCertificateRevocation` controls online revocation checks
(off by default, matching FluentFTP). When enabled, unavailable revocation
information is reported as a validation error, just like a revoked certificate.

Validation failures are passed through FluentFTP's custom-stream callback and
reject the connection by default. An explicit validation callback or
`ValidateAnyCertificate` can override them. For self-signed devices, prefer a
callback that checks a known certificate fingerprint; accepting any certificate
disables server identity protection. FluentFTP's existing custom-stream API
carries validation details in `PolicyErrorMessage`; its `PolicyErrors` enum
does not distinguish chain failures from hostname failures.

A rejected certificate sends a `bad_certificate` TLS alert to the server. The
certificate passed to the validation callback stays valid until the stream is
disposed, as with `SslStream`; copy it if you need it for longer.

## Failures and diagnostics

Handshake and certificate failures are raised as
`System.Security.Authentication.AuthenticationException`, matching FluentFTP's
default `SslStream` behaviour, so FluentFTP's own error handling and existing
`catch` blocks keep working. The Bouncy Castle `TlsException` is attached as
the inner exception and carries the TLS alert description.

Connection diagnostics (session offers, resumption results and certificate
acceptance) are written to FluentFTP's log at the Verbose level with a
`BouncyCastle:` prefix. `BouncyCastleFtpConfig.Diagnostic` optionally receives
the same messages.

## Legacy session resumption

Some legacy servers require session resumption without RFC 7627 Extended Master
Secret. Bouncy Castle blocks that by default. Enable compatibility only for a
known server that requires it:

```csharp
client.Config.CustomStreamConfig = new BouncyCastleFtpConfig {
    RequireSessionResumption = true,
    AllowLegacyResumption = true,
};
```

Allowing legacy resumption reduces TLS protections and should not be enabled as
a general fallback.

## Current limitations

- The adapter offers TLS 1.2 only.
- Client certificates are not supported. If `client.Config.ClientCertificates`
  is populated, a warning is logged and the handshake proceeds without one.
- The Bouncy Castle handshake is synchronous, including when used through
  `AsyncFtpClient`. The custom-stream interface does not pass a cancellation
  token to the handshake.
- Bouncy Castle's stream does not observe cancellation tokens on reads and
  writes. A cancelled `AsyncFtpClient` operation completes only when the
  socket read timeout (`client.Config.ReadTimeout`) expires.
- Concurrent data transfers have not yet been verified.

The original adapter was developed against the implicit FTPS server of a Bambu
Lab X1 Carbon printer. The hardened certificate validation has not been
re-tested on that device.

## Tests

```powershell
# Only needed if the .NET 7 runtime is absent.
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test FluentFTP.Tests --filter FullyQualifiedName~Unit.BouncyCastle
```

The tests generate their own certificates, use custom chain trust without
changing the operating system's trusted roots, and run loopback TLS, FTPS and
CRL servers. No printer or external server is required.

See the [security notes][security-notes] for the validation defects fixed in
the adapter and its remaining limits.

[security-notes]: https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP.BouncyCastle/SECURITY-REVIEW.md
