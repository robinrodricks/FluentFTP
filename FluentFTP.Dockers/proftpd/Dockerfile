#
# FluentFTP Integration Test Server: proftpd
#

FROM	python:3-slim AS prebuild

SHELL	["/bin/bash", "-c"]

WORKDIR	/usr/src/app
RUN	pip install --user apt-smart

WORKDIR	/root
RUN	python3 /root/.local/bin/apt-smart -b > deb_mirror

#
# Stage 1: build & production
#

FROM	debian:bullseye-slim AS build

LABEL	Description="FluentFTP proftpd docker image based on Debian Bullseye."

SHELL	["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	--from=prebuild /root/deb_mirror /root/deb_mirror

WORKDIR	/
RUN	mapfile -t lines < /root/deb_mirror && \
	DEB_MIRROR=${lines[0]} && \
	echo $DEB_MIRROR && \
	\
	printf "\
	deb $DEB_MIRROR bullseye main\n\
#	deb-src $DEB_MIRROR bullseye main\n\
	\n\
	deb http://deb.debian.org/debian-security bullseye-security main contrib\n\
#	deb-src http://deb.debian.org/debian-security bullseye-security main contrib\n\
	\n\
	# bullseye-updates, previously known as 'volatile'\n\
	deb $DEB_MIRROR bullseye-updates main\n\
#	deb-src $DEB_MIRROR bullseye-updates main\n\
	" > /etc/apt/sources.list

RUN	apt update && apt upgrade -y && apt install -y apt-utils && \
		\
		$APT_CMD \
			openssl \
			proftpd \
			proftpd-mod-crypto

COPY	run-proftpd.sh /usr/sbin/

COPY	proftpd.conf /etc/proftpd/
COPY	modules.conf /etc/proftpd/
COPY	tls.conf /etc/proftpd/

RUN	sed -i -e "s/\r//" /etc/proftpd/proftpd.conf && \
	sed -i -e "s/\r//" /etc/proftpd/modules.conf && \
	sed -i -e "s/\r//" /etc/proftpd/tls.conf && \
	sed -i -e "s/\r//" /usr/sbin/run-proftpd.sh && \
	chmod +x /usr/sbin/run-proftpd.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser && \
	\
	openssl req -x509 -newkey rsa:4096 \
		-keyout /etc/ssl/private/proftpd.key -out /etc/ssl/certs/proftpd.crt \
		-subj "/C=US/ST=State/L=/O=Dev/CN=fluentftp" \
		-nodes -days 3650 && \
	\
	chmod 0600 /etc/ssl/private/proftpd.key && \
	chmod 0640 /etc/ssl/private/proftpd.key

VOLUME	["/home/fluentuser", "/var/log/proftpd"]

EXPOSE	20 21

CMD	["/usr/sbin/run-proftpd.sh"]
