#!/usr/bin/with-contenv sh
export GTK2_RC_FILES="${GTK2_RC_FILES:=/usr/share/themes/Adwaita/gtk-2.0/gtkrc}"
exec /usr/bin/filezilla_wrapper --local=/storage
