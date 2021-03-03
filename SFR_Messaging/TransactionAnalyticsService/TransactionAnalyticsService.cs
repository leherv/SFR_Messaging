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

namespace SFR_Messaging.TransactionAnalyticsService
{
    public class TransactionAnalyticsService : BackgroundService
    {
        private readonly ILogger<TransactionAnalyticsService> _logger;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ConsumerConfig _consumerConfig;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter()}
        };
        
        // TODO: using KTable from kafka would have been smarter
        private (int accepted, int declined) _stats = new(0, 0);

        public TransactionAnalyticsService(ILogger<TransactionAnalyticsService> logger,
            IOptions<KafkaSettings> kafkaSettings)
        {
            _logger = logger;
            _kafkaSettings = kafkaSettings.Value;
            _consumerConfig = new ConsumerConfig
            {
                GroupId = "transaction-analytics-service-group",
                BootstrapServers = _kafkaSettings.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
            try
            {
                consumer.Subscribe("laundering-check");
                while (true)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        var moneyLaunderingCheckResult =
                            JsonSerializer.Deserialize<MoneyLaunderingCheckResult>(consumeResult.Message.Value, _serializerOptions);
                        UpdateStatistics(moneyLaunderingCheckResult);
                        _logger.LogInformation("Consumed money laundering check result with id: {id} and status: {status}", consumeResult.Message?.Key, moneyLaunderingCheckResult?.Status.ToString());
                        _logger.LogInformation("Current statistics:\nAccepted payments: {accepted}\nDeclined payments: {declined}", _stats.accepted.ToString(), _stats.declined.ToString());
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Error occured: {e.Error.Reason}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Something went wrong in the TransactionAnalyticsService: {exception}", e.Message);
                consumer.Close();
            }
        }

        private void UpdateStatistics(MoneyLaunderingCheckResult moneyLaunderingCheckResult)
        {
            var status = moneyLaunderingCheckResult.Status;
            if (status == MoneyLaunderingStatus.Accepted)
            {
                _stats.accepted++;
            }
            else
            {
                _stats.declined++;
            }
        }
    }
}