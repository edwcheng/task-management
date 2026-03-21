#!/bin/sh
set -e

# Default backend URL if not set (for Railway private networking)
: ${BACKEND_URL:="http://backend.railway.internal:5000"}

echo "Starting nginx on port ${PORT}"
echo "Backend URL: ${BACKEND_URL}"

# Substitute variables in nginx config
envsubst '${PORT} ${BACKEND_URL}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# Start nginx
exec nginx -g 'daemon off;'
