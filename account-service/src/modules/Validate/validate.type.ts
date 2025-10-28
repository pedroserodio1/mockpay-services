import { FastifyJWT } from "@fastify/jwt";
import { FastifyReply } from "fastify";

export type JwtVerifiedReply = FastifyReply & { 
  jwtVerify: FastifyJWT['verify'] 
};