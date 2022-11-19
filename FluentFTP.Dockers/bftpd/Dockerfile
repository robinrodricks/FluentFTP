#
# FluentFTP Integration Test Server: bftpd
#

#
# Stage 1: build
#

FROM	debian:bullseye-slim AS build

SHELL	["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

RUN	apt update && apt upgrade -y && apt install -y apt-utils && \
		\
		$APT_CMD \
			ca-certificates \
			wget \
			build-essential


WORKDIR	/tmp/bftpd
RUN	wget --no-check-certificate -O bftpd.tar.gz https://downloads.sourceforge.net/project/bftpd/bftpd/bftpd-6.1/bftpd-6.1.tar.gz && \
	tar -xzf bftpd.tar.gz && \
	cd bftpd && \
	./configure && \
	make -j$(nprocs) && \
	make install

#
# Stage 2: production
#

FROM	debian:bullseye-slim AS production

LABEL	Description="FluentFTP bftpd docker image based on Debian Bullseye."

SHELL	["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

WORKDIR	/
RUN	apt update && apt upgrade -y && apt install -y apt-utils

COPY	run-bftpd.sh /usr/sbin/

COPY	--from=build /usr/sbin/bftpd /usr/sbin

COPY	bftpd.conf /usr/etc/

WORKDIR	/
RUN	sed -i -e "s/\r//" /usr/etc/bftpd.conf && \
	sed -i -e "s/\r//" /usr/sbin/run-bftpd.sh && \
	chmod +x /usr/sbin/run-bftpd.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser

VOLUME	["/home/fluentuser", "/var/log/bftpd"]

EXPOSE	20 21

CMD	["/usr/sbin/run-bftpd.sh"]
