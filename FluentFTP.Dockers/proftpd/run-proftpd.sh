#!/bin/bash

# stdout server info:
cat << EOB
	*************************************************
	*                                               *
	*    Docker image: fluentftp proftpd            *
	*                                               *
	*************************************************

	SERVER SETTINGS
	---------------
	· FTP User: fluentuser
	· FTP Password: fluentpass
	· SSL: $USE_SSL
EOB

if [[ -n "${USE_SSL}" ]]; then
  sed -i "s/^\(# \)\?TLSEngine.*$/TLSEngine on/" /etc/proftpd/tls.conf
fi

# Run proftpd:
&>/dev/null /usr/sbin/proftpd -n
