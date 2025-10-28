import {z} from "zod";

// --- Schema para o Body ---
export const apiKeySchema = z.object({
    body: z.object({
        name: z.string().min(3, "O nome da API Key deve ter no mínimo 3 caracteres"),
        prefix: z.string().min(3, "O prefixo da API Key deve ter no mínimo 3 caracteres"),
    }),
});

// --- respostas ---
export const apiKeyRegisterResponseSchema = z.object({
    id: z.string(),
    userId: z.string(),
    name: z.string(),
    hashedKey: z.string(),
    prefix: z.string()
});

export const apiKeyResponseSchema = z.object({
    id: z.string(),
    userId: z.string(),
    name: z.string(),
    prefix: z.string()
});

// --- Tipos ---
export type ApiKeyInput = z.infer<typeof apiKeySchema>["body"];
export type ApiKeyResponse = z.infer<typeof apiKeyRegisterResponseSchema>;
export type ApiKeyListResponse = z.infer<typeof apiKeyResponseSchema>;