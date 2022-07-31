[preview]: https://raw.githubusercontent.com/MarkusMcNugen/docker-templates/master/crushftp10/crushftp.png "CrushFTP Logo"

![alt text][preview]

# CrushFTP 10

Share your files securely with FTP, Implicit FTPS, SFTP, HTTP, or HTTPS using CrushFTP

## Docker Features
* Base: Alpine 3.12 Linux (microblink/java)
* CrushFTP 10
* Size: 234.8MB

This container is distributed under the [MIT Licence](LICENSE).

# Volumes, Paths, Ports, and Envrionmental Variables
## Volumes
| Volume | Required | Function | Example |
|----------|----------|----------|----------|
| `/var/opt/CrushFTP10` | Yes | Persistent storage for CrushFTP config | `/your/config/path/:/var/opt/CrushFTP10`|
| `/mnt/FTP/Shared` | No | Shared host folder for file sharing with users | `/your/host/path/:/mnt/FTP/Shared`|

* You can add as many volumes as you want between host and the container and change their mount location within the container. You will configure individual folder access and permissions for each user in CrushFTPs User Manager. The "/mnt/FTP/Shared" in the table above is just one such example.

## Ports
| Port | Proto | Required | Function | Example |
|----------|----------|----------|----------|----------|
| `21` | TCP | Yes | FTP Port | `21:21`|
| `443` | TCP | Yes | HTTPS Port | `443:443`|
| `2000-2100` | TCP | Yes | Passive FTP Ports | `2000-2100:2000-2100`|
| `2222` | TCP | Yes | SFTP Port | `2222:2222`|
| `8080` | TCP | Yes | HTTP Port | `8080:8080`|
| `9090` | TCP | Yes | HTTP Alt Port | `9090:9090`|

* If you wish to run certain protocols on different ports you will need to change these to match the CrushFTP config. If you enable implicit or explicit FTPS those ports will also need to be opened.

## Environment Variables
| Variable               | Description               | Default      |
|:-----------------------|:--------------------------|:-------------|
| `CRUSH_ADMIN_USER`     | Admin user of CrushFTP    | `crushadmin` |
| `CRUSH_ADMIN_PASSWORD` | Password for admin user   | `crushadmin` |
| `CRUSH_ADMIN_PROTOCOL` | Protocol for health cecks | `http`       |
| `CRUSH_ADMIN_PORT`     | Port for health cecks     | `8080`       |

# Installation
Run this container and mount the containers `/var/opt/CrushFTP10` volume to the host to keep CrushFTP's configuration persistent. Open a browser and go to `http://<IP>:8080`. Note that the default username and password are both `crushadmin` unless the default environment variables are changed.

This command will create a new container and expose all ports. Remember to change the `<volume>` to a location on your host machine.

```
docker run -p 21:21 -p 443:443 -p 2000-2100:2000-2100 -p 2222:2222 -p 8080:8080 -p 9090:9090 -v <volume>:/var/opt/CrushFTP10 markusmcnugen/crushftp:latest
```

# CrushFTP Configuration
Visit the [CrushFTP 10 Wiki](https://www.crushftp.com/crush10wiki/)
