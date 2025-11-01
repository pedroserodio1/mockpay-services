using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using PaymentService.Domains;
using PaymentService.Models;

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

    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RABBITMQ_HOST"] ?? "rabbit-mq",
            UserName = _configuration["RABBITMQ_USER"],
            Password = _configuration["RABBITMQ_PASS"]
        };

        for (int attempt = 1; attempt <= MaxConnectionAttempts; attempt++)
        {
            try
            {
                Console.WriteLine($"RabbitMQ Publisher: A tentar ligar (Tentativa {attempt}/{MaxConnectionAttempts})...");

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync();


                Console.WriteLine("RabbitMQ Publisher: Ligação estabelecida.");
                return; 
            }
            catch (Exception ex) when (ex is System.Net.Sockets.SocketException || ex is RabbitMQ.Client.Exceptions.BrokerUnreachableException)
            {
                Console.WriteLine($"Falha na ligação (Erro: {ex.Message}). A tentar novamente em {RetryDelayMs / 1000} segundos...");

                if (attempt == MaxConnectionAttempts)
                {
                    throw new InvalidOperationException("Falha crítica ao ligar ao RabbitMQ após múltiplas tentativas.", ex);
                }

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

    public async Task PublishDelayedApproval(string txId, TimeSpan delay)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Canal RabbitMQ não está aberto.");
        }

        var messageBody = JsonSerializer.Serialize(new
        {
            TxId = txId,
            Event = "delayed_approval"
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

        Console.WriteLine($"Publicado check de aprovação para {txId} com delay de {delay.TotalSeconds} segundos");
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