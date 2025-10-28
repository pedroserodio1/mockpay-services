

import { FastifyInstance } from 'fastify';
import fp from 'fastify-plugin'; // O "Fastify Plugin" wrapper
import mongoose from 'mongoose';

async function connectToDatabase(fastify: FastifyInstance) {
  try {
    
    const url = process.env.MONGO_URL;

    if (!url) {
      fastify.log.fatal('MONGO_URL não está definida no .env');
      throw new Error('MONGO_URL não definida');
    }

    
    mongoose.connection.on('connected', () => {
      fastify.log.info('Mongoose conectado ao banco.');
    });

    mongoose.connection.on('error', (err) => {
      fastify.log.error(err, 'Mongoose falhou ao conectar.');
    });

    
    await mongoose.connect(url);

  } catch (err) {
    fastify.log.fatal(err, 'Falha ao conectar ao MongoDB');
    
    process.exit(1);
  }
}

// Exporta o plugin envolvido pelo 'fp'
export default fp(connectToDatabase);