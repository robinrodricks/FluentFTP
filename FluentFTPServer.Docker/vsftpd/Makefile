NAME := vsftpd
TAG  := latest
IMAGE_NAME := panubo/$(NAME)

.PHONY: build build-local bash run run-ssl help push clean
help:
	@printf "$$(grep -hE '^\S+:.*##' $(MAKEFILE_LIST) | sed -e 's/:.*##\s*/:/' -e 's/^\(.\+\):\(.*\)/\\x1b[36m\1\\x1b[m:\2/' | column -c2 -t -s :)\n"

build: ## Build for publishing
	docker build --pull -t $(IMAGE_NAME):$(TAG) .

build-local: ## Builds with local users UID and GID
	docker build --build-arg FTP_UID=$(shell id -u) --build-arg FTP_GID=$(shell id -g) -t $(IMAGE_NAME):$(TAG) .

bash:
	docker run --rm -it $(IMAGE_NAME):$(TAG) bash

env:
	@echo "FTP_USER=ftp" >> env
	@echo "FTP_PASSWORD=ftp" >> env

vsftpd.pem:
	openssl req -new -newkey rsa:2048 -days 365 -nodes -sha256 -x509 -keyout vsftpd.pem -out vsftpd.pem -subj '/CN=self_signed'

run: env
	$(eval ID := $(shell docker run -d --env-file env -v $(shell pwd)/srv:/srv ${IMAGE_NAME}:${TAG}))
	$(eval IP := $(shell docker inspect --format '{{ .NetworkSettings.IPAddress }}' ${ID}))
	@echo "Running ${ID} @ ftp://${IP}"
	@docker attach ${ID}
	@docker kill ${ID}

run-ssl: env vsftpd.pem
	$(eval ID := $(shell docker run -d --env-file env -v $(shell pwd)/srv:/srv -v $(PWD)/vsftpd.pem:/etc/ssl/certs/vsftpd.crt -v $(PWD)/vsftpd.pem:/etc/ssl/private/vsftpd.key ${IMAGE_NAME}:${TAG} vsftpd /etc/vsftpd_ssl.conf))
	$(eval IP := $(shell docker inspect --format '{{ .NetworkSettings.IPAddress }}' ${ID}))
	@echo "Running ${ID} @ ftp://${IP}"
	@docker attach ${ID}
	@docker kill ${ID}

push: ## Pushes the docker image to hub.docker.com
	docker push $(IMAGE_NAME):$(TAG)

clean: ## Remove built images
	docker rmi $(IMAGE_NAME):$(TAG)
