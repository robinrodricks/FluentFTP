BUILD = xbuild
RELEASEDIR = Releases
RELEASE = FluentFTP.$(shell date +%y.%m.%d)
RELEASEPATH = $(RELEASEDIR)/$(RELEASE)
SNK = $(HOME)/Dropbox/Documents/FluentFTP-SNK/FluentFTP.snk

all: debug

release:
	$(BUILD) /p:Configuration=Release FluentFTP/FluentFTP.csproj

release-signed: 
	$(BUILD) /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="$(SNK)" FluentFTP/FluentFTP.csproj

debug:
	$(BUILD) /p:Configuration=Debug FluentFTP/FluentFTP.csproj

test: debug
	$(BUILD) /p:Configuration=Debug Tests/Tests.csproj
	mono Tests/bin/Debug/tests.exe

clean:
	rm -rf Examples/bin
	rm -rf Examples/obj
	rm -rf FluentFTP/bin
	rm -rf FluentFTP/obj
	rm -rf Tests/bin
	rm -rf Tests/obj
	rm -rf $(RELEASEDIR)/*

codeplex: clean release debug
	rm -rf $(RELEASEPATH)
	mkdir -p $(RELEASEPATH)
	mkdir -p $(RELEASEPATH)/bin
	mkdir -p $(RELEASEPATH)/source
	mkdir -p $(RELEASEPATH)/examples
	cp -R FluentFTP/bin/* $(RELEASEPATH)/bin
	cp -R FluentFTP/*.cs $(RELEASEPATH)/source
	cp -R Examples/*.cs $(RELEASEPATH)/examples
	cp LICENSE.TXT $(RELEASEPATH)
	cd $(RELEASEDIR); zip -r $(RELEASE).zip $(RELEASE)/
	rm -rf $(RELEASEPATH)
	@echo Release: $(RELEASEDIR)/$(RELEASE).zip

nuget: clean release-signed
	#nuget pack System.Net.FtpClient/System.Net.FtpClient.csproj -Prop Configuration=Release -OutputDirectory $(RELEASEDIR)
	nuget pack FluentFTP/FluentFTP.nuspec -Version $(shell monodis --assembly FluentFTP/bin/Release/FluentFTP.dll | awk '/Version/ {print $$2}') -OutputDirectory $(RELEASEDIR)


packages: codeplex nuget