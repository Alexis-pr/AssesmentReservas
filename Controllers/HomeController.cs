using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AssesmentReservas.API.DTOs.Properties;
using AssesmentReservas.API.Interfaces.Properties;
using AssesmentReservas.API.Models;

namespace AssesmentReservas.API.Controllers;

public class HomeController : Controller
{
    private readonly IPropertyService _properties;

    public HomeController(IPropertyService properties) => _properties = properties;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var featured = await _properties.SearchAsync(new PropertySearchDto { Page = 1, PageSize = 6 }, ct);
        return View(featured.Items);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
