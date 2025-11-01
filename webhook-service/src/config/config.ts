const env = process.env;

export const CONFIG = {
    // Configurações do RabbitMQ (lidas do docker-compose)
    RABBITMQ: {
        URL: `amqp://${env.RABBIT_USER}:${env.RABBIT_PASS}@${env.RABBITMQ_HOST}:5672`,
        QUEUE_NAME: 'payment_approved_webhooks',
    },
    PAYMENT_SERVICE_URL: `http://payment-service:8082`,
    
    PORT: env.PORT || 8083,
};