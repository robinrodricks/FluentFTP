#!/usr/bin/env bash
# Adds a virtual ftp user to /etc/vsftpd/virtual-users.db

set -e

[[ "${DEBUG}" == "true" ]] && set -x

DB="/etc/vsftpd/virtual-users.db"

if [[ "${#}" -lt 2 ]] || [[ "${#}" -gt 3 ]]; then
  echo "Usage: $0 [-d] <user> <password>" >&2
  echo >&2
  echo "[ -d ] Delete the database first" >&2
  exit 1
fi

if [[ "${1}" == "-d" ]]; then
  if [[ -f "${DB}" ]]; then
    rm "${DB}"
  fi
  shift
fi

printf '%s\n%s\n' "${1}" "${2}" | db5.3_load -T -t hash "${DB}"
