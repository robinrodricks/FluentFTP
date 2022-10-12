#!/bin/bash

# stdout server info:
cat << EOB
	*************************************************
	*                                               *
	*    Docker image: fluentftp bftpd              *
	*                                               *
	*************************************************

	SERVER SETTINGS
	---------------
	· FTP User: fluentuser
	· FTP Password: fluentpass
EOB

# Run bftpd:
&>/dev/null /usr/sbin/bftpd -c /usr/etc/bftpd.conf -D
