#!/bin/bash
# To install tzdata noninteractive
export DEBIAN_FRONTEND=noninteractive
export tgz_name="glftpd-LNX-2.12_3.0.1_x64"

# Install necessary packages
apt-get update
install_clean wget ftp unzip zip xinetd tzdata unrar

# Start xinetd so installer will go through without complaining
service xinetd start

# Download and install glftpd
cd /root
wget https://glftpd.io/files/${tgz_name}.tgz
tar xzvf ${tgz_name}.tgz
rm ${tgz_name}.tgz
cd ${tgz_name}
{ echo; echo n; echo n; echo; echo; echo; echo x; echo n; echo /ftp-data; echo; echo; echo; } | ./installgl.sh
# ^ bug on line 1251, //ftp-data

# move glftpd.conf so it can easily be mounted on host
data_path='/glftpd/ftp-data'
mv /etc/glftpd.conf $data_path/
sed -i '/server_args/s/$/-r \/glftpd\/ftp-data\/glftpd.conf/' /etc/xinetd.d/glftpd
echo "0  0 * * *      /glftpd/bin/reset -r /glftpd/ftp-data/glftpd.conf" | crontab -

# change location of passwd and group so it can easily be mounted on host
mv /glftpd/etc/passwd $data_path/
mv /glftpd/etc/group $data_path/
sed -i '/^datapath/a pwd_path        /ftp-data/passwd' $data_path/glftpd.conf
sed -i '/^pwd_path/a grp_path        /ftp-data/group' $data_path/glftpd.conf

# change to static version since glibc 2.28 is required from version 2.10
cp /glftpd/bin/glftpd-full-static /glftpd/bin/glftpd

# disable IPV6
sed -i 's/IPv6$//' /etc/xinetd.d/glftpd

# comment out DHPARAM_FILE from glftpd
sed -i '/^DHPARAM_FILE/s/^/#/g' $data_path/glftpd.conf

# Install unrar for /glftpd/bin/zipscript
cp /usr/bin/unrar /glftpd/bin/
cp /usr/lib/x86_64-linux-gnu/libstdc++.so.6 /glftpd/lib/x86_64-linux-gnu/
cp /lib/x86_64-linux-gnu/libgcc_s.so.1 /glftpd/lib/x86_64-linux-gnu/
