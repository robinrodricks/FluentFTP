FROM debian:bullseye-slim

MAINTAINER Philippe Le Van (@plv on twitter)

RUN apt-get update -qq && \
	apt-get install -y proftpd && \
	apt-get clean && \
    rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

RUN sed -i "s/# DefaultRoot/DefaultRoot /" /etc/proftpd/proftpd.conf

EXPOSE 20 21

ADD docker-entrypoint.sh /usr/local/sbin/docker-entrypoint.sh
# Remove \r from the windows style \r\n newline.
RUN sed -e 's/\r//' /usr/local/sbin/docker-entrypoint.sh > /usr/local/sbin/docker-entrypoint2.sh && \
	chmod 777 /usr/local/sbin/docker-entrypoint2.sh
ENTRYPOINT ["/usr/local/sbin/docker-entrypoint2.sh"]

CMD ["proftpd", "--nodaemon"]
