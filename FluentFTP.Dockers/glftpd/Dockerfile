FROM	debian:bullseye-slim

ARG	GLFTPD_VERSION=2.12
ARG     OPENSSL_VERSION=3.0.1

ENV	DEBIAN_FRONTEND=noninteractive
ENV	APT_CMD='apt install -y --no-install-recommends'

RUN	apt update && apt upgrade -y && apt install -y apt-utils && \
	\
	$APT_CMD \
		iproute2 \
		ca-certificates \
		libpsl5 \
		openssl \
		publicsuffix \
		wget \
		zip unzip \
		build-essential \
		xinetd ftp procps 

RUN	cd /tmp && \
	wget https://glftpd.io/files/glftpd-LNX-${GLFTPD_VERSION}_${OPENSSL_VERSION}_x64.tgz && \
	tar -xzf  glftpd-LNX-${GLFTPD_VERSION}_${OPENSSL_VERSION}_x64.tgz && \
        \
	cd glftpd-LNX-${GLFTPD_VERSION}_${OPENSSL_VERSION}_x64 && \
	printf '\nN\nN\n\n\n21\nx\nY\n\n\n\n\n\n' | ./installgl.sh 

COPY	xinetd.conf /etc/xinetd.conf
RUN	sed -i -e "s/\r//" /etc/xinetd.conf

COPY	xinetd_glftpd.conf /etc/xinetd.d/glftpd
RUN	sed -i -e "s/\r//" /etc/xinetd.d/glftpd

COPY	glftpd.conf /etc/glftpd.conf
RUN	sed -i -e "s/\r//" /etc/glftpd.conf

COPY	run-glftpd.sh /usr/sbin/
RUN	sed -i -e "s/\r//" /usr/sbin/run-glftpd.sh && \
	chmod +x /usr/sbin/run-glftpd.sh

RUN	useradd -m -p savatlcb.1m26 fluentuser && \
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser

VOLUME	/home/fluentuser
VOLUME	/var/log/glftpd

EXPOSE	20 21

CMD	["/usr/sbin/run-glftpd.sh"]
