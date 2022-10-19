#!/bin/bash

# stdout server info:
cat << EOB
	*************************************************
	*                                               *
	*    Docker image: fluentftp glftpd             *
	*                                               *
	*************************************************

	SERVER SETTINGS
	---------------
	· FTP User: fluentuser
	· FTP Password: fluentpass
EOB

# start xinetd, because glftpd cannot be run standalone.

/usr/sbin/xinetd -pidfile /run/xinetd.pid -stayalive -inetd_compat -inetd_ipv6

# The -n AND the quote user, quote password are NEEDED to make this hack work -
# add the fluentuser to the initial glftpd user database. Can only do this be
# logging on the glftpd and using site commands.

ftp -n localhost <<EOF
quote user glftpd
quote pass glftpd
site adduser fluentuser fluentpass IP *@172.17.0.1
site change fluentuser flags +134ABCDEFGHI
quit
EOF

# stay in there and let things happen.

#&>/dev/null /opt/
tail -f /dev/null