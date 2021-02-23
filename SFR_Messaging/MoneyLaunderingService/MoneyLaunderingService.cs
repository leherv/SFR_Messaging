using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFR_Messaging.Config;

namespace SFR_Messaging.MoneyLaunderingService
{
    public class MoneyLaunderingService : BackgroundService
    {
        private readonly ILogger<MoneyLaunderingService> _logger;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ConsumerConfig _consumerConfig;
        private readonly ProducerConfig _producerConfig;

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public MoneyLaunderingService(ILogger<MoneyLaunderingService> logger, IOptions<KafkaSettings> kafkaSettings)
        {
            _logger = logger;
            _kafkaSettings = kafkaSettings.Value;
            _consumerConfig = new ConsumerConfig
            {
                GroupId = "money-laundering-service-consumer-group",
                BootstrapServers = _kafkaSettings.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
            using var p = new ProducerBuilder<string, string>(_producerConfig).Build();
            try
            {
                consumer.Subscribe("payments");
                while (true)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        var paymentTransaction =
                            JsonSerializer.Deserialize<PaymentTransaction>(consumeResult.Message.Value);
                        _logger.LogInformation("Consumed payment with id: {id} sender: {sender} recipient: {recipient} value: {value}", consumeResult.Message.Key, paymentTransaction?.Sender, paymentTransaction?.Recipient, paymentTransaction?.Value.ToString(CultureInfo.InvariantCulture));
                        var moneyLaunderingCheckResult = new MoneyLaunderingCheckResult(paymentTransaction);

                        try
                        {
                            var message = new Message<string, string>
                            {
                                Key = moneyLaunderingCheckResult.Id,
                                Value = JsonSerializer.Serialize(moneyLaunderingCheckResult, _serializerOptions)
                            };
                            var deliveryResult = await p.ProduceAsync("laundering-check", message, stoppingToken);
                            _logger.LogInformation("Delivered money laundering check result with id: {id} and status: {status}", deliveryResult.Message?.Key, moneyLaunderingCheckResult.Status.ToString());
                            p.Flush(TimeSpan.FromSeconds(10));

                        }
                        catch (ProduceException<string, string> pE)
                        {
                            _logger.LogError("Delivery failed: {reason}", pE.Error.Reason);
                        }
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError("Error occured: {reason}", e.Error.Reason);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Something went wrong in the MoneyLaunderingService: {exception}", e.Message);
                consumer.Close();
            }
        }
    }
}