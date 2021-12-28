PACKAGE_NAME=evergreen
DESTDIR ?= /usr/local/bin

build:
    dotnet publish Evergreen/Evergreen.csproj -c Release -r linux-x64 --self-contained

clean:
	rm -rf src
	rm -rf pkg
	rm -rf evergreen

install: clean build
    install -d /opt/$(PACKAGE_NAME)
    install -d /usr/bin

    cp -rf $(PACKAGE_NAME)/Evergreen/bin/Release/net6.0/linux-x64/publish/* /opt/$(PACKAGE_NAME)
    ln -s "/opt/$(PACKAGE_NAME)/$(PACKAGE_NAME)" "/usr/bin/$(PACKAGE_NAME)"

    install -Dm644 $(PACKAGE_NAME).desktop "/usr/share/applications/$(PACKAGE_NAME).desktop"
