#
# FluentFTP Integration Test: Debian Run Image
#

FROM	debian:bullseye-slim AS build

SHELL	["/bin/bash", "-c"]

ARG	DEBIAN_FRONTEND=noninteractive
ARG	APT_CMD='apt install -y --no-install-recommends'

COPY	sources.list /etc/apt/sources.list

RUN	apt update && apt upgrade -y
