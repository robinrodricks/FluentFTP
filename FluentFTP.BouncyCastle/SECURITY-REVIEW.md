<!-- markdownlint-disable MD043 -->

# Bouncy Castle adapter security notes

These notes cover the optional `FluentFTP.BouncyCastle` stream only. They do
not describe, and do not apply to, FluentFTP's default `SslStream`
implementation.

## Fixed findings

The adapter as first merged upstream (FluentFTP 54.2.1) had the defects below.
All are fixed in this version and covered by the `BouncyCastle*Tests`
regression tests.

- **High.** The TLS target host was not used during certificate validation, so
  a trusted certificate for a different hostname satisfied the default policy.
  `ServerCertificateValidation` now checks DNS or IP identity independently of
  chain trust.
- **Medium.** The chain was built without a server-authentication application
  policy, so trust alone did not enforce the certificate's intended usage. The
  server-authentication extended key usage is now required.
- **Medium.** Revocation checking was always disabled, even when
  `ValidateCertificateRevocation` was enabled. The setting is now honoured, and
  missing revocation information fails closed when checking is enabled.
- **Low.** An exception thrown while closing the TLS protocol during a failed
  initialization could replace the original failure. The original exception is
  now preserved.
- **Low.** Handshake and certificate failures surfaced as Bouncy Castle
  `TlsFatalAlert` exceptions and sent an `internal_error` alert to the server.
  They are now raised as `AuthenticationException`, matching `SslStream`, and
  a rejected certificate sends `bad_certificate`.

## Validation policy

- Chain trust uses the operating system's trust store, with the certificates
  sent by the server as extra intermediates.
- DNS SANs take precedence over the CN; a wildcard matches one label; an IP
  address host requires a matching IP SAN; IDN hosts are matched in ASCII.
- Malformed SAN or subject data fails closed.
- Online revocation checks follow `client.Config.ValidateCertificateRevocation`.
- The validation result is passed to FluentFTP's callback, which can override
  it explicitly.

## Verifying

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test FluentFTP.Tests --filter FullyQualifiedName~Unit.BouncyCastle
```

The tests generate disposable certificates, use custom chain trust without
altering the operating system's roots, and run loopback TLS, FTPS and CRL
servers. They exercise TLS streams and FluentFTP login; they are not end-to-end
file transfer tests.

On 2026-09-13, revision `e29848dc` also passed all 85 adapter tests against the
minimum FluentFTP 48.0.3 NuGet package on both Windows/.NET 8.0.25 and .NET
10.0.8, with zero failures or skips. The existing local compatibility harness
references the adapter project and links the adapter tests, but references
FluentFTP as a package rather than the in-repository core project. The harness
must set `<AssemblyName>FluentFTP.Tests</AssemblyName>` to access the internal
adapter members exposed through `InternalsVisibleTo("FluentFTP.Tests")`.
Its net8.0 run uses the net8.0 adapter; its net10.0 run uses the net9.0 adapter.
Both runs resolve the same `lib/net6.0/FluentFTP.dll` from the 48.0.3 package.
They exercise different adapter targets and host runtimes, not different core
package assets.
The resolved dependency and copied FluentFTP DLL were checked against the
48.0.3 package, including SHA-256 equality. Results remain in the local review
artifacts; this harness is not part of hosted CI. This verifies loopback
compatibility at the dependency minimum, not physical printer transfers with
that core version.

## Remaining limits

Cipher review date: 2026-09-13. The adapter preserves the original Bouncy Castle
defaults, which stop at AES-128, and adds two forward-secret AES-256-GCM suites:
ECDHE-RSA-AES256-GCM-SHA384 and ECDHE-ECDSA-AES256-GCM-SHA384. Both are a
compatibility measure for servers restricted to AES-256, not a hardening change.
Loopback tests negotiate each suite against a server offering only that suite,
using an RSA or an EC server certificate respectively, and verify repeated data
connection resumption plus an exact 32769-byte round trip on every handshake.
Negative tests confirm the addition did not widen further: AES-256 CBC
(ECDHE-RSA and ECDHE-ECDSA), DHE-RSA-AES256-GCM and the AES-256 static-RSA
suites are all still refused. The original AES-128 static-RSA compatibility
remains; this is not a policy that guarantees forward secrecy for every accepted
connection.

On 2026-09-13, revision `b85b822e` passed authenticated X1 Carbon tests with
both FtpClient and AsyncFtpClient, using the local FluentFTP 54.2.1 core on
Windows/.NET 8.0.25. Each client listed files, uploaded 32,769 bytes, downloaded
them twice with exact byte equality, and verified deletion of its test file.
Each recorded five resumed data connections; both negotiated
ECDHE-RSA-AES256-GCM-SHA384. Tests used `ValidateAnyCertificate = true` and
`AllowLegacyResumption = true`, matching the existing connection settings.
They verify interoperability with those overrides, not default-policy trust
or server identity protection. Credentials and printer identifiers are not
included in this report.

- TLS 1.2 only; client certificates are not supported.
- The handshake is synchronous, and reads and writes do not observe
  cancellation tokens.
- Hardware evidence is limited to the X1 Carbon operations and settings above.
  Other printers and concurrent transfers remain unverified. The physical
  printer tests ran on Windows only.
- On revision `53bca793`, which includes the AES-256-GCM pair above,
  [hosted Windows and Linux runs][hosted-tests] each passed 85 adapter unit
  tests, and the Linux vsftpd and proftpd integration scenarios both passed.
  The preceding revision `b85b822e` was also reproduced independently under
  WSL2. Those runs used a GitHub Actions workflow and test-server
  certificates carrying a `localhost` SAN, neither of which is part of this
  change; reproducing them needs both restored.
- The vsftpd and proftpd container tests set `ValidateAnyCertificate`, as the
  other streams' container tests do, so they cover session resumption and
  transfers rather than trust or host name checking. Certificate and host
  name behaviour is covered by the unit tests instead.
- Owner: maintainers. Next action: review these results before release and
  repeat hardware checks after changes to the adapter or printer firmware.

[hosted-tests]: https://github.com/Wixely/FluentFTP/actions/runs/34737182942
