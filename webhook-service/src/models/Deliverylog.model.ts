import mongoose, { Schema, Document } from 'mongoose';

export interface IDeliveryLog extends Document {
    webhookId: mongoose.Types.ObjectId;
    txId: string;
    attempt: number;
    status: 'SENT' | 'FAILED' | 'RETRY'; 
    httpStatusCode: number;
    eventPayload: any; 
}

const DeliveryLogSchema: Schema = new Schema({
    webhookId: { type: Schema.Types.ObjectId, ref: 'Webhook', required: true, index: true },
    txId: { type: String, required: true, index: true },
    attempt: { type: Number, required: true, default: 1 },
    status: { type: String, enum: ['SENT', 'FAILED', 'RETRY'], required: true },
    httpStatusCode: { type: Number },
    eventPayload: { type: Schema.Types.Mixed, required: true },
}, { timestamps: true });

export const DeliveryLogModel = mongoose.model<IDeliveryLog>('DeliveryLog', DeliveryLogSchema);