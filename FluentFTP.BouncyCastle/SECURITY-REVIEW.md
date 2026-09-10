<!-- markdownlint-disable MD043 -->

# Bouncy Castle adapter security review

Review date: 2026-09-10. Recheck dependencies and upstream before release.

## Revision and scope

The local `feature/fluentftp-bouncycastle` branch was fast-forwarded from
`6096c7ae` to upstream `4a3780789e64b702e09fbc363046b27318e268d0`
(FluentFTP 54.2.1). At synchronization time, `HEAD...upstream/master` had zero
commits on either side.
No new merge commit or history rewrite was needed. The existing uncommitted
certificate-validation changes were preserved and included in this review.

Reviewed the complete Wixely contribution (`c54c2296` and `6096c7ae`), upstream
merge `fd1a74ca`, the subsequent release change, and the local security changes.
The merge tree exactly matches its Wixely parent: there are no merge-resolution
changes. The release changes only the core version and release notes.
The Codacy follow-up changes documentation/style and explicitly discards an
unused parameter; it does not fix certificate validation.

The findings below concern the optional Bouncy Castle adapter. This review does
not establish that the default SslStream implementation has these defects.
The security corrections and added tests are included with this report.

## Findings

- **High.** `targetHost` was unused during certificate validation. A trusted
  certificate for another hostname could therefore satisfy the adapter's
  default policy. Correction and evidence: `ServerCertificateValidation`
  checks DNS/IP identity independently of chain trust. Trusted-chain tests
  cover matching and mismatching hosts. A real-handshake regression fails
  against the unmodified merged adapter because it omits the hostname error,
  and passes with the correction. The negative control does not install a
  trusted attacker certificate or demonstrate a complete interception attack.

- **Medium.** The chain builder had no server-authentication application
  policy. Trust alone did not enforce the intended certificate usage.
  Correction and evidence: Validation sets the server-authentication OID. A
  custom-trust test rejects a client-authentication-only certificate.

- **Medium.** Revocation was always disabled, including when the caller
  enabled `ValidateCertificateRevocation`. Correction and evidence: Validation
  honors the setting. Tests serve a signed CRL over loopback, verify actual
  `Revoked` chain status, and check that missing revocation information fails
  closed when enabled.

- **Low.** An exception during cleanup could replace the original
  initialization failure. Correction and evidence: The existing local cleanup
  correction preserves the original exception. Fault injection verifies
  exception identity, closed capabilities, and idempotent disposal.

The hostname checks also cover SAN precedence over CN, exact CN fallback,
one-label DNS wildcards, IP SAN requirements, IDN normalization, malformed SAN
data, embedded NULs, and invalid trailing dots. Live SslStream peers verify DNS
SNI and omission for IP addresses. Expired and not-yet-valid certificates are
rejected under custom trust without altering OS trusted roots.

Both real FluentFTP clients reject the generated untrusted certificate before
sending any FTP login command. Explicit fingerprint pinning and
`ValidateAnyCertificate` permit login; an explicit rejection prevents it.
These overrides intentionally remain capable of overriding validation errors.
The custom-stream callback communicates error details as a string; the existing
FluentFTP `PolicyErrors` value does not classify the individual error types.

## Verification

All executions below ran on Windows. Tests generate disposable certificates
and loopback servers. Bouncy Castle adapter tests need no printer, Docker,
Python, Node.js, or additional trust roots. Some pre-existing general unit
tests use an external hostname for connection-timeout checks.

- **Release rebuild of adapter targets net6.0, net7.0, net8.0, net9.0,
  net10.0.** Passed; zero warnings/errors

- **Release build of the complete solution.** Passed; 46 warnings, zero
  errors; warnings are in unchanged core and test code

- **Complete unit suite against the local 54.2.1 core.** 195 passed, zero
  failed/skipped; net7.0 test assembly ran on .NET 8.0.25 using major
  roll-forward

- **Adapter tests with the minimum FluentFTP 48.0.3 package, native net8.0
  adapter/test targets.** 73 passed, zero failed/skipped on .NET 8.0.25

- **Same minimum-dependency tests, native net10.0 adapter/test targets.** 73
  passed, zero failed/skipped on .NET 10.0.8

- **Unmodified merged adapter, real TLS negative control.** Hostname-error
  regression fails as expected; remaining nine TLS tests pass

- **NuGet vulnerability query including transitive adapter dependencies.** No
  known vulnerable packages reported by the configured sources on the review
  date

TLS tests verify two sequential resumed data connections per positive scenario,
server-side resumption state, and byte-for-byte 32,769-byte exchanges across TLS
record boundaries. They also exercise a server declining resumption, optional
fallback, legacy resumption with and without opt-in, and servers limited to TLS
1.0, 1.1, or 1.3. Successful connections negotiate TLS 1.2; incompatible peers
fail without exposing a usable application stream. These are TLS stream tests,
not end-to-end FTP file-transfer tests.

