import amqp from "amqplib";
import "dotenv/config";

const RABBIT_USER = process.env.RABBIT_USER;
const RABBIT_PASS = process.env.RABBIT_PASS;
const RABBIT_HOST = process.env.RABBITMQ_HOST;

const RABBIT_URL = `amqp://${RABBIT_USER}:${RABBIT_PASS}@${RABBIT_HOST}`;
const FailedExchange = "failed_exchange";
const ExpirationExchange = "delayed_expiration_events";
const ApprovedQueue = "payment_approved_webhooks";
const RetryQueue = "payment_approved_webhooks_retry";
const RetryDelayMs = 5000;

async function safeDeclare(declareFn: () => Promise<any>, name: string) {
  try {
    await declareFn();
  } catch (err: any) {
    if (err?.code === 406) {
      console.log(`[RabbitMQ] '${name}' já existe, ignorando declaração.`);
    } else {
      throw err;
    }
  }
}

async function setup() {
  let conn;
  let channel;

  // Retry infinito na conexão
  while (!conn) {
    try {
      conn = await amqp.connect(RABBIT_URL);
      channel = await conn.createChannel();
      console.log(`[RabbitMQ] Conectado ao host.`);
    } catch (err) {
      console.log(`[RabbitMQ] Falha na conexão, tentando novamente em 2s...`);
      await new Promise((res) => setTimeout(res, 2000));
    }
  }

  if (!channel) throw new Error("Canal RabbitMQ não disponível.");

  // Exchanges
  await safeDeclare(
    () => channel.assertExchange(FailedExchange, "direct", { durable: true }),
    FailedExchange
  );
  await safeDeclare(
    () =>
      channel.assertExchange(ExpirationExchange, "x-delayed-message", {
        durable: true,
        arguments: { "x-delayed-type": "direct" },
      }),
    ExpirationExchange
  );

  // Fila principal
  await safeDeclare(
    () =>
      channel.assertQueue(ApprovedQueue, {
        durable: true,
        arguments: { "x-dead-letter-exchange": FailedExchange },
      }),
    ApprovedQueue
  );

  // Fila de retry
  await safeDeclare(
    () =>
      channel.assertQueue(RetryQueue, {
        durable: true,
        arguments: {
          "x-dead-letter-exchange": ExpirationExchange,
          "x-dead-letter-routing-key": ApprovedQueue,
          "x-message-ttl": RetryDelayMs,
        },
      }),
    RetryQueue
  );

  // Bindings
  await safeDeclare(
    () => channel.bindQueue(RetryQueue, ExpirationExchange, RetryQueue),
    `${RetryQueue} -> ${ExpirationExchange}`
  );
  await safeDeclare(
    () => channel.bindQueue(ApprovedQueue, ExpirationExchange, ApprovedQueue),
    `${ApprovedQueue} -> ${ExpirationExchange}`
  );

  console.log("[SETUP] Exchanges, filas e bindings criados com sucesso.");
  await channel.close();
  await conn.close();
}

setup().catch((err) => {
  console.error("[SETUP] Erro ao configurar RabbitMQ:", err);
  process.exit(1);
});
