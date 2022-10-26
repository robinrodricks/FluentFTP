#!/bin/bash

# stdout server info:
cat << EOB
	*************************************************
	*                                               *
	*    Docker image: fluentftp pureftpd           *
	*                                               *
	*************************************************

	SERVER SETTINGS
	---------------
	· FTP User: fluentuser
	· FTP Password: fluentpass
	· SSL: $USE_SSL
EOB

if [[ -n "${USE_SSL}" ]]; then
  TLS=1
else
  TLS=0
fi

# Run pureftpd:
&>/dev/null /usr/sbin/pure-ftpd -A -E -j -R -l unix -p 21100:21199 --tls $TLS
