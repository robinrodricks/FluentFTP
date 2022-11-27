#!/bin/bash

# stdout server info:
cat << EOB
	********************************************************
	*                                                      *
	*    Docker helper image: fluentftp mirror select      *
	*                                                      *
	********************************************************
EOB

DEB_MIRROR=""

# Run apt-smart, it might fail miserably
python3 /root/.local/bin/apt-smart -b > /root/deb_mirror

# Get the mirror url
if [ -f /root/deb_mirror ]
then
	mapfile -t lines < /root/deb_mirror
	DEB_MIRROR=${lines[0]}
fi

# replace the sources.list, only if DEB_MIRROR is not empty
if [ -z "$DEB_MIRROR" ]
then
	# In case apt-smart fails in any way, we use the default
	cp /etc/apt/sources.list /root/sources.list
else
	printf "\
deb $DEB_MIRROR bullseye main\n\
deb http://deb.debian.org/debian-security bullseye-security main contrib\n\
deb $DEB_MIRROR bullseye-updates main\n\
"	> /root/sources.list
fi

echo " "
echo "New sources.list:"
echo " "
cat /root/sources.list
echo " "
