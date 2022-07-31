#
# filezilla Dockerfile
#
# https://github.com/jlesage/docker-filezilla
#

# Pull base image.
FROM jlesage/baseimage-gui:alpine-3.15-v3.5.8

# Docker image version is provided via build arg.
ARG DOCKER_IMAGE_VERSION=unknown

# Define software versions.
ARG LIBFILEZILLA_VERSION=0.37.2
ARG FILEZILLA_VERSION=3.60.1
ARG VIM_VERSION=8.0.0830
ARG GNOMETHEMES_VERSION=3.28

# Define software download URLs.
ARG LIBFILEZILLA_URL=https://download.filezilla-project.org/libfilezilla/libfilezilla-${LIBFILEZILLA_VERSION}.tar.bz2
ARG FILEZILLA_URL=https://download.filezilla-project.org/client/FileZilla_${FILEZILLA_VERSION}_src.tar.bz2
ARG VIM_URL=https://github.com/vim/vim/archive/v${VIM_VERSION}.tar.gz
ARG GNOMETHEMES_URL=https://download-fallback.gnome.org/sources/gnome-themes-extra/${GNOMETHEMES_VERSION}/gnome-themes-extra-${GNOMETHEMES_VERSION}.tar.xz

# Define working directory.
WORKDIR /tmp

