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

The adapter checks certificate chain trust, server-authentication usage, and
the requested server's identity before calling FluentFTP's validation callback.
DNS Subject Alternative Names (SANs) take precedence over the Common Name (CN).
A DNS wildcard matches one whole label only. CN fallback uses an exact match
when no DNS SAN is present; an IP address always requires a matching IP SAN.
Internationalized hostnames are converted to ASCII for matching and SNI.
SNI is sent for DNS hosts and omitted for IP addresses.

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

- The adapter currently offers TLS 1.2 only.
- Client certificates are not currently supported.
- The Bouncy Castle handshake is synchronous, including when used through
  `AsyncFtpClient`. The custom-stream interface does not pass a cancellation
  token to the handshake.
- Concurrent data transfers have not yet been verified.

Earlier development reported directory listings and file uploads against a
Bambu Lab X1 Carbon implicit FTPS server. The security review below did not
repeat hardware testing; those reports do not establish certificate rejection
or compatibility of the current changes with that device.

## Regression tests

Run the unit suite with:

```powershell
# Only needed if the .NET 7 runtime is absent.
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test FluentFTP.Tests -c Release --filter FullyQualifiedName~Unit
```

The certificate tests generate their own certificates and use custom chain
trust without changing the operating system's trusted roots. Loopback TLS
servers verify SNI and callback acceptance/rejection. A loopback HTTP server
serves a signed CRL to test revocation. No printer or external server is
required for these tests. Cleanup-failure injection verifies that initialization
keeps the original exception even if closing the TLS protocol also fails.
Additional loopback tests check repeated TLS session resumption, rejection when
required resumption fails, legacy opt-in, TLS version restrictions, and exact
32,769-byte bidirectional transfers. Both FluentFTP clients are tested for
certificate rejection before login and explicit acceptance through pinning or
`ValidateAnyCertificate`. The payload tests exercise TLS streams, not FTP file
upload/download commands.

See [the 2026-09-10 security review](SECURITY-REVIEW.md) for tested revisions,
results, reproduction commands, and remaining verification limits.
