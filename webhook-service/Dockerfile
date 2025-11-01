FROM oven/bun:1.1.18-alpine AS builder

WORKDIR /app

COPY package.json bun.lock ./

RUN bun install --frozen-lockfile

COPY . .

RUN bun build src/main.ts --outdir ./dist --target node

FROM oven/bun:1.1.18-alpine

WORKDIR /app

COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/dist ./dist 

COPY package.json .

EXPOSE 8083

ENTRYPOINT ["bun", "run", "dist/main.js"]