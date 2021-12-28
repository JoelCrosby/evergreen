BINARY_NAME=dotnet-install
DESTDIR ?= /usr/local/bin

build:
    dotnet publish Evergreen/Evergreen.csproj -c Release -r linux-x64 --self-contained

clean:
	rm -rf src
	rm -rf pkg
	rm -rf evergreen

install: clean build
    install -d ${pkgdir}/opt/${name}
    install -d ${pkgdir}/usr/bin

    cp -rf ${srcdir}/${name}/Evergreen/bin/Release/net6.0/linux-x64/publish/* ${pkgdir}/opt/${name}
    ln -s "/opt/${name}/${name}" "${pkgdir}/usr/bin/${name}"

    install -Dm644 ${name}.desktop "${pkgdir}/usr/share/applications/${name}.desktop"
