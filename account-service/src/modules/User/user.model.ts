// /src/modules/users/user.model.ts
import { Schema, model, Document } from 'mongoose';
import bcrypt from 'bcryptjs';

export interface IUser extends Document {
  email: string;
  passwordHash: string;
  username: string; 
  comparePassword(password: string): Promise<boolean>;
}

const userSchema = new Schema<IUser>({
  email: {
    type: String,
    required: true,
    unique: true, 
    lowercase: true,
    trim: true,
  },
  passwordHash: {
    type: String,
    required: true,
  },
  username: { 
    type: String,
    required: true, 
  },

}, {
  timestamps: true,
});


userSchema.pre('save', async function (next) {
  
  if (!this.isModified('passwordHash')) {
    return next();
  }
  
  try {
    
    const salt = await bcrypt.genSalt(10);
    
    
    this.passwordHash = await bcrypt.hash(this.passwordHash, salt);
    
    next();
  } catch (err) {
    // @ts-ignore
    next(err);
  }
});


userSchema.methods.comparePassword = function (password: string): Promise<boolean> {

  return bcrypt.compare(password, this.passwordHash);
};


// Exporta o modelo
export const UserModel = model<IUser>('User', userSchema);