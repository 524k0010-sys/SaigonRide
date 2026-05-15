using System;
using System.Collections.Generic;
using System.Linq;
using SaigonRide.Models;

namespace SaigonRide.Services
{
    public class PaymentResult
    {
        public bool Succeeded { get; set; }
        public PaymentStatus Status { get; set; }
        public string ReferenceCode { get; set; }
        public string Message { get; set; }
    }

    public interface IPaymentStrategy
    {
        PaymentMethod Method { get; }
        PaymentResult Pay(int rentalId, decimal amount);
    }

    public abstract class SimulatedPaymentStrategy : IPaymentStrategy
    {
        protected SimulatedPaymentStrategy(PaymentMethod method, string referencePrefix)
        {
            Method = method;
            ReferencePrefix = referencePrefix;
        }

        public PaymentMethod Method { get; }
        protected string ReferencePrefix { get; }

        public PaymentResult Pay(int rentalId, decimal amount)
        {
            if (amount < 0)
            {
                return new PaymentResult
                {
                    Succeeded = false,
                    Status = PaymentStatus.Failed,
                    Message = "Payment amount cannot be negative."
                };
            }

            return new PaymentResult
            {
                Succeeded = true,
                Status = PaymentStatus.Success,
                ReferenceCode = $"{ReferencePrefix}-{rentalId}-{DateTime.Now:yyyyMMddHHmmss}",
                Message = "Simulated payment completed."
            };
        }
    }

    public sealed class CashPaymentStrategy : SimulatedPaymentStrategy
    {
        public CashPaymentStrategy() : base(PaymentMethod.Cash, "CASH")
        {
        }
    }

    public sealed class MoMoPaymentStrategy : SimulatedPaymentStrategy
    {
        public MoMoPaymentStrategy() : base(PaymentMethod.MoMo, "MOMO")
        {
        }
    }

    public sealed class VNPayPaymentStrategy : SimulatedPaymentStrategy
    {
        public VNPayPaymentStrategy() : base(PaymentMethod.VNPay, "VNPAY")
        {
        }
    }

    public sealed class PayPalPaymentStrategy : SimulatedPaymentStrategy
    {
        public PayPalPaymentStrategy() : base(PaymentMethod.PayPal, "PAYPAL")
        {
        }
    }

    public sealed class ApplePayPaymentStrategy : SimulatedPaymentStrategy
    {
        public ApplePayPaymentStrategy() : base(PaymentMethod.ApplePay, "APPLE")
        {
        }
    }

    public class PaymentGatewayService
    {
        private readonly IReadOnlyCollection<IPaymentStrategy> strategies;

        public PaymentGatewayService()
        {
            strategies = new IPaymentStrategy[]
            {
                new CashPaymentStrategy(),
                new MoMoPaymentStrategy(),
                new VNPayPaymentStrategy(),
                new PayPalPaymentStrategy(),
                new ApplePayPaymentStrategy()
            };
        }

        public PaymentResult Pay(PaymentMethod method, int rentalId, decimal amount)
        {
            var strategy = strategies.FirstOrDefault(s => s.Method == method);
            if (strategy == null)
            {
                return new PaymentResult
                {
                    Succeeded = false,
                    Status = PaymentStatus.Failed,
                    Message = "Unsupported payment method."
                };
            }

            return strategy.Pay(rentalId, amount);
        }
    }
}