The adapter uses BouncyCastle.Cryptography 2.7.0. The vendor's
[release notes](https://www.bouncycastle.org/download/bouncy-castle-c/) and
[advisory page](https://www.bouncycastle.org/vulnerability-advisory.html) were
consulted. A clean NuGet advisory query is a dated database result, not proof
that no undisclosed vulnerabilities exist.

## Reproduction

From the repository root, using Windows PowerShell:

```powershell
git fetch upstream --prune
git rev-list --left-right --count HEAD...upstream/master
git diff --exit-code fd1a74ca^2 fd1a74ca
dotnet build FluentFTP.BouncyCastle/FluentFTP.BouncyCastle.csproj `
  -c Release -t:Rebuild
dotnet build FluentFTP.sln -c Release
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test FluentFTP.Tests -c Release --filter FullyQualifiedName~Unit
dotnet list FluentFTP.BouncyCastle package --vulnerable --include-transitive
git diff --check
```

The local review artifacts are under `.git/security-review/`, including TRX
results, the solution build log, and two isolated test projects. These artifacts
contain local environment details and are deliberately outside tracked files.

The `compatibility/Compatibility.Tests.csproj` harness targets net8.0/net10.0,
references the actual adapter project and FluentFTP 48.0.3, links all
`FluentFTP.Tests/Unit/BouncyCastle*Tests.cs`, and uses assembly name
`FluentFTP.Tests` for the internal validation tests. It uses the same xUnit and
test SDK package versions as the repository.
Run it with `dotnet test -c Release`.

The `original/Original.Tests.csproj` harness targets net8.0, compiles the
unchanged stream/config sources extracted with `git show 4a378078:<path>`, and
links `BouncyCastleTlsIntegrationTests.cs`. It references FluentFTP 48.0.3,
BouncyCastle.Cryptography 2.7.0, and the repository's test package versions.
One failing test, `HandshakeReportsHostnameErrorEvenWhenChainIsAlsoUntrusted`,
is the expected result for this negative control.

## Codacy follow-up

Style review date: 2026-09-10. Repository `.editorconfig` requires C# tabs,
CRLF, same-line opening braces, camelCase locals/parameters, and `m_` fields.
There is no tracked Codacy configuration, SonarLint.xml, Markdownlint
configuration, or separate contributor style guide. The workflow under
`.github/workflows_WIP` is outside the active GitHub Actions directory.

GitHub's check records confirm that PR 1864 initially had 14 Codacy issues at
`c54c2296`, then passed with zero issues at `6096c7ae`. The accepted fix removed
unused parameters and addressed Markdown tabs, line length, and MD043 required
headings. Both adapter documents now follow that file-specific MD043 precedent;
their own headings remain intact. Prose and code blocks fit within 80 columns.

Codacy's public repository API confirms SonarC#, Markdownlint, and Lizard are
enabled, alongside other tools. Their repository-specific rule settings return
HTTP 401 without authentication. The public PR response confirms a quality gate
of zero new issues. Local `.editorconfig` checks alone cannot certify that gate.
See [Codacy's configuration documentation][codacy-config] for the distinction
between repository files and settings maintained by Codacy.

The local C# runner uses [Codacy's published SonarC# implementation][sonar-tool]
at commit `2ef30f519004f53540f6d34ebf67acd25f5aa492`, with SonarAnalyzer.CSharp
9.32.0.97167. It reuses the engine's file compilation, rule discovery, and
diagnostics code, plus its default patterns, parameters, and blacklist.
A small local entry point supplies the files without Docker. It reproduces
the original S1172 unused-parameter finding before the accepted fix.

The follow-up found and corrected:

- S108: an empty catch block in the TLS test server now explains the expected
  peer-close failure.
- Additional S3776/S1541 checks: split initialization, certificate parsing, and
  test-server methods. The files pass cognitive complexity 15 and cyclomatic
  complexity 10 thresholds. These extra thresholds are conservative checks,
  not confirmed repository settings.
- CA1806: hostname classification now explicitly uses the TryParse result.
- CA2016: test HTTP/FTP readers receive cancellation tokens directly.
- Markdown: replaced overlong tables with wrapped lists and removed extra
  blank lines. The established, narrowly scoped MD043 directive is retained.

Re-run evidence is stored in `.git/codacy-review/`. The runner reports zero
findings on the changed C# files with both the default pattern set and the
additional complexity/200-column checks. `dotnet format` checks include info
diagnostics; the adapter check excludes CA1513 because its suggested
`ObjectDisposedException.ThrowIf` API is unavailable on .NET 6/7. The existing
explicit throw is retained for those supported targets. No diagnostic is
disabled in repository configuration for this local-check exception.

PowerShell checks cover Markdown tabs, trailing whitespace, repeated blank
lines, 80-column lines, heading progression, and fenced-code formatting.
They are targeted checks, not execution of the Node-based Markdownlint engine.
The hosted engine versions and protected rule settings may differ. A hosted
Codacy result remains unverified.

[codacy-config]: https://docs.codacy.com/repositories-configure/codacy-configuration-file/
[sonar-tool]: https://github.com/codacy/codacy-sonar-csharp

## Remaining work and limits

- Owner: Wixely/maintainers. Recommended next action: review the fork's
  certificate corrections and regression tests for release. Upstream 54.2.1
  still contains the reviewed certificate-validation defects until the fixes
  are integrated there.
- Owner: maintainers. Re-run device and end-to-end FTP transfer tests before
  claiming current hardware compatibility. Earlier X1 Carbon reports were not
  reproduced in this review and are not used as validation evidence.
- Owner: maintainers. Linux, concurrent transfers, client certificates, and
  execution on actual .NET 6/7/9 runtimes remain unverified. Docker integration
  tests were not run; Docker is unavailable in this environment.
- Owner: maintainers. The synchronous custom-stream handshake has no
  cancellation-token parameter. Prompt cancellation and a total handshake
  deadline are not certified here. A slow peer can consume time within repeated
  blocking reads; the adapter should not be described as fully asynchronous.
- Owner: application developers. Reference current FluentFTP explicitly to
  receive core fixes. Keeping 48.0.3 as the adapter's API minimum preserves
  compatibility; the tests do not establish security of every older core API.

This is a bounded adversarial review with executable regression evidence,
not a guarantee that the library is free of all security defects.
