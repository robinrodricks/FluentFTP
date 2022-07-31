docker-proftpd
==============

Simple way to install a proftp server on an host.

This FTP server work in passive mode (perhaps in active mode also but not sure...)


Quick start
-----------

```bash
docker run -d --net host \
	-e FTP_LIST="user1:pass1;user2:pass2" \
	-e MASQUERADE_ADDRESS=1.2.3.4 \
	-v /path_to_ftp_dir_for_user1:/home/user1 \
	-v /path_to_ftp_dir_for_user2:/home/user2 \
	kibatic/proftpd
```

The default passive ports are 50000-50100.

The masquerade address should be the external address of your FTP server

Warning
-------

The way to define the users and passwords makes that you should not
use ";" or ":" in your user name or password.

(ok, this is ugly, but using FTP in 2018 is ugly too)

USERADD_OPTIONS and PASSIVE_MIN_PORT, PASSIVE_MAX_PORT
------------------------------------------------------

```bash
docker run -d --net host \
	-e FTP_LIST="user1:pass1;user2:pass2" \
	-e USERADD_OPTIONS="-o --gid 33 --uid 33" \
	-e PASSIVE_MIN_PORT=50000
	-e PASSIVE_MAX_PORT=50100
	-e MASQUERADE_ADDRESS=1.2.3.4
	-v /path_to_ftp_dir_for_user1:/home/user1 \
	-v /path_to_ftp_dir_for_user2:/home/user2 \
	kibatic/proftpd
```

The USERADD_OPTIONS is not mandatory. It contains parameters we can
give to the useradd command (in order for example to indicates the
created user can have the uid of www-data (33) ).

It allows to give different accesses, but each user will create
the files and directory with the right user on the host.

docker-compose.yml example
--------------------------

You can for example use a docker-compose like this :

```yaml
version: '3.7'

services:
  proftpd:
    image: kibatic/proftpd
    network_mode: "host"
    restart: unless-stopped
    environment:
      FTP_LIST: "myusername:mypassword"
      USERADD_OPTIONS: "-o --gid 33 --uid 33"
      # optional : default to 50000 and 50100
      PASSIVE_MIN_PORT: 50000
      PASSIVE_MAX_PORT: 50100
      # optional : default to undefined
      MASQUERADE_ADDRESS: 1.2.3.4
    volumes:
      - "/the_direcotry_on_the_host:/home/myusername"
```

Firewall
--------

You can use these firewall rules with the FTP in active mode

```bash
iptables -A INPUT -p tcp --dport 21 -j ACCEPT
iptables -A OUTPUT -p tcp --dport 21 -j ACCEPT
iptables -A INPUT -p tcp --dport 20 -j ACCEPT
iptables -A OUTPUT -p tcp --dport 20 -j ACCEPT
iptables -A INPUT -p tcp --dport 50000:50100 -j ACCEPT
iptables -A OUTPUT -p tcp --dport 50000:50100 -j ACCEPT
```

Testing this Dockerfile
-----------------------

If you want to test this Dockerfile, you can use the tester directory :

```bash
cd tester
docker-compose build --pull
docker-compose up
```

Versions
--------

* 2022-05-10 : passive port config and masquerade config
* 2022-05-09 : update to debian:bullseye-slim and better doc
* 2019-10-09 : USERADD_OPTIONS added
* 2019-04-01 : update to debian stretch
* 2018-03-30 : creation

Author
------

inspired by the good idea and the image hauptmedia/proftpd
from Julian Haupt.
