[appurl]: https://glftpd.eu/
[hub]: https://hub.docker.com/r/jonarin/glftpd/

# jonarin/glftpd

[![glFTPd](https://glftpd.eu/media/logo.gif)][appurl]

[glFTPd](https://glftpd.eu/) is a free FTP server for UNIX based systems. It is highly configurable and its possibilities are endless. One of the main differences between many other ftp servers and glFTPd is that it has its own user database which can be completely maintained online using ftp site commands. glFTPd runs within a chroot environment which makes it relatively safe.

glFTPd has numerous features making many complex and complicated setups possible. A number of the most important features are:

* Virtual users and groups
* Bandwidth throttling (global and per user)
* Upload/Download ratio support
* On the fly CRC calculating of files being uploaded
* Script support on almost all commands and operations
* Online user management (add/remove/edit users using site commands)
* Built-in statistics viewable using site commands
* Encryption support through TLS/SSL integration
* ACL Support
* Many more ...

## Usage
```
docker create \
  --name=glFTPd \
  --net=host \
  -v <path to ftp-data>:/glftpd/ftp-data \
  -v <path to site-data:/glftpd/site \
  -e GL_PORT=<port> \
  -e TZ=<timezone> \
  -e GL_RESET_ARGS=<arguments> \
  jonarin/glftpd
```
## Parameters

* `-v /glftpd/ftp-data` - Config and user data
* `-v /glftpd/site` - FTP site data
* `-e GL_PORT` - FTP listen port [1337]
* `-e TZ` - for timezone information *eg Europe/Stockholm* [UTC]
* `-e GL_RESET_ARGS` - Argumets to glreset

Set GL_RESET_ARGS to "-e" to reset stats on Mondays instead of Sundays

## First time setup

Login with glftpd as username and password
```
ftp localhost <port>
site adduser <username> <password> <username>@<ip>
site change <username> flags +1
site change <username> ratio 0
site deluser glftpd`
```
#### Change certifcate CN
Default CN is set to glftpd, to generate a new certificate with a different CN:

```
docker exec -it glftpd /bin/bash
/root/glftpd-LNX-*_x64/create_server_key.sh <CN>
mv /root/glftpd-LNX-*_x64/ftpd-ecdsa.pem /glftpd/ftp-data
```

## Using an existing glftpd installation
/etc/passwd, /etc/groups, /etc/ftpd-ecdsa.pem and glftpd.conf is re-located to the /glftpd/ftp-data/ directory.

Make sure your glftpd.conf is updated accordingly:

```
CERT_FILE /glftpd/ftp-data/ftpd-ecdsa.pem
pwd_path  /ftp-data/passwd
grp_path  /ftp-data/group
```
## pz-ng

pz-ng is configured to monitor x264,x265,tv,dvdr,games,requests and xvid. To make changes

```
docker exec -it glftpd /bin/bash
cd /glftpd/ftp-data/pzs-ng-master
vi zipscript/conf/zsconfig.h
make
make install
```
