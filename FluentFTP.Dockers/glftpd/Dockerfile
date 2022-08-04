FROM phusion/baseimage:0.11
CMD ["/sbin/my_init"]
RUN rm -rf /etc/service
COPY root/ /
RUN /root/glinstall.sh && /root/pznginstall.sh
VOLUME /glftpd/site /glftpd/ftp-data
RUN apt-get clean -y && rm -rf /var/lib/apt/lists/* /var/cache/* /var/tmp/* /tmp/*
RUN cp -arp /glftpd/ftp-data /glftpd/ftp-data-dist && rm -rf /glftpd/ftp-data/*
