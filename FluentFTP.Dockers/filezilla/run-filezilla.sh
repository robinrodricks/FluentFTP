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
&>/dev/null /opt/filezilla-server/bin/filezilla-server --config-dir /opt/filezilla-server/etc
