#!/bin/sh
# Writes the backend URL into config.js when the container starts.
# Vite normally bakes env vars in at BUILD time; doing it at START time means the
# same image works with any backend URL — just change BACKEND_URL and restart.
set -eu
cat > /usr/share/nginx/html/config.js <<EOF
window.__APP_CONFIG__ = { apiUrl: "${BACKEND_URL}" };
EOF
echo "Frontend configured with BACKEND_URL=${BACKEND_URL}"
