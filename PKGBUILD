# Maintainer: Your Name <joelcrosby@live.co.uk>
pkgname=Evergreen
pkgver=0.0.1
pkgrel=1
pkgdesc="A simple user freindly git client built with gtk & c#"
arch=('x86_64')
url="https://github.com/joelcrosby/evergreen"
source=()
license=('GPL')
depends=('git' 'libgit2' 'gtk3')
makedepends=('git')
options=(staticlibs !strip)
md5sums=()

build() {
  cd $pkgname

  ## Restore
  dotnet restore

  ## Build powershell core
  dotnet publish --configuration Linux --output bin --runtime "linux-x64" --self-contained
}

package() {
  mkdir -pv "$pkgdir/opt/evergreen/$_pkgnum"

  cd "$srcdir/Evergreen/bin/Linux/net5.0/linux-x64/"
  cp -ar ./ "$pkgdir/opt/evergreen/$_pkgnum/"

  mkdir -p "$pkgdir/usr/bin"
  ln -s "/opt/evergreen/$_pkgnum/$_binaryname" "$pkgdir/usr/bin/$_binaryname"
}
