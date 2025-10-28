import { ApiKeyModel } from "./apikey.model";
import { ApiKeyInput } from "./apikey.schema";

export class ApiKeyService {
  async createApiKey(body: ApiKeyInput, userId: string) {
    try {
      const apiKey =
        (await Math.random().toString(36).substring(2, 15)) +
        Math.random().toString(36).substring(2, 15);

      const apiKeyWithPrefix = `${body.prefix}_${apiKey}`;

      const newApiKey = await ApiKeyModel.create({
        userId,
        name: body.name,
        prefix: body.prefix,
        hashedKey: apiKeyWithPrefix,
      });

      newApiKey.hashedKey = apiKeyWithPrefix;

      return newApiKey;
    } catch (err: any) {
      throw new Error("Erro ao criar api key");
    }
  }

  async listApiKeysByUser(userId: string) {
    try {
      const apiKeys = await ApiKeyModel.find({ userId }).select("-hashedKey");
      return apiKeys;
    } catch (err: any) {
      throw new Error("Erro ao listar api keys");
    }
  }
}
