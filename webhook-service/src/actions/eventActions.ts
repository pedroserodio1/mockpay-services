// /webhook-service/src/eventActions.ts
import { CONFIG } from "../config/config";
import axios from "axios";
import { WebhookModel, type IWebhook } from "../models/Webhook.model"; // Seu modelo de webhooks
import { DeliveryLogModel } from "../models/Deliverylog.model"; // Seu modelo de logs
import type { MessagePayload } from "../interfaces/MessagePayload";
import { logger } from "../../logger/logger";

async function callInternalUpdate(
  txId: string,
  action: "EXPIRED" | "PAID"
) {
  const url = `${CONFIG.PAYMENT_SERVICE_URL}/internal/update-status`;

  const param = {
    TxId: txId,
    Action: action,
  }

  logger.info(`🔹 Enviando para C#: ${param}` );

  try {
    const response = await axios.post(
      url,
      {
        TxId: txId,
        Action: action,
      },
      {
        headers: { "Content-Type": "application/json" },
        timeout: 5000,
      }
    );
    logger.info("✅ Resposta do C#:", response.data);
  } catch (err) {
    if (axios.isAxiosError(err)) {
      logger.error({ status: err.response?.status, data: err.response?.data},"❌ Erro Axios:");
    } else {
      logger.error(err,"❌ Erro desconhecido:" );
    }
  }
}

export async function handleExpirationCheck(payload: MessagePayload) {
  await callInternalUpdate(payload.TxId, "EXPIRED");
  logger.info(`[ACTION] Sucesso: TxId ${payload.TxId} marcado como EXPIRED.`);
}

export async function handleDelayedApproval(payload: MessagePayload) {
  await callInternalUpdate(payload.TxId, "PAID");
  logger.info(`[ACTION] Sucesso: TxId ${payload.TxId} marcado como PAID.`);
  await handleWebhookDispatch({ ...payload, Status: "PAID" });
}

export async function handleWebhookDispatch(payload: MessagePayload) {
  const webhooks: IWebhook[] = await WebhookModel.find({
    userId: payload.OwnerUserId,
    isActive: true,
    events: { $in: [payload.Status] },
  });

  if (webhooks.length === 0) {
    logger.info(
      `[WEBHOOK] Nenhum webhook ativo encontrado para o UserID: ${payload.OwnerUserId}`
    );
    return;
  }

  const dispatchPromises = webhooks.map((webhook) =>
    sendWebhook(webhook.id, webhook.url, payload)
  );

  await Promise.allSettled(dispatchPromises);
}

async function sendWebhook(
  webhookId: string,
  url: string,
  payload: MessagePayload
) {
  let logStatus: "SENT" | "FAILED" | "RETRY" = "FAILED";
  let httpCode: number = 0;

  try {
    const response = await axios.post(url, payload, {
      timeout: 10000,
    });

    httpCode = response.status;
    logStatus =
      response.status >= 200 && response.status < 300 ? "SENT" : "FAILED";
  } catch (error: any) {
    httpCode = error.response?.status || 0;
    logStatus = "FAILED";
  } finally {
    await DeliveryLogModel.create({
      webhookId,
      txId: payload.TxId,
      attempt: 1,
      status: logStatus,
      httpStatusCode: httpCode,
      eventPayload: payload,
    });

    if (logStatus === "FAILED") {
      logger.error(
        `[WEBHOOK FAIL] Falha ao enviar para ${url}. HTTP: ${httpCode}`
      );
    } else {
      logger.info(`[WEBHOOK SUCCESS] Sucesso ao enviar para ${url}.`);
    }
  }
}
