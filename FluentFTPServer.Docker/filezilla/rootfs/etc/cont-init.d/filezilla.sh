#!/usr/bin/with-contenv sh

set -e # Exit immediately if a command exits with a non-zero status.
set -u # Treat unset variables as an error.

run() {
    j=1
    while eval "\${pipestatus_$j+:} false"; do
        unset pipestatus_$j
        j=$(($j+1))
    done
    j=1 com= k=1 l=
    for a; do
        if [ "x$a" = 'x|' ]; then
            com="$com { $l "'3>&-
                        echo "pipestatus_'$j'=$?" >&3
                      } 4>&- |'
            j=$(($j+1)) l=
        else
            l="$l \"\$$k\""
        fi
        k=$(($k+1))
    done
    com="$com $l"' 3>&- >&4 4>&-
               echo "pipestatus_'$j'=$?"'
    exec 4>&1
    eval "$(exec 3>&1; eval "$com")"
    exec 4>&-
    j=1
    while eval "\${pipestatus_$j+:} false"; do
        eval "[ \$pipestatus_$j -eq 0 ]" || return 1
        j=$(($j+1))
    done
    return 0
}

log() {
    if [ -n "${1-}" ]; then
        echo "[cont-init.d] $(basename $0): $*"
    else
        while read OUTPUT; do
            echo "[cont-init.d] $(basename $0): $OUTPUT"
        done
    fi
}

# Generate machine id.
if [ ! -f /etc/machine-id ]; then
    log "generating machine-id..."
    cat /proc/sys/kernel/random/uuid | tr -d '-' > /etc/machine-id
fi

# Install requested packages.
if [ "${INSTALL_EXTRA_PKGS:-UNSET}" != "UNSET" ]; then
    log "installing requested package(s)..."
    for PKG in $INSTALL_EXTRA_PKGS; do
        if cat /etc/apk/world | grep -wq "$PKG"; then
            log "package '$PKG' already installed"
        else
            log "installing '$PKG'..."
            run add-pkg "$PKG" 2>&1 \| log
        fi
    done
fi

# Make sure required directories exist.
mkdir -p "$XDG_CONFIG_HOME"/filezilla # For FileZilla config.
mkdir -p "$XDG_CONFIG_HOME"/putty     # Needed to store host keys.
mkdir -p "$XDG_DATA_HOME"             # Looks like FileZilla is not creating this folder automatically.

# Copy default configuration files.
[ -f "$XDG_CONFIG_HOME"/filezilla/filezilla.xml ] || cp -v /defaults/filezilla.xml "$XDG_CONFIG_HOME"/filezilla/

# Make sure FileZilla is set to used our default editor.
sed -i 's|<Setting name="Default editor">.*|<Setting name="Default editor">2/usr/bin/filezilla_editor</Setting>|' "$XDG_CONFIG_HOME"/filezilla/filezilla.xml
sed -i 's|<Setting name="Always use default editor">.*|<Setting name="Always use default editor">1</Setting>|' "$XDG_CONFIG_HOME"/filezilla/filezilla.xml

# Take ownership of the config directory content.
find /config -mindepth 1 -exec chown $USER_ID:$GROUP_ID {} \;

# vim: set ft=sh :
