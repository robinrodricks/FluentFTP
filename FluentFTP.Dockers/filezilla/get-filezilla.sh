#!/bin/bash

wget -O tempfile $1

STEP2=$(cat tempfile | grep linux-gnu.deb)
echo " "
echo $STEP2

rm tempfile

STEP3=$(echo $STEP2 | grep -P -o '(?<=a href=").*?(?=")')
echo " "
echo $STEP3

wget -O FileZilla-Server.deb $STEP3
