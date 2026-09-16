#
# FluentFTP Integration Test Server: vsftpd
#

#
# Stage 1: build & production
#

FROM	common-debian-slim:fluentftp AS build

LABEL	Description="FluentFTP vsftpd docker image based on Debian Bullseye."

SHELL	["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	sources.list /etc/apt/sources.list

RUN	apt update && \
	\
	$APT_CMD \
		openssl \
		vsftpd

COPY	vsftpd.conf /etc/

COPY	run-vsftpd.sh /usr/sbin/

RUN	sed -i -e "s/\r//" /usr/sbin/run-vsftpd.sh && \
	sed -i -e "s/\r//" /etc/vsftpd.conf && \
	mkdir /var/ftp

WORKDIR	/
RUN	sed -i -e "s/\r//" /etc/vsftpd.conf && \
	sed -i -e "s/\r//" /usr/sbin/run-vsftpd.sh && \
	chmod +x /usr/sbin/run-vsftpd.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser && \
	\
	openssl req -x509 -newkey rsa:4096 \
		-keyout /etc/ssl/private/vsftpd.key -out /etc/ssl/certs/vsftpd.crt \
		-subj "/C=US/ST=State/L=/O=Dev/CN=fluentftp" \
		-nodes -days 3650 && \
	\
	chmod 0600 /etc/ssl/private/vsftpd.key && \
	chmod 0640 /etc/ssl/private/vsftpd.key

VOLUME	["/home/fluentuser", "/var/log/vsftpd"]

EXPOSE	20 21

CMD	["/usr/sbin/run-vsftpd.sh"]
