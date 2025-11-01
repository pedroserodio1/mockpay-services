import { Router } from 'express';
import { registerWebhook } from '../controller/webhook.controller';

const router = Router();
router.post('/', registerWebhook); 
export default router;