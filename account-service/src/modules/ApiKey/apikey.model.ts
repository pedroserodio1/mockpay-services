import { Schema, model, Document, Types } from 'mongoose';
import crypto from 'crypto';

export interface IApiKey extends Document {
  userId: Types.ObjectId; 
  prefix: string;
  hashedKey: string;
  name: string;
}

const ApiKeySchema = new Schema<IApiKey>({
  userId: {
    type: Schema.Types.ObjectId, 
    ref: 'User', 
    required: true 
  },
  name: { 
    type: String, 
    required: true 
  },
  
  prefix: { 
    type: String, 
    required: true 
  },
  
  
  hashedKey: { 
    type: String, 
    required: true, 
    unique: true 
  },

}, {
  timestamps: true,
});

ApiKeySchema.pre('save', async function (next) {
    
    
    if (!this.isModified('hashedKey')) {
        return next();
    }
    
    try {
        
        const keyHash = crypto
            .createHash('sha256')
            .update(this.hashedKey) 
            .digest('hex');      
            
        
        this.hashedKey = keyHash;
        
        next();

    } catch (err) {
        // @ts-ignore
        next(err);
    }
});

export const ApiKeyModel = model<IApiKey>('ApiKey', ApiKeySchema);
