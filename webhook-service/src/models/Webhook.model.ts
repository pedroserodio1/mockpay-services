import mongoose, { Schema, Document } from 'mongoose';

export interface IWebhook extends Document {
    userId: string; 
    url: string;    
    events: string[]; 
    isActive: boolean;
}

const WebhookSchema: Schema = new Schema({
    userId: { type: String, required: true, index: true }, 
    url: { type: String, required: true, unique: true },
    events: { 
        type: [String], 
        default: ['payment.approved', 'payment.cancel'] 
    },
    isActive: { type: Boolean, default: true },
}, { timestamps: true });

export const WebhookModel = mongoose.model<IWebhook>('Webhook', WebhookSchema);