FROM debian:bullseye AS build

MAINTAINER FluentFTP
LABEL Description="FluentFTP vsftpd docker image based on Debian Bullseye."

RUN echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections

RUN apt -y update && apt clean all

RUN apt install -y \
	apt-utils \
	dialog

RUN apt install -y \
	iproute2

RUN apt remove --purge -y \
	exim4-base \
	mariadb-common

RUN apt autoremove -y

RUN apt install -y  \
        wget \
	build-essential


RUN mkdir /tmp/bftpd/ && \
	cd /tmp/bftpd/ && \
        wget -O bftpd.tar.gz https://downloads.sourceforge.net/project/bftpd/bftpd/bftpd-6.1/bftpd-6.1.tar.gz && \
        tar -xzf bftpd.tar.gz && \
        cd bftpd && \
        ./configure && \
        make && \
        make install

RUN apt remove --purge -y \
        build-essential \
	wget

RUN apt autoremove -y

FROM debian:bullseye AS production

RUN echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections

RUN apt -y update && apt clean all

RUN apt install -y \
	apt-utils \
	dialog

RUN apt install -y \
	iproute2

RUN apt remove --purge -y \
	exim4-base \
	mariadb-common

COPY --from=build /usr/sbin/bftpd /usr/sbin

COPY bftpd.conf /usr/etc/
RUN sed -i -e "s/\r//" /usr/etc/bftpd.conf
COPY run-bftpd.sh /usr/sbin/
RUN sed -i -e "s/\r//" /usr/sbin/run-bftpd.sh

RUN chmod +x /usr/sbin/run-bftpd.sh

RUN useradd -m -p savatlcb.1m26 fluentuser

RUN mkdir -p /home/fluentuser/
RUN chown -R fluentuser:users /home/fluentuser

RUN mkdir /var/ftp

VOLUME /home/fluentuser
VOLUME /var/log/bftpd

EXPOSE 20 21

CMD ["/usr/sbin/run-bftpd.sh"]
