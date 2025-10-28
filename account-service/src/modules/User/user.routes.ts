
import { FastifyInstance } from 'fastify';
import { registerUserSchema, loginUserSchema } from './user.schema';
import { registerHandler, loginHandler } from './user.controller';

async function userRoutes(app: FastifyInstance) {

  app.post('/register', {
    handler: registerHandler,
    schema: registerUserSchema,
  });

  app.post('/login', {
    handler: loginHandler,
    schema: loginUserSchema,
  });
}

export default userRoutes;