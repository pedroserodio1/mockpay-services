import { ApiKeyService } from "./apikey.service";

const apiKeyService = new ApiKeyService();

export async function createApiKeyHandler(request: any, reply: any) {
  try {
    const userId = request.user.id;
    const apiKey = await apiKeyService.createApiKey(request.body, userId);
    return reply.code(201).send(apiKey);
  } catch (e: any) {
    return reply.code(400).send({ message: e.message });
  }
}

export async function listApiKeysHandler(request: any, reply: any) {
  try {
    const userId = request.user.id;
    const apiKeys = await apiKeyService.listApiKeysByUser(userId);
    return reply.code(200).send(apiKeys);
  } catch (e: any) {
    return reply.code(400).send({ message: e.message });
  }
}

export async function deleteApiKeyHandler(request: any, reply: any) {
    try {
        const userId = request.user.id;
        const apiKeyId = request.params.id;
        await apiKeyService.deleteApiKey(apiKeyId, userId);
        return reply.code(204).send();
    } catch (e: any) {
        return reply.code(400).send({ message: e.message });
    }
}
