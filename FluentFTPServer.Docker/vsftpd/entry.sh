#!/usr/bin/env bash
# vsftpd container entrypoint script

set -e

[[ "${DEBUG}" == "true" ]] && set -x

# Generate password if hash not set
if [[ ! -z "${FTP_PASSWORD}" ]] && [[ -z "${FTP_PASSWORD_HASH}" ]]; then
  FTP_PASSWORD_HASH="$(echo "${FTP_PASSWORD}" | mkpasswd -s -m sha-512)"
fi

if [[ ! -z "${FTP_USER}" ]] || [[ ! -z "${FTP_PASSWORD_HASH}" ]]; then
  /add-virtual-user.sh -d "${FTP_USER}" "${FTP_PASSWORD_HASH}"
fi

# Support multiple users
while read -r user; do
  IFS=: read -r name pass <<< "${!user}"
  echo "Adding user ${name}"
  /add-virtual-user.sh "${name}" "${pass}"
done < <(env | grep "FTP_USER_" | sed 's/^\(FTP_USER_[a-zA-Z0-9]*\)=.*/\1/')

# Support user directories
if [[ ! -z "${FTP_USERS_ROOT}" ]]; then
  # shellcheck disable=SC2016
  sed -i 's/local_root=.*/local_root=\/srv\/$USER/' /etc/vsftpd*.conf
fi

# Support setting the passive address
if [[ ! -z "$FTP_PASV_ADDRESS" ]]; then
  for f in /etc/vsftpd*.conf; do
    echo "pasv_address=${FTP_PASV_ADDRESS}" >> "$f"
  done
fi

# Manage /srv permissions
if [[ ! -z "${FTP_CHOWN_ROOT}" ]]; then
  chown ftp:ftp /srv
fi

vsftpd_stop() {
  echo "Received SIGINT or SIGTERM. Shutting down vsftpd"
  # Get PID
  pid="$(cat /var/run/vsftpd/vsftpd.pid)"
  # Set TERM
  kill -SIGTERM "${pid}"
  # Wait for exit
  wait "${pid}"
  # All done.
  echo "Done"
}

if [[ "${1}" == "vsftpd" ]]; then
  trap vsftpd_stop SIGINT SIGTERM
  echo "Running ${*}"
  "${@}" &
  pid="${!}"
  echo "${pid}" > /var/run/vsftpd/vsftpd.pid
  wait "${pid}" && exit ${?}
else
  exec "${@}"
fi
