// /account-service/src/plugins/auth.ts

import { FastifyInstance } from 'fastify';
import fp from 'fastify-plugin';
import jwt from '@fastify/jwt';

async function jwtAuthPlugin(fastify: FastifyInstance) {
  
  const secret = process.env.JWT_SECRET;
  if (!secret) {
    fastify.log.fatal('JWT_SECRET não está definido no .env');
    throw new Error('JWT_SECRET não definido');
  }

  
  fastify.register(jwt, {
    secret: secret,
    sign: {
      expiresIn: '1h', 
    },
  });

  
  fastify.decorate('authenticate', async (request: any, reply: any) => {
    try {
      await request.jwtVerify();
    } catch (err) {
      reply.send(err);
    }
  });
}

export default fp(jwtAuthPlugin);