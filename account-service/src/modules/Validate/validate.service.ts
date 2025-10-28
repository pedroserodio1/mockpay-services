// /account-service/src/modules/validate/validate.service.ts

import { FastifyReply } from 'fastify';
import { ApiKeyModel } from '../ApiKey/apikey.model';
import * as crypto from 'crypto';
import { FastifyJWT } from '@fastify/jwt';
import { JwtVerifiedReply } from './validate.type';

type ValidateResult = Promise<{ userId: string }>;


export class ValidateService {


  private async findUserByApiKey(keyString: string): Promise<{ userId: string } | null> {
    
   
    const receivedKeyHash = crypto
      .createHash('sha256')
      .update(keyString)
      .digest('hex');

  
    const apiKeyDoc = await ApiKeyModel.findOne({ hashedKey: receivedKeyHash });

    if (!apiKeyDoc) {
      return null; 
    }
    
    
    return { userId: apiKeyDoc.userId.toString() }; 
  }


  

public async validateCredential(
  credentialString: string, 
  reply: JwtVerifiedReply 
): ValidateResult {
  
 
  try {
    
    const payload: any = await reply.jwtVerify(credentialString);
    
    
    return { userId: payload.userId };
    
  } catch (jwtError) {
    
  }
  
  
  try {
    const apiKeyResult = await this.findUserByApiKey(credentialString);
    
    if (apiKeyResult) {
      
      return { userId: apiKeyResult.userId };
    }
    
  } catch (apiKeyError) {
    
    reply.log.error(apiKeyError, 'Falha ao buscar API Key no banco.');
    throw new Error('Erro interno do servidor durante a validação.'); 
  }
  
  throw new Error('Não Autorizado: Credencial inválida.');
}
}