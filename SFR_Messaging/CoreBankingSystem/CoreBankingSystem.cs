using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFR_Messaging.Config;

namespace SFR_Messaging.CoreBankingSystem
{
    public class CoreBankingSystem : BackgroundService
    {
        private readonly ILogger<CoreBankingSystem> _logger;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ProducerConfig _config;

        private readonly List<PaymentTransaction> _paymentTransactions = new()
        {
            new PaymentTransaction("John", "Tom", new decimal(300)),
            new PaymentTransaction("Beate", "Joe", new decimal(1100)),
            new PaymentTransaction("Chris", "Hank", new decimal(200)),
            new PaymentTransaction("Jim", "Susi", new decimal(3333)),
        };

        public CoreBankingSystem(ILogger<CoreBankingSystem> logger, IOptions<KafkaSettings> kafkaSettings)
        {
            _logger = logger;
            _kafkaSettings = kafkaSettings.Value;
            _config = new ProducerConfig {BootstrapServers = _kafkaSettings.BootstrapServers};
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var p = new ProducerBuilder<string, string>(_config).Build();
                var i = 0;
                do
                {
                    var paymentTransaction = _paymentTransactions[i];
                    var message = new Message<string, string>
                    {
                        Key = paymentTransaction.Id,
                        Value = JsonSerializer.Serialize(paymentTransaction)
                    };
                    var deliveryResult = await p.ProduceAsync("payments", message, stoppingToken);
                    _logger.LogInformation("Delivered payment with id: {id} sender: {sender} recipient: {recipient} value: {value}", deliveryResult.Key, paymentTransaction.Sender, paymentTransaction.Recipient, paymentTransaction.Value.ToString(CultureInfo.InvariantCulture));
                    p.Flush(TimeSpan.FromSeconds(10));
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                    i++;
                } while (!stoppingToken.IsCancellationRequested && i < _paymentTransactions.Count);
            }
            catch (Exception e)
            {
                if (e is ProduceException<string, string> pE)
                {
                    _logger.LogError("Delivery failed: {reason}", pE.Error.Reason);
                }
                else
                {
                    _logger.LogError("Something went wrong in the CoreBankingSystem: {exception}", e.Message);
                }
            }
        }
    }
}