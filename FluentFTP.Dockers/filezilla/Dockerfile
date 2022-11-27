#
# FluentFTP Integration Test Server: filezilla-server
#

#
# Stage 1: build
#

FROM	common-debian:fluentftp AS build

SHELL   ["/bin/bash", "-c"]

ARG	LIBFILEZILLA_VERSION=0.39.2
ARG	FILEZILLA_VERSION=1.5.1

ARG	LIBFILEZILLA_URL=https://download.filezilla-project.org/libfilezilla/libfilezilla-${LIBFILEZILLA_VERSION}.tar.bz2
ARG	FILEZILLA_URL=https://download.filezilla-project.org/server/FileZilla_Server_${FILEZILLA_VERSION}_src.tar.bz2

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	sources.list /etc/apt/sources.list

RUN	apt update && apt upgrade -y && apt install -y apt-utils && \
	\
	$APT_CMD \
		curl \
		bzip2 \
		binutils \
		pkg-config \
		libgmp-dev \
		nettle-dev \
		gnutls-dev \
		gettext \
		libwxgtk3.0-gtk3-dev

WORKDIR	/tmp/libfilezilla
RUN	curl -L ${LIBFILEZILLA_URL} | tar xj --strip 1 -C /tmp/libfilezilla

WORKDIR	/tmp/filezilla
RUN	curl -L ${FILEZILLA_URL} | tar xj --strip 1 -C /tmp/filezilla

#
# configure, make, make install sequences for libfilezilla and filezilla-server
#
WORKDIR	/
RUN	export CFLAGS="-Os -fomit-frame-pointer" && \
	export CXXFLAGS="$CFLAGS" && \
	export CPPFLAGS="$CFLAGS" && \
	export LDFLAGS="-Wl,--as-needed" && \
#
# libfilezilla
#
	cd /tmp/libfilezilla && \
	\
	./configure --enable-shared=no --with-pic && \
	\
	make -j$(nproc) && \
	make install && \
#
# filezilla-server
#
	cd /tmp/filezilla/src/server && \
#
# need to changes the source code to not refuse to work if unprotected_hardlinks is 0
#
	sed -i "s/(has_unprotected_hardlinks)/(false)/" main.cpp && \
#
# need to create some bogus files because there is a bug in filezilla-server Makefile
#
	cd /tmp/filezilla/res/share/icons/hicolor && \
	\
	mkdir -p 128x128/apps  && cd $_ && echo '.' > filezilla-server-gui.png && cd ../.. && \
	mkdir -p 16x16/apps    && cd $_ && echo '.' > filezilla-server-gui.png && cd ../.. && \
	mkdir -p 20x20/apps    && cd $_ && echo '.' > filezilla-server-gui.png && cd ../.. && \
	mkdir -p 24x24/apps    && cd $_ && echo '.' > filezilla-server-gui.png && cd ../.. && \
	mkdir -p 256x256/apps  && cd $_ && echo '.' > filezilla-server-gui.png && cd ../.. && \
	mkdir -p 32x32/apps    && cd $_ && echo '.' > filezilla-server-gui.png && cd ../.. && \
	mkdir -p 48x48/apps    && cd $_ && echo '.' > filezilla-server-gui.png && cd ../.. && \
	mkdir -p scalable/apps && cd $_ && echo '.' > filezilla-server-gui.svg && cd ../.. && \
	\
	cd /tmp/filezilla && \
	\
	export PKG_CONFIG_PATH=/usr/local/lib/pkgconfig && \
	./configure --with-pugixml=builtin && \
	\
	make -j$(nproc) && \
	make install

WORKDIR	/tmp/filezilla

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

#
# Bring in settings.xml (PASV port ranges, certificate refs),
# and users.xml (contains fluentuser and his password)
#
COPY	--from=build /usr/local/bin /usr/local/bin

COPY	--from=build /tmp/filezilla/cert.pem /root/cert.pem
COPY	--from=build /tmp/filezilla/key.pem /root/key.pem

COPY	settings.xml /root/.config/filezilla-server/settings.xml
COPY	users.xml /root/.config/filezilla-server/users.xml

WORKDIR	/root/.config/filezilla-server

RUN	sed -i "s/<keyfile><\/keyfile>/<keyfile>\/root\/key.pem<\/keyfile>/" /root/.config/filezilla-server/settings.xml && \
	sed -i "s/<certsfile><\/certsfile>/<certsfile>\/root\/cert.pem<\/certsfile>/" /root/.config/filezilla-server/settings.xml && \
#
# All other settings files can be empty but must exist.
#
	touch allowed_ips.xml && \
	touch disallowed_ips.xml && \
	touch groups.xml

WORKDIR	/
RUN	sed -i -e "s/\r//" /usr/sbin/run-filezilla.sh && \
	chmod +x /usr/sbin/run-filezilla.sh && \
	\
	useradd -m -p savatlcb.1m26 fluentuser && \
	\
	mkdir -p /home/fluentuser/ && \
	chown -R fluentuser:users /home/fluentuser

VOLUME	["/home/fluentuser", "/var/log/filezilla"]

EXPOSE	20 21

CMD	["/usr/sbin/run-filezilla.sh"]
