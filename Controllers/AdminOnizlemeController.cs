using System;
using Microsoft.AspNetCore.Mvc;

namespace StajYönetimSistemi.Controllers
{
    public class AdminOnizlemeController : Controller
    {
        [HttpGet]
        public IActionResult BasvuruDetay(string? kaynak)
        {
            bool tamamlananlar = string.Equals(
                kaynak,
                "tamamlananlar",
                StringComparison.OrdinalIgnoreCase
            );

            ViewData["AktifMenu"] = tamamlananlar
                ? "Tamamlananlar"
                : "Basvurular";

            ViewData["GeriAction"] = tamamlananlar
                ? "AdminTamamlananlar"
                : "AdminBasvurular";

            return View(
                "~/Views/FrontendPreview/AdminBasvuruDetay.cshtml"
            );
        }
    }
}
