# Serves the Scanning WinService MSI from the cluster.
# Build context must contain:  msi/*.msi  (produced by the build-msi job) and version.json
FROM nginxinc/nginx-unprivileged:1.27-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY msi/ /usr/share/nginx/html/winservice/
COPY version.json /usr/share/nginx/html/winservice/version.json

EXPOSE 8080
