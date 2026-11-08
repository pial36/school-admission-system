# ---------- build stage: compile the React app to static files ----------
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# ---------- test stage: used by `docker compose --profile test run --rm frontend-tests` ----------
FROM build AS test
CMD ["npm", "test"]

# ---------- runtime stage: nginx serves the static files ----------
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
# The nginx image runs every script in /docker-entrypoint.d/ before starting.
COPY docker-entrypoint.sh /docker-entrypoint.d/40-write-config.sh
RUN chmod +x /docker-entrypoint.d/40-write-config.sh

# Backend URL as seen from the USER'S BROWSER (not from inside Docker).
ENV BACKEND_URL=http://localhost:8080
EXPOSE 80
