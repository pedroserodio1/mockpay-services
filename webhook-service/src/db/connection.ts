import mongoose from 'mongoose';
import { logger } from '../../logger/logger';

export async function connectDB() {
    const mongoUri = process.env.MONGO_URI;

    if (!mongoUri) {
        logger.error("ERRO CRÍTICO: Variável de ambiente MONGO_URI não está definida.");
        process.exit(1);
    }
    
    try {
        await mongoose.connect(mongoUri, {
        });
        logger.info('[INFRA] MongoDB (Webhook-DB) conectado com sucesso.');
    } catch (error) {
        logger.error(error, '[CRITICAL] Falha ao conectar ao MongoDB:');
        throw error;
    }
}