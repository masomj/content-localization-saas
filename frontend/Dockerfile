FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:24-alpine AS runtime
WORKDIR /app

COPY --from=build /app/.output .output

ENV NITRO_PORT=3000
ENV NITRO_HOST=0.0.0.0
EXPOSE 3000

CMD ["node", ".output/server/index.mjs"]
