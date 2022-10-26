#!/bin/bash

# stdout server info:
cat << EOB
	*************************************************
	*                                               *
	*    Docker image: fluentftp vsftpd             *
	*                                               *
	*************************************************

	SERVER SETTINGS
	---------------
	· FTP User: fluentuser
	· FTP Password: fluentpass
	· SSL: $USE_SSL
EOB

if [[ -n "${USE_SSL}" ]]; then
  sed -i "s/^\(# \)\?ssl_enable=.*$/ssl_enable=YES/" /etc/vsftpd.conf
fi

# Run vsftpd:
&>/dev/null /usr/sbin/vsftpd /etc/vsftpd.conf
