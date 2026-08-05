namespace ZeroPaper.Services.Models;

public sealed class SubscriptionExpiredException : InvalidOperationException
{
    public const string ErrorCode = "SUBSCRIPTION_EXPIRED";
    public SubscriptionExpiredException() : base("A mensalidade desta conta venceu. A conta e os dados continuam salvos; renove o plano para acessar novamente.") { }
}
