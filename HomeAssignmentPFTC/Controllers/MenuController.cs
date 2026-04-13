using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeAssignmentPFTC.Models;

namespace HomeAssignmentPFTC.Controllers;

public class MenuController :Controller
{
    private readonly ILogger<MenuController> _logger;

    public MenuController(ILogger<MenuController> logger)
    {
        _logger = logger;
    }
    
    public IActionResult Index()
    {
        return View();
    }
}