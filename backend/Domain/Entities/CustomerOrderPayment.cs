using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class CustomerOrderPayment : TenantOwnedEntity
{
    private CustomerOrderPayment()
    {
    }

    public CustomerOrderPayment(Guid tenantId, PaymentMethod method, decimal amount) : base(tenantId)
    {
        Update(method, amount);
    }

    public CustomerOrderPayment(Guid tenantId, Guid customerOrderId, PaymentMethod method, decimal amount) : this(tenantId, method, amount)
    {
        CustomerOrderId = customerOrderId;
    }

    public Guid CustomerOrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }

    public CustomerOrder CustomerOrder { get; private set; } = null!;

    public void Update(PaymentMethod method, decimal amount)
    {
        if (method == PaymentMethod.Undefined)
        {
            throw new ArgumentException("Informe uma forma de pagamento valida.", nameof(method));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("O valor do pagamento precisa ser maior que zero.", nameof(amount));
        }

        Method = method;
        Amount = decimal.Round(amount, 2);
        Touch();
    }
}
