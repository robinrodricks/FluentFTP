FROM debian:stretch

ARG FTP_UID=48
ARG FTP_GID=48
RUN set -x \
  && groupadd -g ${FTP_GID} ftp \
  && useradd --no-create-home --home-dir /srv -s /bin/false --uid ${FTP_UID} --gid ${FTP_GID} -c 'ftp daemon' ftp \
  ;

RUN set -x \
  && apt-get update \
  && apt-get install -y --no-install-recommends vsftpd db5.3-util whois \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/* \
  ;

RUN set -x \
  && mkdir -p /var/run/vsftpd/empty /etc/vsftpd/user_conf /var/ftp /srv \
  && touch /var/log/vsftpd.log \
  && rm -rf /srv/ftp \
  ;

COPY vsftpd*.conf /etc/
COPY vsftpd_virtual /etc/pam.d/
COPY *.sh /

VOLUME ["/etc/vsftpd", "/srv"]

EXPOSE 21 4559 4560 4561 4562 4563 4564

ENTRYPOINT ["/entry.sh"]
CMD ["vsftpd"]
