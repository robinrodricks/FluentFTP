#
# FluentFTP Integration Test: Debian Mirror Selection
#

FROM	python:3-slim AS prebuild

SHELL	["/bin/bash", "-c"]

WORKDIR	/usr/src/app
RUN	pip install --user apt-smart

COPY	run.sh /usr/bin/

RUN	sed -i -e "s/\r//" /usr/bin/run.sh && \
	chmod +x /usr/bin/run.sh

CMD	["/usr/bin/run.sh"]