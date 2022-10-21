#!/bin/bash

# stdout server info:
cat << EOB
	*************************************************
	*                                               *
	*    Docker image: fluentftp filezilla          *
	*                                               *
	*************************************************

	SERVER SETTINGS
	---------------
	· FTP User: fluentuser
	· FTP Password: fluentpass
EOB

# Run filezilla:

&>/dev/null /usr/local/bin/filezilla-server
