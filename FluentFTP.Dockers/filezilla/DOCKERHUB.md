# Docker container for FileZilla
[![Docker Image Size](https://img.shields.io/microbadger/image-size/jlesage/filezilla)](http://microbadger.com/#/images/jlesage/filezilla) [![Build Status](https://drone.le-sage.com/api/badges/jlesage/docker-filezilla/status.svg)](https://drone.le-sage.com/jlesage/docker-filezilla) [![GitHub Release](https://img.shields.io/github/release/jlesage/docker-filezilla.svg)](https://github.com/jlesage/docker-filezilla/releases/latest) [![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://paypal.me/JocelynLeSage/0usd)

This is a Docker container for [FileZilla](https://filezilla-project.org/).

The GUI of the application is accessed through a modern web browser (no installation or configuration needed on the client side) or via any VNC client.

---

[![FileZilla logo](https://images.weserv.nl/?url=raw.githubusercontent.com/jlesage/docker-templates/master/jlesage/images/filezilla-icon.png&w=200)](https://filezilla-project.org/)[![FileZilla](https://dummyimage.com/400x110/ffffff/575757&text=FileZilla)](https://filezilla-project.org/)

FileZilla is a cross-platform graphical FTP, SFTP, and FTPS file management tool with a vast list of features.

---

## Quick Start

**NOTE**: The Docker command provided in this quick start is given as an example
and parameters should be adjusted to your need.

Launch the FileZilla docker container with the following command:
```
docker run -d \
    --name=filezilla \
    -p 5800:5800 \
    -v /docker/appdata/filezilla:/config:rw \
    -v $HOME:/storage:rw \
    jlesage/filezilla
```

Where:
  - `/docker/appdata/filezilla`: This is where the application stores its configuration, log and any files needing persistency.
  - `$HOME`: This location contains files from your host that need to be accessible by the application.

Browse to `http://your-host-ip:5800` to access the FileZilla GUI.
Files from the host appear under the `/storage` folder in the container.

## Documentation

Full documentation is available at https://github.com/jlesage/docker-filezilla.

## Support or Contact

Having troubles with the container or have questions?  Please
[create a new issue].

For other great Dockerized applications, see https://jlesage.github.io/docker-apps.

[create a new issue]: https://github.com/jlesage/docker-filezilla/issues
