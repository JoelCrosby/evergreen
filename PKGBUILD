# Maintainer: Joel Crosby <joelcrosby@live.co.uk>
name="evergreen"

pkgname="evergreen-git"
pkgver=r120.a17dc9b
pkgrel=1
pkgdesc="Simple GTK+ git client"
arch=(x86_64)
url="https://github.com/joelcrosby/evergreen.git"
license=('MIT')
groups=()
depends=(gtk3 libgit2)
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
	cd $srcdir/${name}/
  
  install -d ${pkgdir}/opt/${name}
  install -d ${pkgdir}/usr/bin
  
  cp -rf ${srcdir}/${name}/Evergreen/bin/Release/net5.0/linux-x64/publish/* ${pkgdir}/opt/${name}
  ln -s "/opt/${name}/${name}" "${pkgdir}/usr/bin/${name}"
  
  install -Dm644 ${name}.desktop "${pkgdir}/usr/share/applications/${name}.desktop"
}
