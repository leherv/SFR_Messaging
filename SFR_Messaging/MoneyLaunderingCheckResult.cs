using System;
using System.Text.Json.Serialization;

namespace SFR_Messaging
{
    public class MoneyLaunderingCheckResult
    {
        public string Id { get; set; }
        public MoneyLaunderingStatus Status { get; set; }

        public MoneyLaunderingCheckResult(PaymentTransaction paymentTransaction)
        {
            Id = paymentTransaction.Id;
            Status = paymentTransaction.Value > 1000 ? MoneyLaunderingStatus.Declined : MoneyLaunderingStatus.Accepted;
        }
        
        public MoneyLaunderingCheckResult() {}
    }
}