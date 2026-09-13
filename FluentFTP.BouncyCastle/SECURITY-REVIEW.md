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

## Remaining limits

Cipher review date: 2026-09-13. The adapter preserves the original Bouncy Castle
defaults, which stop at AES-128, and adds two forward-secret AES-256-GCM suites:
ECDHE-RSA-AES256-GCM-SHA384 and ECDHE-ECDSA-AES256-GCM-SHA384. Both are a
compatibility measure for servers restricted to AES-256, not a hardening change.
Loopback tests negotiate each suite against a server offering only that suite,
using an RSA or an EC server certificate respectively, and verify repeated data
connection resumption plus a byte-exact 32769-byte round trip on every handshake.
Negative tests confirm the addition did not widen further: AES-256 CBC
(ECDHE-RSA and ECDHE-ECDSA), DHE-RSA-AES256-GCM and the AES-256 static-RSA
suites are all still refused. The original AES-128 static-RSA compatibility
remains; this is not a policy that guarantees forward secrecy for every accepted
connection.

A real X1 Carbon handshake with this policy selected
ECDHE-RSA-AES256-GCM-SHA384. That probe explicitly accepted certificate errors
and sent no login or file commands. Owner: maintainers. Next action: verify
authenticated listing, upload/download, and data-session resumption on the
printer before release.

- TLS 1.2 only; client certificates are not supported.
- The handshake is synchronous, and reads and writes do not observe
  cancellation tokens.
- Full hardware compatibility (Bambu Lab X1 Carbon) remains unverified beyond
  the handshake described above.
- On revision `53bca793`, which includes the AES-256-GCM pair above,
  [hosted Windows and Linux runs][hosted-tests] each passed 85 adapter unit
  tests, and the Linux vsftpd and proftpd integration scenarios both passed.
  The preceding revision `b85b822e` was also reproduced independently under
  WSL2. Concurrent transfers remain unverified.

[hosted-tests]: https://github.com/Wixely/FluentFTP/actions/runs/34737182942
