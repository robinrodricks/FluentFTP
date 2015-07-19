BUILD = xbuild
RELEASEDIR = Releases
RELEASE = System.Net.FtpClient.$(shell date +%y.%m.%d)
RELEASEPATH = $(RELEASEDIR)/$(RELEASE)
SNK = $(HOME)/Dropbox/Documents/System.Net.FtpClient-SNK/System.Net.FtpClient.snk

all: debug

release:
	$(BUILD) /p:Configuration=Release System.Net.FtpClient/System.Net.FtpClient.csproj

release-signed: 
	$(BUILD) /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="$(SNK)" System.Net.FtpClient/System.Net.FtpClient.csproj

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
	rm -rf $(RELEASEDIR)/*

codeplex: clean release debug
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
	@echo Release: $(RELEASEDIR)/$(RELEASE).zip

nuget: clean release-signed
	#nuget pack System.Net.FtpClient/System.Net.FtpClient.csproj -Prop Configuration=Release -OutputDirectory $(RELEASEDIR)
	nuget pack System.Net.FtpClient/System.Net.FtpClient.nuspec -Version $(shell monodis --assembly System.Net.FtpClient/bin/Release/System.Net.FtpClient.dll | awk '/Version/ {print $$2}') -OutputDirectory $(RELEASEDIR)


packages: codeplex nuget