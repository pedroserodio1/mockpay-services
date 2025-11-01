using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using PaymentService.Domains;
using PaymentService.Models;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

public class RabbitMQPublisher : IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string ApprovedQueue = "payment_approved_webhooks";
    private const string ExpirationExchange = "delayed_expiration_events";

    public RabbitMQPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private const int MaxConnectionAttempts = 10;
    private const int RetryDelayMs = 5000;

    private const string RetryExchange = "retry_exchange";
    private const string RetryQueue = "retry_delay_queue";
    private const int RetryDelayMsRetry = 10000; // 10 segundos de espera

    private const string FailedExchange = "failed_exchange";



    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RABBITMQ_HOST"] ?? "rabbit-mq",
            UserName = _configuration["RABBITMQ_USER"] ?? "mockpay",
            Password = _configuration["RABBITMQ_PASS"] ?? "mockpay"
        };

        for (int attempt = 1; ; attempt++) // retry infinito até conectar
        {
            try
            {
                Console.WriteLine($"RabbitMQ Publisher: Tentando conectar (Tentativa {attempt})...");
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(); // só cria canal

                Console.WriteLine("[RabbitMQ] Conexão estabelecida.");
                return;
            }
            catch (Exception ex) when (ex is BrokerUnreachableException || ex is SocketException)
            {
                Console.WriteLine($"Falha na ligação (Erro: {ex.Message}). Tentando novamente em {RetryDelayMs / 1000}s...");
                await Task.Delay(RetryDelayMs, cancellationToken);
            }
        }
    }
    public async Task PublishPaymentApproved(Payment payment)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Canal RabbitMQ não está aberto.");
        }

        var messageBody = JsonSerializer.Serialize(new
        {
            TxId = payment.TxId,
            Status = payment.Status.ToString(),
            OwnerUserId = payment.OwnerUserId
        });

        var body = Encoding.UTF8.GetBytes(messageBody);

        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: ApprovedQueue,
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: body);

        Console.WriteLine($"Publicado pagamento aprovado: {payment.TxId}");
    }

    public async Task PublishExpirationCheck(string txId, TimeSpan delay)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Canal RabbitMQ não está aberto.");
        }

        var messageBody = JsonSerializer.Serialize(new
        {
            TxId = txId,
            Event = "expiration_check"
        });

        var body = Encoding.UTF8.GetBytes(messageBody);

        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                { "x-delay", (long)delay.TotalMilliseconds }
            }
        };

        await _channel.BasicPublishAsync(
            exchange: ExpirationExchange,
            routingKey: ApprovedQueue,
            mandatory: false,
            basicProperties: properties,
            body: body);

        Console.WriteLine($"Publicado check de expiração para {txId} com delay de {delay.TotalMinutes} minutos");
    }

    public async Task PublishDelayedApproval(string txId, long delay)
    {
        try
        {
            Console.WriteLine("======================================================");
            Console.WriteLine($"[LOG-PUB] Tentativa de publicar DELAYED APPROVAL");
            Console.WriteLine($"[LOG-PUB] TxId: {txId}");
            Console.WriteLine($"[LOG-PUB] Delay: {delay} ms ({delay.GetType()})");

            if (_channel == null)
            {
                Console.WriteLine($"[LOG-ERROR] Falha: Canal RabbitMQ não está aberto.");
                throw new InvalidOperationException("Canal RabbitMQ não está aberto.");
            }

            // Criando o body da mensagem
            var messageBody = JsonSerializer.Serialize(new
            {
                TxId = txId,
                Event = "delayed_approval",
                TimestampUtc = DateTime.UtcNow
            });

            var body = Encoding.UTF8.GetBytes(messageBody);

            Console.WriteLine($"[LOG-PUB] Body da mensagem (JSON): {messageBody}");
            Console.WriteLine($"[LOG-PUB] Body em bytes: {BitConverter.ToString(body)}");

            // Definindo headers
            var properties = new BasicProperties
            {
                Headers = new Dictionary<string, object?>
            {
                { "x-delay", delay }
            }
            };

            Console.WriteLine("[LOG-PUB] Propriedades da mensagem (Headers):");
            foreach (var header in properties.Headers!)
            {
                Console.WriteLine($"   {header.Key} = {header.Value} ({header.Value?.GetType()})");
            }

            Console.WriteLine($"[LOG-PUB] Exchange: {ExpirationExchange}");
            Console.WriteLine($"[LOG-PUB] RoutingKey: {ApprovedQueue}");
            Console.WriteLine($"[LOG-PUB] Mandatory: false");

            // Publicando a mensagem
            await _channel.BasicPublishAsync(
                exchange: ExpirationExchange,
                routingKey: ApprovedQueue,
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            Console.WriteLine($"[LOG-PUB-OK] Mensagem publicada com sucesso para {txId} com delay de {delay}ms.");
            Console.WriteLine("======================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine($"[LOG-CRITICO-FALHA] ERRO AO PUBLICAR NO RABBITMQ!");
            Console.WriteLine($"Tipo: {ex.GetType().Name}");
            Console.WriteLine($"Mensagem: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            Console.WriteLine("======================================================");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("RabbitMQ Publisher: Encerrando...");

        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}