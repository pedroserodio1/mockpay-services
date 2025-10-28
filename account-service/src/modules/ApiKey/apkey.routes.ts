import { FastifyInstance } from "fastify";
import { verifyJWT } from "../User/user.middleware";
import { createApiKeyHandler, deleteApiKeyHandler, listApiKeysHandler } from "./apikey.controller";

async function apiKeyRoutes(app: FastifyInstance) {
    app.get("/", {
        preHandler: [verifyJWT],
        handler: listApiKeysHandler
    });

    app.post("/create", {
        preHandler: [verifyJWT],
        handler: createApiKeyHandler
    })

    app.delete("/:id", {
        preHandler: [verifyJWT],
        handler: deleteApiKeyHandler
    });
}

export default apiKeyRoutes;