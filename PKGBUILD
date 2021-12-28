# Maintainer: Joel Crosby <joelcrosby@live.co.uk>
name="evergreen"

pkgname="evergreen-git"
pkgver=r119.bc9242c
pkgrel=1
pkgdesc=""
arch=(x86_64)
url="https://github.com/joelcrosby/evergreen.git"
license=('MIT')
groups=()
depends=()
makedepends=('git')
provides=("${name}")
conflicts=("${name}")
replaces=()
backup=()
options=()
install=
source=("evergreen::git+$url")
noextract=("global.json")
md5sums=('SKIP')

pkgver() {
	cd "$srcdir/${name}"
	printf "r%s.%s" "$(git rev-list --count HEAD)" "$(git rev-parse --short HEAD)"
}

build() {
	cd "$srcdir/${name}"
  dotnet publish Evergreen/Evergreen.csproj -c Release -r linux-x64 --self-contained
}

package() {
	cd "$srcdir/${name}/"
  mkdir -p ${pkgdir}/opt/${pkgdir}
  cp -rf "${srcdir}/${name}/Evergreen/bin/Release/net5.0/linux-x64/publish/*" ${pkgdir}/opt/${pkgdir}
  install -Dm644 evergreen.desktop "${pkgdir}/usr/share/applications"
  ln -s "${pkgdir}/opt/${pkgdir}/${name}" "${pkgdir}/usr/bin/${name}"
}
