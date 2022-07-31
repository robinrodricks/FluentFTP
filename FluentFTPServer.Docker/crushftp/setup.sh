#!/usr/bin/env bash

CRUSH_FTP_BASE_DIR="/var/opt/CrushFTP10"

if [[ -f /tmp/CrushFTP10.zip ]] ; then
    echo "Unzipping CrushFTP..."
    unzip -o -q /tmp/CrushFTP10.zip -d /var/opt/
    rm -f /tmp/CrushFTP10.zip
fi

if [ -z ${CRUSH_ADMIN_USER} ]; then
    CRUSH_ADMIN_USER=crushadmin
fi

if [ -z ${CRUSH_ADMIN_PASSWORD} ] && [ -f ${CRUSH_FTP_BASE_DIR}/admin_user_set ]; then
    CRUSH_ADMIN_PASSWORD="NOT DISPLAYED!"
elif [ -z ${CRUSH_ADMIN_PASSWORD} ] && [ ! -f ${CRUSH_FTP_BASE_DIR}/admin_user_set ]; then
    CRUSH_ADMIN_PASSWORD=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 16 | head -n 1)
fi

if [ -z ${CRUSH_ADMIN_PROTOCOL} ]; then
    CRUSH_ADMIN_PROTOCOL=http
fi

if [ -z ${CRUSH_ADMIN_PORT} ]; then
    CRUSH_ADMIN_PORT=8080
fi

if [[ ! -d ${CRUSH_FTP_BASE_DIR}/users/MainUsers/${CRUSH_ADMIN_USER} ]] || [[ -f ${CRUSH_FTP_BASE_DIR}/admin_user_set ]] ; then
    echo "Creating default admin..."
    cd ${CRUSH_FTP_BASE_DIR} && java -jar ${CRUSH_FTP_BASE_DIR}/CrushFTP.jar -a "${CRUSH_ADMIN_USER}" "${CRUSH_ADMIN_PASSWORD}"
    touch ${CRUSH_FTP_BASE_DIR}/admin_user_set
fi

chmod +x crushftp_init.sh
${CRUSH_FTP_BASE_DIR}/crushftp_init.sh start

until [ -f prefs.XML ]
do
     sleep 1
done

echo "########################################"
echo "# User:		${CRUSH_ADMIN_USER}"
echo "# Password:	${CRUSH_ADMIN_PASSWORD}"
echo "########################################"

while true; do sleep 86400; done
