#
# FluentFTP Integration Test Server: glftpd
#

#
# Stage 1: build & production
#

FROM	common-debian:fluentftp AS build

LABEL	Description="FluentFTP proftpd docker image based on Debian Bullseye."

SHELL	["/bin/bash", "-c"]

ARG	GLFTPD_VERSION=2.12
ARG	OPENSSL_VERSION=3.0.1

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	sources.list /etc/apt/sources.list

RUN	apt update && apt upgrade -y && apt install -y apt-utils && \
	\
	$APT_CMD \
		libpsl5 \
		publicsuffix \
		zip unzip \
		xinetd \
		ftp && \
	\
	cd /tmp && \
	wget https://glftpd.io/files/glftpd-LNX-${GLFTPD_VERSION}_${OPENSSL_VERSION}_x64.tgz && \
	tar -xzf  glftpd-LNX-${GLFTPD_VERSION}_${OPENSSL_VERSION}_x64.tgz && \
	\
	cd glftpd-LNX-${GLFTPD_VERSION}_${OPENSSL_VERSION}_x64 && \
	printf '\nN\nN\n\n\n21\nx\nY\n\n\n\n\n\n' | ./installgl.sh && \
	\
	apt remove -y --purge wget build-essential && \
	apt clean -y && \
	apt autoclean -y && \
	apt autoremove -y && \
	rm -rf /var/lib/apt/lists/*

COPY	run-glftpd.sh /usr/sbin/

COPY	xinetd.conf /etc/xinetd.conf
COPY	xinetd_glftpd.conf /etc/xinetd.d/glftpd
COPY	glftpd.conf /etc/glftpd.conf

RUN	sed -i -e "s/\r//" /etc/xinetd.conf && \
	sed -i -e "s/\r//" /etc/xinetd.d/glftpd && \
	sed -i -e "s/\r//" /etc/glftpd.conf && \
	sed -i -e "s/\r//" /usr/sbin/run-glftpd.sh && \
	chmod +x /usr/sbin/run-glftpd.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser
	
VOLUME	["/home/fluentuser", "/var/log/glftpd"]

EXPOSE	20 21

CMD	["/usr/sbin/run-glftpd.sh"]
