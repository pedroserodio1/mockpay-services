import express from "express";
import mongoose from "mongoose";
import { connectDB } from "./db/connection"; // Seu ficheiro de conexão Mongoose
import webhookRoutes from "./routes/webhook.routes";
import { startConsumer } from "./consumer/consumer";
import { logger } from "../logger/logger";

const PORT = process.env.PORT || 8083;

async function bootstrap() {
  try {
    const RABBIT_USER = process.env.RABBIT_USER;
    const RABBIT_PASS = process.env.RABBIT_PASS;
    const RABBIT_HOST = process.env.RABBITMQ_HOST;

    logger.info(`RabbitMQ user: ${RABBIT_USER}`)
    logger.info(`RabbitMQ pass: ${RABBIT_PASS}`)
    logger.info(`RabbitMQ host: ${RABBIT_HOST}`);

    logger.info("[INFRA] A ligar ao MongoDB...");
    await connectDB();

    // 2. CONFIGURAÇÃO E INÍCIO DO EXPRESS (API Síncrona - Porta 8083)
    const app = express();
    app.use(express.json()); // Middleware para ler JSON

    // Ligação das Rotas de Gestão (Protegidas pelo NGINX)
    app.use("/api/webhooks", webhookRoutes);

    // Health Check simples
    app.get("/api/webhooks/health", (req, res) =>
      res.json({
        status: "API OK",
        db: mongoose.connection.readyState === 1 ? "OK" : "FAIL",
        service: "webhook-service",
      })
    );

    // Middleware de Erro (para evitar que a API crash)
    app.use((err: any, req: any, res: any, next: any) => {
      logger.error(err.stack);
      res.status(500).send({ message: "Erro interno do servidor." });
    });

    // Iniciar o servidor Express
    app.listen(PORT, () => {
      logger.info(
        `[API] Webhook Manager API a correr em http://localhost:${PORT}`
      );
    });

    // 3. INÍCIO DO CONSUMIDOR RABBITMQ (Worker Assíncrono)
    // O .catch garante que a falha na ligação do RabbitMQ não derruba o servidor HTTP.
    startConsumer(RABBIT_USER, RABBIT_PASS, RABBIT_HOST).catch((err) => {
      logger.error(
        "[CRITICAL] Falha ao iniciar consumidor RabbitMQ. Verifique a infraestrutura.",
        err
      );
      // Continua a correr a API, mas o Worker falhou.
    });
  } catch (error) {
    logger.error(error, "[CRITICAL] Falha ao arrancar o serviço:");
    process.exit(1);
  }
}

bootstrap();
