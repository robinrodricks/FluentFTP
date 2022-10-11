FROM debian:bullseye-slim

MAINTAINER Philippe Le Van (@plv on twitter)

RUN apt-get update -qq && \
	apt-get install -y proftpd && \
	apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

<<<<<<< Updated upstream
RUN sed -i "s/# DefaultRoot/DefaultRoot /" /etc/proftpd/proftpd.conf
=======
RUN apt -y update && apt clean all

RUN apt install -y \
	apt-utils \
	dialog

RUN apt install -y \
	openssl \
	iproute2 \
	proftpd \
	proftpd-mod-crypto


RUN apt remove --purge -y \
	exim4-base \
	mariadb-common

RUN apt autoremove -y

COPY proftpd.conf /etc/proftpd/
RUN sed -i -e "s/\r//" /etc/proftpd/proftpd.conf
COPY modules.conf /etc/proftpd/
RUN sed -i -e "s/\r//" /etc/proftpd/modules.conf
COPY tls.conf /etc/proftpd/
RUN sed -i -e "s/\r//" /etc/proftpd/tls.conf
COPY run-proftpd.sh /usr/sbin/
RUN sed -i -e "s/\r//" /usr/sbin/run-proftpd.sh

RUN chmod +x /usr/sbin/run-proftpd.sh

RUN useradd -m -p savatlcb.1m26 fluentuser

RUN mkdir -p /home/fluentuser/
RUN chown -R fluentuser:users /home/fluentuser

RUN mkdir /var/ftp

RUN openssl req -x509 -newkey rsa:4096 \
           -keyout /etc/ssl/private/proftpd.key -out /etc/ssl/certs/proftpd.crt \
	   -subj "/C=US/ST=State/L=/O=Dev/CN=fluentftp" \
           -nodes -days 3650

RUN chmod 0600 /etc/ssl/private/proftpd.key 
RUN chmod 0640 /etc/ssl/private/proftpd.key

VOLUME /home/fluentuser
VOLUME /var/log/proftpd
>>>>>>> Stashed changes

EXPOSE 20 21

ADD docker-entrypoint.sh /usr/local/sbin/docker-entrypoint.sh
# Remove \r from the windows style \r\n newline.
RUN sed -e 's/\r//' /usr/local/sbin/docker-entrypoint.sh > /usr/local/sbin/docker-entrypoint2.sh && \
	chmod 777 /usr/local/sbin/docker-entrypoint2.sh
ENTRYPOINT ["/usr/local/sbin/docker-entrypoint2.sh"]

CMD ["proftpd", "--nodaemon"]