# Compile FileZilla.
RUN \
    # Install build dependencies.
    add-pkg --virtual build-dependencies \
        curl \
        file \
        patch \
        build-base \
        libidn-dev \
        nettle-dev \
        gnutls-dev \
        sqlite-dev \
        xdg-utils \
        wxgtk-dev \
        && \
    # Set same default compilation flags as abuild.
    export CFLAGS="-Os -fomit-frame-pointer" && \
    export CXXFLAGS="$CFLAGS" && \
    export CPPFLAGS="$CFLAGS" && \
    export LDFLAGS="-Wl,--as-needed" && \
    # Download sources.
    echo "Downloading sources..." && \
    mkdir /tmp/libfilezilla && \
    curl -# -L ${LIBFILEZILLA_URL} | tar xj --strip 1 -C /tmp/libfilezilla && \
    mkdir /tmp/filezilla && \
    curl -# -L ${FILEZILLA_URL} | tar xj --strip 1 -C /tmp/filezilla && \
    # Compile libfilezilla.
    cd libfilezilla && \
    ./configure \
        --prefix=/tmp/libfilezilla_install \
        --enable-shared=no \
        --with-pic \
        && \
    make -j$(nproc) && \
    make install && \
    cd .. && \
    # Compile FileZilla.
    cd filezilla && \
    # Fix compilation,
    sed-patch '/^#define/a #include <list>' src/interface/Mainfrm.h && \
    env PKG_CONFIG_PATH=/tmp/libfilezilla_install/lib/pkgconfig ./configure \
        --prefix=/usr \
        --with-pugixml=builtin \
        --without-dbus \
        --disable-autoupdatecheck \
        --disable-manualupdatecheck \
        && \
    # Disable usage of memfd_create() system call, which is not available on
    # older kernels (<3.17).  See:
    #     https://github.com/jlesage/docker-filezilla/issues/27.
    sed-patch 's|#define HAVE_MEMFD_CREATE 1|/* #undef HAVE_MEMFD_CREATE */|' /tmp/filezilla/config/config.h && \
    make -j$(nproc) && \
    make install && \
    strip /usr/bin/filezilla && \
    rm /usr/share/applications/filezilla.desktop && \
    rm -r /usr/share/applications && \
    cd .. && \
    # Cleanup.
    del-pkg build-dependencies && \
    rm -rf /tmp/* /tmp/.[!.]*

# Compile VIM.
RUN \
    # Install build dependencies.
    add-pkg --virtual build-dependencies \
        curl \
        build-base \
        ncurses-dev \
        libxt-dev \
        gtk+2.0-dev && \
    # Set same default compilation flags as abuild.
    export CFLAGS="-Os -fomit-frame-pointer" && \
    export CXXFLAGS="$CFLAGS" && \
    export CPPFLAGS="$CFLAGS" && \
    export LDFLAGS="-Wl,--as-needed" && \
    # Download sources.
    mkdir /tmp/vim && \
    curl -# -L ${VIM_URL} | tar xz --strip 1 -C /tmp/vim && \
    # Compile.
    cd vim && \
    ./configure \
        --prefix=/usr \
        --enable-gui=gtk2 \
        --disable-nls \
        --enable-multibyte \
        --localedir=/tmp/vim-local \
        --mandir=/tmp/vim-man \
        --docdir=/tmp/vim-doc \
        && \
    echo '#define SYS_VIMRC_FILE "/etc/vim/vimrc"' >> src/feature.h && \
    echo '#define SYS_GVIMRC_FILE "/etc/vim/gvimrc"' >> src/feature.h && \
    cd src && \
    make -j$(nproc) && \
    make installvimbin && \
    make installrtbase && \
    cd .. && \
    cd .. && \
    # Cleanup.
    del-pkg build-dependencies && \
    rm -rf /tmp/* /tmp/.[!.]*

# Compile GTK theme.
RUN \
    # Install build dependencies.
    add-pkg --virtual build-dependencies \
        curl \
        build-base \
        intltool \
        gtk+2.0-dev \
        librsvg-dev \
        && \
    # Set same default compilation flags as abuild.
    export CFLAGS="-Os -fomit-frame-pointer" && \
    export CXXFLAGS="$CFLAGS" && \
    export CPPFLAGS="$CFLAGS" && \
    export LDFLAGS="-Wl,--as-needed" && \
    # Download sources.
    mkdir /tmp/gnome-themes-extra && \
    curl -# -L ${GNOMETHEMES_URL} | tar xJ --strip 1 -C /tmp/gnome-themes-extra && \
    # Compile.
    cd gnome-themes-extra && \
    ./configure \
        --prefix=/usr \
        --disable-gtk3-engine \
        && \
    make -j$(nproc) && \
    make DESTDIR=/tmp/gnome-themes-extra-install install && \
    find /tmp/gnome-themes-extra-install -name "*.so" -exec strip {} ';' && \
    find /tmp/gnome-themes-extra-install -name "*.la" -delete && \
    mkdir -p /usr/share/themes/Adwaita && \
    cp -av /tmp/gnome-themes-extra-install/usr/share/themes/Adwaita/gtk-2.0 /usr/share/themes/Adwaita/ && \
    cp -av /tmp/gnome-themes-extra-install/usr/lib/gtk-2.0 /usr/lib/ && \
    cd .. && \
    # Cleanup.
    del-pkg build-dependencies && \
    rm -rf /tmp/* /tmp/.[!.]*

# Install dependencies.
RUN \
    add-pkg \
        # The following package is used to send key presses to the X process.
        xdotool \
        # The following package is needed by VIM.
        ncurses \
        # The following packages are needed by FileZilla.
        gtk+2.0 \
        libidn \
        sdl \
        sqlite-libs \
        ttf-dejavu \
        wxgtk

# Adjust the openbox config.
RUN \
    # Maximize only the main/initial window.
    sed-patch 's/<application type="normal">/<application type="normal" title="FileZilla">/' \
        /etc/xdg/openbox/rc.xml && \
    # Make sure the main window is always in the background.
    sed-patch '/<application type="normal" title="FileZilla">/a \    <layer>below</layer>' \
        /etc/xdg/openbox/rc.xml

# Generate and install favicons.
RUN \
    APP_ICON_URL=https://github.com/jlesage/docker-templates/raw/master/jlesage/images/filezilla-icon.png && \
    install_app_icon.sh "$APP_ICON_URL"

# Add files.
COPY rootfs/ /

# Set environment variables.
ENV APP_NAME="FileZilla"

# Define mountable directories.
VOLUME ["/config"]
VOLUME ["/storage"]

# Metadata.
LABEL \
      org.label-schema.name="filezilla" \
      org.label-schema.description="Docker container for FileZilla" \
      org.label-schema.version="$DOCKER_IMAGE_VERSION" \
      org.label-schema.vcs-url="https://github.com/jlesage/docker-filezilla" \
      org.label-schema.schema-version="1.0"
