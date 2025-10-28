import { z } from "zod";

// --- Schema para o Body ---

// Validação para a rota POST /register
export const registerUserSchema = z.object({
  body: z.object({
    username: z.string({error: (e) => e.input === undefined ? "Campo obrigatório" : "Entrada inválida."}).min(3, "Username deve ter no mínimo 3 caracteres"),
    email: z
      .email("Email inválido"),
    password: z.string({error: (e) => e.input === undefined ? "Campo obrigatório" : "Entrada inválida."}).min(6, "Senha deve ter no mínimo 6 caracteres"),
  }),
});

// Validação para a rota POST /login
export const loginUserSchema = z.object({
  body: z.object({
    email: z.email("Email ou senha inválidos"),
    password: z.string().min(1, "Email ou senha inválidos"),
  }),
});

// --- Schema para a Resposta ---

// O que nós enviamos de volta para o usuário
const userResponseSchema = z.object({
  id: z.string(),
  email: z.string().email(),
  createdAt: z.string(), // (Datas são convertidas para string no JSON)
});

// Resposta do login (usuário + token)
export const loginResponseSchema = z.object({
  token: z.string(),
  user: userResponseSchema,
});

// --- Tipos ---
// Gera tipos TypeScript automaticamente a partir dos schemas Zod
export type RegisterUserInput = z.infer<typeof registerUserSchema>["body"];
export type LoginUserInput = z.infer<typeof loginUserSchema>["body"];
