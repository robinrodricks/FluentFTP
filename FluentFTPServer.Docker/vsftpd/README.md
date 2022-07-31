# VSFTPD Docker Image

This is a micro-service image for VSFTPD.

There are a few limitations but it will work if you are using host networking
`--net host` or have a direct/routed network between the Docker container and
the client.

## Virtual Users

This VSFTPD container uses virtual users. Each user that logs in will have the same system UID and GID. The real users has UID and GID of 48:48 by default, however using build-args this can be changed when building the container.

For example use `--build-arg FTP_UID=1000 --build-arg FTP_GID=1000` to set the UID and GID to 1000:1000.

## Options

The following environment variables are accepted.

- `FTP_USER`: Sets the default FTP user

- `FTP_PASSWORD`: Plain text password (not recommended), or

- `FTP_PASSWORD_HASH`: Sets the password for the user specified by `FTP_USER`. This
requires a hashed password such as the ones created with `mkpasswd -m sha-512`
which is in the _whois_ debian package.

- `FTP_USER_*`: Adds multiple users. Value must be in the form of `username:hash`. Should not be used in conjunction with `FTP_USER` and `FTP_PASSWORD(_HASH)`.

- `FTP_USERS_ROOT`: if set the vsftpd `local_root` will be set to `/srv/$USER` so each user is chrooted to their own directory instead of a shared one.

- `FTP_CHOWN_ROOT`: if set `chown` will be run against `/srv` setting the FTP user and group as owner and group of the directory. _Note: chown is run non-recursively ie. will only chown the root`_

- `FTP_PASV_ADDRESS`: override the IP address that vsftpd will advertise in
  response to the PASV command

## Usage Example

```
docker run --rm -it -p 21:21 -p 4559-4564:4559-4564 -e FTP_USER=ftp -e FTP_PASSWORD=ftp docker.io/panubo/vsftpd:latest
```

## SSL Usage

SSL can be configured (non-SSL by default). Firstly the SSL certificate and key
need to be added to the image, either using volumes or baking it into an image.
Then specify the `vsftpd_ssl.conf` config file as the config vsftpd should use.

This example assumes the ssl cert and key are in the same file and are mounted
into the container read-only.

```
docker run --rm -it \
-e FTP_USER=panubo -e FTP_PASSWORD_HASH='$6$XWpu...DwK1' \
-v `pwd`/server.pem:/etc/ssl/certs/vsftpd.crt:ro \
-v `pwd`/server.pem:/etc/ssl/private/vsftpd.key:ro \
docker.io/panubo/vsftpd vsftpd /etc/vsftpd_ssl.conf
```

## Security

Currently `allow_writeable_chroot` is turned ON, however this isn't recommended as a security precaution. We might look at making this configurable in the future. The main consequence of turning this off is that the `local_root` can not be writable by the FTP user.

See [serverfault: vsftp: whu is allow_writable_chroot=YES a bad idea?](https://serverfault.com/q/743949/259651)

## Logs

To get the FTP logs mount `/var/log` outside of the container. For example add `-v /var/log/ftp:/var/log` to your `docker run ...` command.
