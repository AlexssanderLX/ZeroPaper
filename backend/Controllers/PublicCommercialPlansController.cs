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
        if (segment != BusinessSegment.Restaurant)
        {
            return NotFound();
        }

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
                "essencial" => ["Cadastro de tutores e animais", "Catalogo de servicos com precos", "Agenda interna de atendimentos", "Pedidos e cobranca vinculados"],
                "operacao" => ["Tudo do Essencial", "Agendamento publico online (link direto)", "WhatsApp com IA para agendamento", "Relatorio operacional de atendimentos", "Historico completo do animal"],
                "gestao" => ["Tudo do Operacao", "Dashboard gerencial em tempo real", "Relatorios avancados e exportacao", "Cupons, fidelizacao e recorrencia", "Gestao de equipe e comissoes"],
                _ => []
            };
        }

        return key switch
        {
            "essencial" => ["Cardapio digital com QR Code", "Mesas, pedidos, cozinha e caixa", "Chamada de garcom por mesa", "Impressao manual de comandas"],
            "operacao" => ["Tudo do Essencial", "WhatsApp com IA para atendimento", "Delivery, retirada e rastreio", "Impressao automatica de pedidos", "Relatorio de vendas diario"],
            "gestao" => ["Tudo do Operacao", "Dashboard gerencial em tempo real", "Relatorios avancados e exportacao", "Cupons, fidelizacao e recorrencia", "Gestao de equipe e comissoes"],
            _ => []
        };
    }
}
