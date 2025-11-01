# Node 20 LTS
FROM node:20

# Diretório da aplicação
WORKDIR /app

# Copia package.json + package-lock.json
COPY package*.json ./

# Instala dependências
RUN npm install

# Copia o resto do código
COPY . .

# Compila TS
RUN npx tsc

# Comando para criar/existir as filas
CMD ["node", "dist/setup.js"]