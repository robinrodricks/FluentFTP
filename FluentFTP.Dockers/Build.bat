rem uncomment for build cache cleanup before starting
rem docker builder prune --all -f

rem uncomment for image cleanup, force total rebuild before starting
rem docker image prune -f

rem
rem Debian mirror selection using a helper container
rem
docker build common-mirror --pull --progress=plain -t common-mirror:fluentftp
docker run --name mirror common-mirror:fluentftp
docker cp mirror:/root/sources.list sources.list
docker rm mirror

rem
rem distribute the sources.list to the dockers for speed
rem
copy sources.list common-debian
copy sources.list common-debian-slim
copy sources.list apache
copy sources.list bftpd
copy sources.list filezilla
copy sources.list glftpd
copy sources.list proftpd
copy sources.list pureftpd
rem for pyftpdlib, not needed
copy sources.list vsftpd

rem
rem build the common-debian build environment
rem
docker build common-debian --pull --progress=plain -t common-debian:fluentftp

rem
rem build the common-debian production environment
rem
docker build common-debian-slim --pull --progress=plain -t common-debian-slim:fluentftp

rem
rem build the actual server images
rem
docker build apache    --progress=plain -t apache:fluentftp
docker build bftpd     --progress=plain -t bftpd:fluentftp
docker build filezilla --progress=plain -t filezilla:fluentftp
docker build glftpd    --progress=plain -t glftpd:fluentftp
docker build proftpd   --progress=plain -t proftpd:fluentftp
docker build pureftpd  --progress=plain -t pureftpd:fluentftp
docker build pyftpdlib --progress=plain -t pyftpdlib:fluentftp
docker build vsftpd    --progress=plain -t vsftpd:fluentftp

rem
rem clean up
rem
del sources.list
del common-debian\sources.list
del common-debian-slim\sources.list
del apache\sources.list
del bftpd\sources.list
del filezilla\sources.list
del glftpd\sources.list
del proftpd\sources.list
del pureftpd\sources.list
rem for pyftpdlib, not needed
del vsftpd\sources.list

rem uncomment this if you need the storage after the build
rem docker image rm common-mirror:fluentftp common-debian:fluentftp common-debian-slim:fluentftp

pause