#
# FluentFTP Integration Test Server: pureftpd
#

#
# Stage 1: build & production
#

FROM	common-debian-slim:fluentftp AS build

LABEL	Description="FluentFTP pureftpd docker image based on Debian Bullseye."

SHELL	["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	sources.list /etc/apt/sources.list

RUN	apt update && \
	\
	$APT_CMD \
		openssl \
		pure-ftpd

COPY	run-pureftpd.sh /usr/sbin/

RUN	sed -i -e "s/\r//" /usr/sbin/run-pureftpd.sh && \
	chmod +x /usr/sbin/run-pureftpd.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser && \
	\
	openssl req -x509 -newkey rsa:4096 \
		-keyout /etc/ssl/private/pure-ftpd.key -out /etc/ssl/certs/pure-ftpd.crt \
		-subj "/C=US/ST=State/L=/O=Dev/CN=fluentftp" \
		-nodes -days 3650 && \
	\
	chmod 0600 /etc/ssl/private/pure-ftpd.key && \
	chmod 0640 /etc/ssl/private/pure-ftpd.key && \
	\
	cat /etc/ssl/certs/pure-ftpd.crt /etc/ssl/private/pure-ftpd.key > /etc/ssl/private/pure-ftpd.pem

VOLUME	["/home/fluentuser", "/var/log/pureftpd"]

EXPOSE	20 21

CMD	["/usr/sbin/run-pureftpd.sh"]
