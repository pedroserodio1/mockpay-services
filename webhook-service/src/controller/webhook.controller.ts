import type { Request, Response } from 'express';
import { WebhookModel } from '../models/Webhook.model';
import { logger } from '../../logger/logger';

interface CreateWebhookRequest {
    url: string;
    events?: string[];
}

export const registerWebhook = async (req: Request, res: Response) => {
    
    const { url, events } = req.body as CreateWebhookRequest;
    
    const userId = req.headers['x-user-id'] as string; 
    
    if (!userId) {
        return res.status(401).json({ message: "X-User-ID header ausente (Autenticação falhou)." });
    }
    if (!url) {
        return res.status(400).json({ message: "O campo 'url' é obrigatório." });
    }

    try {
        const newWebhook = await WebhookModel.create({
            userId,
            url,
            events: events || ['PAID']
        });

        return res.status(201).json({ 
            message: "Webhook registado com sucesso.", 
            data: newWebhook.toJSON() 
        });
        
    } catch (error: any) {
        if (error.code === 11000) {
             return res.status(409).json({ message: "Este URL de webhook já está registado." });
        }
        logger.error(error,"Erro ao registar webhook:");
        return res.status(500).json({ message: "Erro interno do servidor." });
    }
}