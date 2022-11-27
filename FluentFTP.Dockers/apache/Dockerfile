#
# FluentFTP Integration Test Server: apache
#

#
# Stage 1: build
#

FROM	common-debian:fluentftp AS build

SHELL	["/bin/bash", "-c"]

ARG	APACHE_VERSION=1.2.0

ARG	APACHE_URL=https://dlcdn.apache.org/mina/ftpserver/${APACHE_VERSION}/apache-ftpserver-${APACHE_VERSION}-bin.tar.gz

WORKDIR	/root
RUN	wget -O apache.tar.gz ${APACHE_URL} && \
	tar xzf apache.tar.gz && \
	mv apache-ftpserver-${APACHE_VERSION} apache

#
# Stage 2: production
#

FROM	common-debian-slim:fluentftp AS production

LABEL	Description="FluentFTP apache-ftp-server docker image based on Debian Bullseye."

SHELL   ["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	sources.list /etc/apt/sources.list

RUN	apt update && apt upgrade -y && apt install -y apt-utils && \
	\
	$APT_CMD \
		default-jre

COPY	run-apache.sh /usr/bin/

COPY	--from=build /root/apache /root/apache

COPY	config.xml /root/apache/res/conf/config.xml
COPY	users.properties /root/apache/res/conf/users.properties

WORKDIR	/
RUN	sed -i -e "s/\r//" /root/apache/res/conf/config.xml && \
	sed -i -e "s/\r//" /root/apache/res/conf/users.properties && \
	sed -i -e "s/\r//" /usr/bin/run-apache.sh && \
	chmod +x /usr/bin/run-apache.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser

EXPOSE	20 21

CMD	["/usr/bin/run-apache.sh"]
