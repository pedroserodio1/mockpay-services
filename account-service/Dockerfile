
FROM node:25 AS builder

WORKDIR /app

COPY package.json package-lock.json ./

RUN npm install

COPY . .

RUN npm run build

FROM node:25-alpine

WORKDIR /app

COPY package.json package-lock.json ./

RUN npm install --production

COPY --from=builder /app/dist ./dist

EXPOSE 8081

CMD [ "npm", "run", "start" ]