import { FastifyJWT } from "@fastify/jwt";
import { FastifyReply } from "fastify";

export type JwtReply = FastifyReply & { 
  jwt: FastifyJWT; 
};