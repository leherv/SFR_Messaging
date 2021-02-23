using System;

namespace SFR_Messaging
{
    public class PaymentTransaction
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public decimal Value { get; set; }

        public PaymentTransaction(string sender, string recipient, decimal value)
        {
            Id = Guid.NewGuid().ToString();
            Sender = sender;
            Recipient = recipient;
            Value = value;
        }
        
        public PaymentTransaction() {}
    }
}