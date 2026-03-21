#!/bin/sh
# Entrypoint script for Railway deployment
# This allows dynamic configuration of the backend URL at runtime

# If BACKEND_URL is set, update the nginx config
if [ -n "$BACKEND_URL" ]; then
    echo "Configuring backend URL: $BACKEND_URL"
    sed -i "s|http://backend:5000|$BACKEND_URL|g" /etc/nginx/nginx.conf
fi

# Start nginx
exec nginx -g "daemon off;"
