

import '@fastify/jwt';
import { FastifyReply, FastifyRequest } from 'fastify';


declare module 'fastify' {
  interface FastifyRequest {
    user: any; 
  }
}

declare module '@fastify/jwt' {
  
  interface FastifyJWT {
    sign: (payload: any) => Promise<string>;
    verify: (token: string) => Promise<any>;
  }
  
 
  interface FastifyReply {
    jwtVerify: (options?: { decode?: unknown; verify?: unknown; }) => Promise<any>;
    jwtSign: (payload: any, options?: { sign?: unknown; }) => Promise<string>;
  }
}