BUILD = xbuild
RELEASEDIR = Releases
RELEASE = System.Net.FtpClient.$(shell date +%y.%m.%d)
RELEASEPATH = $(RELEASEDIR)/$(RELEASE)

all: debug

release:
	$(BUILD) /p:Configuration=Release System.Net.FtpClient/System.Net.FtpClient.csproj

debug:
	$(BUILD) /p:Configuration=Debug System.Net.FtpClient/System.Net.FtpClient.csproj

test: debug
	$(BUILD) /p:Configuration=Debug Tests/Tests.csproj
	mono Tests/bin/Debug/tests.exe

clean:
	rm -rf Examples/bin
	rm -rf Examples/obj
	rm -rf System.Net.FtpClient/bin
	rm -rf System.Net.FtpClient/obj
	rm -rf Tests/bin
	rm -rf Tests/obj
	rm -rf $(RELEASEDIR)

codeplex: release debug
	rm -rf $(RELEASEPATH)
	mkdir -p $(RELEASEPATH)
	mkdir -p $(RELEASEPATH)/bin
	mkdir -p $(RELEASEPATH)/source
	mkdir -p $(RELEASEPATH)/examples
	cp -R System.Net.FtpClient/bin/* $(RELEASEPATH)/bin
	cp -R System.Net.FtpClient/*.cs $(RELEASEPATH)/source
	cp -R Examples/*.cs $(RELEASEPATH)/examples
	cp LICENSE.TXT $(RELEASEPATH)
	cd $(RELEASEDIR); zip -r $(RELEASE).zip $(RELEASE)/
	rm -rf $(RELEASEPATH)

packages: codeplex