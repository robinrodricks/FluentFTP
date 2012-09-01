SOURCEFILES = \
    FtpActiveStream.cs \
    FtpChannel.cs \
    FtpClient.cs \
    FtpControlConnection.cs \
	FtpReply.cs \
    FtpDataStream.cs \
    FtpDirectory.cs \
    FtpEnums.cs \
    FtpException.cs \
    FtpFile.cs \
    FtpFileSystemObject.cs \
    FtpFileSystemObjectList.cs \
    FtpInvalidSslCertificate.cs \
    FtpListFormatParser.cs \
    FtpListItem.cs \
    FtpPassiveStream.cs \
    FtpSecurityNotAvailable.cs \
    FtpTraceListener.cs \
    FtpTransferInfo.cs \
    Proxy/ProxyBase.cs \
    Proxy/ProxySocket.cs \
    Proxy/ProxyType.cs \
    Proxy/Socks4AProxy.cs \
    Proxy/Socks4Proxy.cs \
    Proxy/Socks5Proxy.cs

CSC = dmcs
CSC_FLAGS =

all: System.Net.FtpClient.dll

System.Net.FtpClient.dll: 
	$(CSC) $(CSC_FLAGS) -target:library -out:$@ $(SOURCEFILES)

clean: 
	rm -f System.Net.FtpClient.dll

