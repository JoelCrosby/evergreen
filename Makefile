PACKAGE_NAME=evergreen
DESTDIR=

build:
	dotnet publish Evergreen/Evergreen.csproj -c Release -r linux-x64 --self-contained

clean:
	rm -rf src/ pkg/ evergreen/

install: clean build
	install -d $(DESTDIR)/opt/$(PACKAGE_NAME)
	install -d $(DESTDIR)/usr/bin

	cp -rf $(PACKAGE_NAME)/Evergreen/bin/Release/net6.0/linux-x64/publish/* $(DESTDIR)/opt/$(PACKAGE_NAME)
	ln -s "$(DESTDIR)/opt/$(PACKAGE_NAME)/$(PACKAGE_NAME)" "/usr/bin/$(PACKAGE_NAME)"

	install -Dm644 $(PACKAGE_NAME).desktop "$(DESTDIR)/usr/share/applications/$(PACKAGE_NAME).desktop"
