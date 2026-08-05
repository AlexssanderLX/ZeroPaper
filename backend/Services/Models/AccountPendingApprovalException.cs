namespace ZeroPaper.Services.Models;

public sealed class AccountPendingApprovalException : InvalidOperationException
{
    public const string ErrorCode = "ACCOUNT_PENDING_APPROVAL";

    public AccountPendingApprovalException()
        : base("Seu cadastro foi recebido e está em análise. Avisaremos pelo telefone ou e-mail informado assim que o acesso for liberado.")
    {
    }
}
