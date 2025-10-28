
import Fastify from 'fastify';
import mongoosePlugin from '@/plugins/db.js';
import 'dotenv/config';
import jwtAuthPlugin from '@/plugins/auth.js';
import { ZodTypeProvider } from 'fastify-type-provider-zod';
import userRoutes from '@/modules/User/user.routes.js';


const app = Fastify({
  logger: true,
}).withTypeProvider<ZodTypeProvider>();

const PORT = process.env.PORT || 8081;


app.get('/api/auth/health', async () => {
  return { status: 'OK', service: 'account-service' };
});

async function main() {
  try {
    
    await app.register(mongoosePlugin);
    await app.register(jwtAuthPlugin)

    await app.register(userRoutes, { prefix: '/api/auth' });
    

    
    await app.listen({
      port: Number(PORT),
      host: '0.0.0.0', 
    });

    app.log.info(`Account-service rodando na porta ${PORT}`);

  } catch (e) {
    app.log.error(e);
    process.exit(1);
  }
}


main();