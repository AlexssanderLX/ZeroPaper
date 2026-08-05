using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/platform-billing")]
public sealed class PublicPlatformBillingController(IPlatformBillingService billingService) : ControllerBase
{
    [HttpPost("confirm/{confirmationToken}")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> ConfirmAsync(string confirmationToken, CancellationToken cancellationToken) =>
        Ok(await billingService.ConfirmSignupPaymentAsync(confirmationToken, cancellationToken));
}
