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

Certificate validation remains controlled by FluentFTP. Configure its
certificate-validation callback or certificate-pinning policy as usual.

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
  `AsyncFtpClient`.
- Concurrent data transfers have not yet been verified.

The adapter has been tested with directory listings and file uploads against the
implicit FTPS server on a Bambu Lab X1 Carbon 3D printer.
