import { FastifyInstance } from "fastify";
import { validateHandler } from "./validate.controller";

async function validateRoutes(app: FastifyInstance) {
    //@ts-ignore
  app.get("/validate", { handler: validateHandler });
}

export default validateRoutes;
