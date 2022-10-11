FROM debian:bullseye

MAINTAINER FluentFTP
LABEL Description="FluentFTP vsftpd docker image based on Debian Bullseye."

RUN echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections

RUN apt -y update && apt clean all

RUN apt install -y \
	apt-utils \
	dialog

RUN apt install -y \
	openssl \
	iproute2 \
	vsftpd

RUN apt remove --purge -y \
	exim4-base \
	mariadb-common

RUN apt autoremove -y

COPY vsftpd.conf /etc/
RUN sed -i -e "s/\r//" /etc/vsftpd.conf
COPY run-vsftpd.sh /usr/sbin/
RUN sed -i -e "s/\r//" /usr/sbin/run-vsftpd.sh

RUN chmod +x /usr/sbin/run-vsftpd.sh

RUN useradd -m -p savatlcb.1m26 fluentuser

RUN mkdir -p /home/fluentuser/
RUN chown -R fluentuser:users /home/fluentuser

RUN mkdir /var/ftp

RUN openssl req -x509 -newkey rsa:4096 \
           -keyout /etc/ssl/private/vsftpd.key -out /etc/ssl/certs/vsftpd.crt \
	   -subj "/C=US/ST=State/L=/O=Dev/CN=fluentftp" \
           -nodes -days 3650

RUN chmod 0600 /etc/ssl/private/vsftpd.key 
RUN chmod 0640 /etc/ssl/private/vsftpd.key

VOLUME /home/fluentuser
VOLUME /var/log/vsftpd

EXPOSE 20 21

CMD ["/usr/sbin/run-vsftpd.sh"]
