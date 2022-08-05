#!/bin/bash

if [ -n "$FTP_LIST" ]; then
	IFS=';' read -r -a parsed_ftp_list <<< "$FTP_LIST" ; unset IFS
	for ftp_account in ${parsed_ftp_list[@]}
	do
		IFS=':' read -r -a tab <<< "$ftp_account" ; unset IFS
		ftp_login=${tab[0]}
		ftp_pass=${tab[1]}
		CRYPTED_PASSWORD=$(perl -e 'print crypt($ARGV[0], "password")' $ftp_pass)
		useradd --shell /bin/sh ${USERADD_OPTIONS} -d /home/$ftp_login --password $CRYPTED_PASSWORD $ftp_login
		echo "ftp_login: $ftp_login"
		# Create home directory. Removes requirement to mount volume.
		mkdir "/home/$ftp_login"
		chown -R $ftp_login:$ftp_login /home/$ftp_login
	done
fi

if [[ -z "${PASSIVE_MIN_PORT}" ]]; then
  PASV_MIN=50000
else
  PASV_MIN="${PASSIVE_MIN_PORT}"
fi
if [[ -z "${PASSIVE_MAX_PORT}" ]]; then
  PASV_MAX=50100
else
  PASV_MAX="${PASSIVE_MAX_PORT}"
fi
sed -i "s/^\(# \)\?PassivePorts.*$/PassivePorts ${PASV_MIN} ${PASV_MAX}/" /etc/proftpd/proftpd.conf

if [[ -z "${MASQUERADE_ADDRESS}" ]]; then
  sed -i "s/^\(# \)\?MasqueradeAddress.*$/# MasqueradeAddress x.x.x.x/" /etc/proftpd/proftpd.conf
else
  sed -i "s/^\(# \)\?MasqueradeAddress.*$/MasqueradeAddress ${MASQUERADE_ADDRESS}/" /etc/proftpd/proftpd.conf
fi

exec "$@"
