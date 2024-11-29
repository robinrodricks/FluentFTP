#
# FluentFTP Integration Test Server: filezilla-server
#

#
# Stage 1: build
#

FROM	common-debian:fluentftp AS build

SHELL   ["/bin/bash", "-c"]

ARG	FILEZILLA_URL=https://filezilla-project.org/download.php?show_all=1&type=server

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	sources.list /etc/apt/sources.list

RUN	apt update && apt upgrade -y && apt install -y apt-utils && \
	\
	$APT_CMD \
		libsm6 \
		libgtk2.0-0

WORKDIR	/tmp/filezilla

COPY	get-filezilla.sh /tmp/filezilla

RUN	sed -i -e "s/\r//" /tmp/filezilla/get-filezilla.sh && \
	chmod +x /tmp/filezilla/get-filezilla.sh

RUN	./get-filezilla.sh ${FILEZILLA_URL} && \
	dpkg -i FileZilla-Server.deb	

#
# The certificate and the certificate signature
#
# Make self signed key/cert pair:
# openssl ecparam -name prime256v1 -genkey -noout -out key.pem && \
# openssl req -new -x509 -key key.pem -out cert.pem -days 3650
#
RUN	openssl ecparam -name prime256v1 -genkey -noout -out key.pem && \
	openssl req -new -x509 -key key.pem -out cert.pem -days 3650 -subj "/C=US/ST=State/L=/O=Dev/CN=fluentftp"

#
# Stage 2: production
#

FROM	common-debian-slim:fluentftp AS production

LABEL	Description="FluentFTP filezilla-server docker image based on Debian Bullseye."

SHELL   ["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	run-filezilla.sh /usr/sbin/

COPY	--from=build /opt/filezilla-server /opt/filezilla-server

COPY	settings.xml /opt/filezilla-server/etc/settings.xml
COPY	users.xml /opt/filezilla-server/etc/users.xml

WORKDIR	/

RUN	sed -i -e "s/\r//" /opt/filezilla-server/etc/settings.xml && \
	sed -i -e "s/\r//" /opt/filezilla-server/etc/users.xml && \
	sed -i -e "s/\r//" /usr/sbin/run-filezilla.sh && \
	chmod +x /usr/sbin/run-filezilla.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser

VOLUME	["/home/fluentuser", "/var/log/filezilla"]

EXPOSE	20 21 14149

CMD	["/usr/sbin/run-filezilla.sh"]
