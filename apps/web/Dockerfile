FROM node:22-alpine AS build
WORKDIR /app
COPY apps/web/package*.json ./
RUN npm ci
COPY apps/web/ ./
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
RUN npm run build

# Academy Desk production web is a Next.js static export (output: "export"),
# deployed to Azure Static Web Apps. This image serves that same out/ tree
# for local runtime validation. It does not run a Next.js standalone server.
FROM nginx:1.27-alpine AS runtime
COPY apps/web/nginx.static.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/out /usr/share/nginx/html
EXPOSE 3000
