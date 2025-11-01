export interface MessagePayload {
    TxId: string;
    Status: string;
    Event?: string; 
    OwnerUserId: string; 
    WebhookUrl?: string;
}