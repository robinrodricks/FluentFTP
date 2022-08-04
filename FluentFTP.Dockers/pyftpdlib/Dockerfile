FROM python:3.10-slim

MAINTAINER Andriy Kogut "kogut.andriy@gmail.com"

COPY . /app
WORKDIR /app
RUN pip install -r requirements.txt

RUN mkdir /ftp_root
RUN mkdir /ftp_root/nobody
RUN mkdir /ftp_root/user

EXPOSE 20 21

CMD python ftpd.py
