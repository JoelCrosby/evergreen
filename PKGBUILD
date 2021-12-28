# Maintainer: Joel Crosby <joelcrosby@live.co.uk>
name="evergreen"

pkgname="evergreen-git"
pkgver=r125.138fc35
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
    make clean build
}

package() {
	cd $srcdir/${name}/
    DESTDIR=${pkgdir} make install
}
