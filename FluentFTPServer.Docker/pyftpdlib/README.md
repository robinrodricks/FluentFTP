# docker-pyftpdlib
Simple FTP server with pyftpdlib

https://hub.docker.com/r/akogut/docker-pyftpdlib/

## Running the server:
```
docker run -it --rm -p 21:21 akogut/docker-pyftpdlib
[I 2016-04-29 18:01:49] >>> starting FTP server on 0.0.0.0:21, pid=5
[I 2016-04-29 18:01:49] concurrency model: async
[I 2016-04-29 18:01:49] masquerade (NAT) address: None
[I 2016-04-29 18:01:49] passive ports: None
```

In another terminal run `ftp <docker-host>` and you should get something like this:
```
Connected to 192.168.99.100.
220 pyftpdlib 1.5.0 ready.
Name (192.168.99.100:akogut): user
331 Username ok, send password.
Password:
230 Login successful.
Remote system type is UNIX.
Using binary mode to transfer files.
ftp>
```

## Command line arguments:
```
docker run -it --rm -p 21:21 [-p 3000-3010] akogut/docker-pyftpdlib python ftpd.py -h
usage: ftpd.py [-h] [--user USER] [--password PASSWORD] [--host HOST]
               [--port PORT] [--passive PASSIVE] [--anon]

optional arguments:
  -h, --help           show this help message and exit
  --user USER          Username for FTP acess (user will be created) (default:
                       user)
  --password PASSWORD  Password for FTP user. (default: password)
  --host HOST
  --port PORT
  --passive PASSIVE    Range of passive ports (default: 3000-3010)
  --anon               Allow anonymous access (default: False)
```
