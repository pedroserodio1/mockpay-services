import * as amqp from "amqplib";
import { processMessage } from "../handlers/eventHandlers";
import { CONFIG } from "../config/config";
import { logger } from "../../logger/logger";

const QUEUE_NAME = CONFIG.RABBITMQ.QUEUE_NAME;

interface AmqpConnection {
  createChannel(): Promise<amqp.Channel>;
  close(): Promise<void>;
}

async function connectToRabbitMQ(): Promise<AmqpConnection> {
  const MAX_RETRIES = 10;
  const RETRY_DELAY = 5000;
  const RABBIT_URL = CONFIG.RABBITMQ.URL;

  for (let i = 1; i <= MAX_RETRIES; i++) {
    try {
      const conn = (await amqp.connect(
        RABBIT_URL
      )) as unknown as AmqpConnection;
      logger.info("[INFRA] Ligação RabbitMQ estabelecida com sucesso.");

      return conn;
    } catch (error) {
      logger.warn(`[INFRA] Tentando ligar ao RabbitMQ... Tentativa ${i}`);
      if (i === MAX_RETRIES) {
        throw new Error(
          "Falha crítica ao ligar ao RabbitMQ após múltiplas tentativas."
        );
      }
      await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY));
    }
  }
  throw new Error("Conexão falhou.");
}

const EXCHANGE_NAME = "delayed_expiration_events";

export async function startConsumer(
  user: string | undefined,
  pass: string | undefined,
  host: string | undefined
) {
  const QUEUE_NAME = "payment_approved_webhooks";
  const delayMs = 2000;

  let conn;
  let channel: any;

  // 1️⃣ Retry infinito para conectar
  while (!conn) {
    try {
      conn = await amqp.connect(
        `amqp://${user}:${pass}@${host || "rabbit-mq"}`
      );
      logger.info(`[RabbitMQ] Conectado ao host: ${host || "rabbit-mq"}`);
      channel = await conn.createChannel();
      logger.info(`Canal conectado: ${channel.ch}`)
    } catch (err) {
      logger.info(
        `[RabbitMQ] Falha na conexão, tentando novamente em ${delayMs}ms...`
      );
      await new Promise((res) => setTimeout(res, delayMs));
    }
  }

  // 2️⃣ Retry infinito para garantir que a fila exista
  while (true) {
    let tmpChannel: any;
    try {
      tmpChannel = await conn.createChannel();

      // checkQueue lança 404 se não existir
      await tmpChannel.checkQueue(QUEUE_NAME);
      logger.info(
        `[RabbitMQ] Fila '${QUEUE_NAME}' encontrada. Iniciando consumidor...`
      );
      await tmpChannel.close();
      break; // fila existe, sai do loop
    } catch (err: any) {
      logger.info("Caiu erro");
      if (err.code === 404 || err.message.includes("NOT_FOUND")) {
        logger.info(
          `[RabbitMQ] Fila '${QUEUE_NAME}' ainda não existe. Esperando ${delayMs}ms...`
        );
        if (tmpChannel) await tmpChannel.close(); // garante fechamento do canal temporário
        await new Promise((res) => setTimeout(res, delayMs));
      } else {
        if (tmpChannel) await tmpChannel.close();
        logger.error(`[RabbitMQ] Erro inesperado ao checar fila:`, err);
        await new Promise((res) => setTimeout(res, delayMs));
      }
    }
  }

  // 3️⃣ Inicia consumidor no canal principal
  await channel.consume(
    QUEUE_NAME,
    async (msg: any) => {
      if (!msg) return;

      logger.info(`Mensagem recebida no canal:${channel.ch}`)

      try {
        const content = JSON.parse(msg.content.toString());
        logger.info(content, "[📩] Mensagem recebida:");
        await processMessage(msg, channel);
        try {
          if (channel) {
            await channel.ack(msg);
          } else {
            logger.info("Canal não existe");
          }
        } catch (err) {
          logger.error(err, "Erro ao dar ack:");
        }
      } catch (err) {
        logger.error(err, "[❌] Erro no processamento:");
        await channel.nack(msg, false, false);
      }
    },
    { noAck: false }
  );

  logger.info(`[RabbitMQ] Consumidor ativo na fila '${QUEUE_NAME}'`);

  // 5. Lidar com fechamento de conexão
  conn.on("close", (err) => {
    logger.warn("[RabbitMQ] Canal fechado. Reconectando...");
    startConsumer(user, pass, host); // recria canal e consumidor
  });

  channel.on("close", (err: any) => {
    logger.error(err, 'Erro no canal')
  });
}
