// /src/modules/validate/validate.controller.ts

import { FastifyRequest, FastifyReply } from "fastify";
import { ValidateService } from "./validate.service";
import { JwtReply } from "./validate.type";

const validateService = new ValidateService();

export async function validateHandler(req: FastifyRequest, reply: JwtReply) {
  const authHeader = req.headers.authorization;
  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    return reply.code(401).send({ message: "Header Authorization inválido." });
  }

  const credentialString = authHeader.replace("Bearer ", "");

  try {
    try {
      const payload: any = await req.jwtVerify();

      console.log(`[AUTH DEBUG] payload: ${payload.id}`)

      const userIdFromToken = payload.id;

      // A CORREÇÃO: LOG EXPLICITO
      console.log(
        `[AUTH DEBUG] JWT SUCESSO. User ID a ser enviado: ${userIdFromToken}`
      );

      reply.header("X-User-ID", userIdFromToken);
      return reply.code(200).send({ message: "Credencial JWT válida." });
    } catch (jwtError: any) {
      req.log.warn(
        {
          errName: jwtError.name,
          errMessage: jwtError.message,
        },
        "validateHandler: FALHA no JWT. Verificando como API Key..."
      );
    }

    req.log.info("validateHandler: Tentativa 2 -> Verificando como API Key...");
    const apiKeyResult = await validateService.findUserByApiKey(
      credentialString
    );

    if (apiKeyResult) {
      reply.header("X-User-ID", apiKeyResult.userId);
      return reply.code(200).send({ message: "Credencial ApiKey válida." });
    }

    return reply
      .code(401)
      .send({ message: "Não Autorizado: Credencial inválida." });
  } catch (e: any) {
    return reply.code(500).send({ message: "Erro interno do servidor" });
  }
}
