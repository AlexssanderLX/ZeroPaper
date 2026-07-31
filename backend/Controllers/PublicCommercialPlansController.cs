using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Public;

namespace ZeroPaper.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/commercial-plans")]
public sealed class PublicCommercialPlansController : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public ActionResult<IReadOnlyList<PublicCommercialPlanDto>> Get([FromQuery] BusinessSegment segment)
    {
        var plans = SegmentCommercialPlanCatalog.GetPlans(segment);
        return Ok(plans.Select(plan => new PublicCommercialPlanDto
        {
            Segment = segment,
            Key = plan.Key,
            Name = plan.Name,
            MonthlyPrice = plan.MonthlyPrice,
            MaxUsers = plan.DefaultMaxUsers,
            Recommended = plan.Tier == CommercialPlanTier.Operation,
            Features = GetFeatures(segment, plan.Key)
        }));
    }

    private static IReadOnlyList<string> GetFeatures(BusinessSegment segment, string key)
    {
        if (segment == BusinessSegment.PetShop)
        {
            return key switch
            {
                "essencial" => ["Cadastro de tutores e animais", "Catalogo de servicos", "Agenda interna", "Historico de atendimentos", "Ate 3 usuarios"],
                "operacao" => ["Tudo do Essencial", "Agendamento publico online", "WhatsApp com IA integrada", "Pedidos e cobrancas vinculados", "Relatorio operacional", "Ate 5 usuarios"],
                "gestao" => ["Tudo do Operacao", "Dashboard gerencial", "Relatorios avancados", "Cupons e recorrencia", "Gestao ampliada da equipe", "Ate 8 usuarios"],
                _ => []
            };
        }

        return key switch
        {
            "essencial" => ["Cardapio digital", "Mesas com QR Code", "Pedidos, cozinha e caixa", "Impressao manual", "Ate 3 usuarios"],
            "operacao" => ["Tudo do Essencial", "WhatsApp com IA", "Delivery e retirada", "Impressao automatica", "Ate 5 usuarios"],
            "gestao" => ["Tudo do Operacao", "Dashboard gerencial", "Relatorios avancados", "Cupons e recorrencia", "Ate 8 usuarios"],
            _ => []
        };
    }
}
