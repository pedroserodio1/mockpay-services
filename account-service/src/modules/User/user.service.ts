import { UserModel } from "./user.model";
import { RegisterUserInput, LoginUserInput } from "./user.schema";


export class UserService {
  async registerUser(input: RegisterUserInput) {
    const existingUser = await UserModel.findOne({
      $or: [{ email: input.email }, { username: input.username }],
    });

    if (existingUser) {
      throw new Error("Email ou username já cadastrado");
    }

    try {
      const user = await UserModel.create({
        email: input.email,
        username: input.username,
        passwordHash: input.password,
      });

      // @ts-ignore
      user.passwordHash = "";
      return user;
    } catch (e) {
      throw new Error("Erro ao criar usuário");
    }
  }

  async loginUser(input: LoginUserInput) {
    const user = await UserModel.findOne({ email: input.email }).select(
      "+passwordHash"
    );

    if (!user) {
      throw new Error("Email ou senha inválidos");
    }

    const isPasswordCorrect = await user.comparePassword(input.password);

    if (!isPasswordCorrect) {
      throw new Error("Email ou senha inválidos");
    }
    
    user.passwordHash = ""

    return user;


  }
}
