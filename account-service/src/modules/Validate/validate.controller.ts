// /src/modules/validate/validate.controller.ts

import { FastifyRequest, FastifyReply } from 'fastify';
import { ValidateService } from './validate.service';
import { JwtVerifiedReply } from './validate.type';

const validateService = new ValidateService();

export async function validateHandler(req: FastifyRequest, reply: JwtVerifiedReply) {
  
  const authHeader = req.headers.authorization;
  
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return reply.code(401).send({ message: 'Header Authorization inválido.' });
  }

  
  const credentialString = authHeader.replace('Bearer ', '');

  try {
    
    const { userId } = await validateService.validateCredential(credentialString, reply);

   
    reply.header('X-User-ID', userId); 
    
    
    return reply.code(200).send({ message: 'Credencial válida.' });
    
  } catch (e: any) {
    
    return reply.code(401).send({ message: e.message || 'Não Autorizado.' });
  }
}