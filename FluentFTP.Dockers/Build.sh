# uncomment for build cache cleanup before starting
# sudo docker builder prune --all -f

# uncomment for image cleanup, force total rebuild before starting
# sudo docker image prune -f

#
# Debian mirror selection using a helper container
#
sudo docker build common-mirror --pull --progress=plain -t common-mirror:fluentftp
sudo docker run --name mirror common-mirror:fluentftp
sudo docker cp mirror:/root/sources.list sources.list
sudo docker rm mirror

#
# distribute the sources.list to the dockers for speed
#
sudo cp sources.list common-debian
sudo cp sources.list common-debian-slim
sudo cp sources.list apache
sudo cp sources.list bftpd
sudo cp sources.list filezilla
sudo cp sources.list glftpd
sudo cp sources.list proftpd
sudo cp sources.list pureftpd
# for pyftpdlib, not needed
sudo cp sources.list vsftpd

#
# build the common-debian build environment
#
sudo docker build common-debian --pull --progress=plain -t common-debian:fluentftp

#
# build the common-debian production environment
#
sudo docker build common-debian-slim --pull --progress=plain -t common-debian-slim:fluentftp

#
# build the actual server images
#
sudo docker build apache    --progress=plain -t apache:fluentftp
sudo docker build bftpd     --progress=plain -t bftpd:fluentftp
sudo docker build filezilla --progress=plain -t filezilla:fluentftp
sudo docker build glftpd    --progress=plain -t glftpd:fluentftp
sudo docker build proftpd   --progress=plain -t proftpd:fluentftp
sudo docker build pureftpd  --progress=plain -t pureftpd:fluentftp
sudo docker build pyftpdlib --progress=plain -t pyftpdlib:fluentftp
sudo docker build vsftpd    --progress=plain -t vsftpd:fluentftp

#
# clean up
#
sudo rm sources.list
sudo rm common-debian\sources.list
sudo rm common-debian-slim\sources.list
sudo rm apache\sources.list
sudo rm bftpd\sources.list
sudo rm filezilla\sources.list
sudo rm glftpd\sources.list
sudo rm proftpd\sources.list
sudo rm pureftpd\sources.list
# for pyftpdlib, not needed
sudo rm vsftpd\sources.list

# uncomment this if you need the storage after the build
# sudo docker image rm common-mirror:fluentftp common-debian:fluentftp common-debian-slim:fluentftp
