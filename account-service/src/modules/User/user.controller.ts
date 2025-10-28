
import { FastifyRequest, FastifyReply } from 'fastify';
import { UserService } from './user.service';
import { RegisterUserInput, LoginUserInput } from './user.schema';


const userService = new UserService();

export async function registerHandler(
  // Pega o 'body' e 'reply'
  req: FastifyRequest<{ Body: RegisterUserInput }>,
  reply: FastifyReply
) {
  try {
    
    const user = await userService.registerUser(req.body);
    
    
    return reply.code(201).send(user);

  } catch (e: any) {
    
    return reply.code(400).send({ message: e.message });
  }
}


export async function loginHandler(
  req: FastifyRequest<{ Body: LoginUserInput }>,
  reply: FastifyReply
) {
  try {
    

    req.log.info({env: process.env.JWT_SECRET}, 'JWT SECRET NO HANDLER DE LOGIN');


    const user = await userService.loginUser(req.body);

    
    const token = await reply.jwtSign({
      id: user._id,
      email: user.email,
      username: user.username,
    });
    
    
    return reply.code(200).send({ token, user });

  } catch (e: any) {
    
    return reply.code(401).send({ message: e.message });
  }
}