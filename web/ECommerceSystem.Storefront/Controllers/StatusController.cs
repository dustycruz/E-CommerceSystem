using ECommerceSystem.Storefront.Models;
using ECommerceSystem.Storefront.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.Storefront.Controllers;

/// <summary>"Service status" page: which microservices are up right now (handy during the demos).</summary>
public sealed class StatusController(ServiceStatusChecker serviceStatusChecker) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var services = await serviceStatusChecker.CheckAllAsync(cancellationToken);
        return View(new StatusViewModel(services, DateTime.UtcNow));
    }
}