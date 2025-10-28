// /account-service/src/modules/validate/validate.service.ts

import { FastifyReply } from "fastify";
import { ApiKeyModel } from "../ApiKey/apikey.model";
import * as crypto from "crypto";
import { FastifyJWT } from "@fastify/jwt";

type ValidateResult = Promise<{ userId: string }>;

export class ValidateService {
  // A única função dele. Recebe uma string, verifica no banco.
  public async findUserByApiKey(
    keyString: string
  ): Promise<{ userId: string } | null> {
    const receivedKeyHash = crypto
      .createHash("sha256")
      .update(keyString)
      .digest("hex");

    const apiKeyDoc = await ApiKeyModel.findOne({ hashedKey: receivedKeyHash });

    if (!apiKeyDoc) {
      return null;
    }

    return { userId: apiKeyDoc.userId.toString() };
  }
}
