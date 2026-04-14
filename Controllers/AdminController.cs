using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WeblogApplication.Data;
using WeblogApplication.Interfaces;
using WeblogApplication.Models;

namespace WeblogApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET: /Admin
        public async Task<IActionResult> Index(string filterType = "all")
        {
            var viewModel = await _adminService.GetDashboardDataAsync(filterType);
            return View("Admin", viewModel);
        }
    }
}
