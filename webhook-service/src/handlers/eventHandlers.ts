// /webhook-service/src/eventHandlers.ts
import * as amqp from "amqplib";
import {
  handleExpirationCheck,
  handleDelayedApproval,
  handleWebhookDispatch,
} from "../actions/eventActions";
import type { MessagePayload } from "../interfaces/MessagePayload";
import { logger } from "../../logger/logger";

export async function processMessage(
  msg: amqp.ConsumeMessage,
  channel: amqp.Channel
) {
  const content = msg.content.toString();

  try {
    const payload: MessagePayload = JSON.parse(content);
    logger.info(
      `[EVENT] Recebido TxId: ${payload.TxId} | Evento: ${
        payload.Event || payload.Status
      }`
    );

    if (payload.Event === "expiration_check") {
      await handleExpirationCheck(payload);
    } else if (payload.Event === "delayed_approval") {
      await handleDelayedApproval(payload);
    } else if (payload.Status === "PAID" || payload.Status === "CANCELLED") {
      await handleWebhookDispatch(payload);
    }

    try {
      channel.ack(msg);
    } catch (ackErr) {
      logger.warn(
        ackErr, `[ACK] Falha ao confirmar mensagem, canal possivelmente fechado. TxId: ${payload.TxId}`,
        
      );
    }
    logger.info(
      `[ACK] Mensagem de TxId ${payload.TxId} processada com sucesso.`
    );
  } catch (error) {
    logger.error(
      error,
      `[NACK] Falha ao processar mensagem. Enviando para fila de Retry.`
    );

    try {
      channel.nack(msg, false, false);
    } catch (nackErr) {
      logger.warn(
        nackErr, `[NACK] Falha ao reenfileirar mensagem, canal possivelmente fechado.`,
        
      );
      // opcional: publicar manualmente na fila de retry se existir
    }
  }
}
