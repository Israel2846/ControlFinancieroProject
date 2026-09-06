using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ControlFinancieroProject.Models;
using ControlFinancieroProject.Services;

namespace ControlFinancieroProject.Controllers;

public class HomeController : Controller
{
    private readonly ITransactionReportService _transactionReportService;

    public HomeController(ITransactionReportService transactionReportService)
    {
        _transactionReportService = transactionReportService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _transactionReportService.GetMonthlyReportAsync(null, null);
        return View(model);
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
