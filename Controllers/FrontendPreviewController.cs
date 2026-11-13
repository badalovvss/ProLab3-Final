using Microsoft.AspNetCore.Mvc;

namespace StajYönetimSistemi.Controllers
{
    public class FrontendPreviewController : Controller
    {
        // GİRİŞ

        public IActionResult Login()
        {
            return View("~/Views/Account/Login.cshtml");
        }


        // ÖĞRENCİ SAYFALARI

        public IActionResult Ogrenci()
        {
            return View("~/Views/FrontendPreview/Ogrenci.cshtml");
        }

        public IActionResult StajBilgileri()
        {
            return View("~/Views/FrontendPreview/StajBilgileri.cshtml");
        }

        public IActionResult Ek16Yukle()
        {
            return View("~/Views/FrontendPreview/Ek16Yukle.cshtml");
        }

        public IActionResult KontrolSonucu()
        {
            return View("~/Views/FrontendPreview/KontrolSonucu.cshtml");
        }

        public IActionResult BasvuruDurumu()
        {
            return View("~/Views/FrontendPreview/BasvuruDurumu.cshtml");
        }

        public IActionResult GoogleForm()
        {
            return View("~/Views/FrontendPreview/GoogleForm.cshtml");
        }

        public IActionResult Yardim()
        {
            return View("~/Views/FrontendPreview/Yardim.cshtml");
        }

        public IActionResult Iletisim()
        {
            return View("~/Views/FrontendPreview/Iletisim.cshtml");
        }

        public IActionResult Profil()
        {
            return View("~/Views/FrontendPreview/Profil.cshtml");
        }


        // GİRİŞ EKRANI BİLGİ SAYFALARI

        public IActionResult GirisBilgi(string konu)
        {
            if (
                konu != "yardim" &&
                konu != "iletisim" &&
                konu != "sifre"
            )
            {
                return RedirectToAction("Login");
            }

            ViewData["Konu"] = konu;

            return View("~/Views/FrontendPreview/GirisBilgi.cshtml");
        }

        public IActionResult YardimDetay(string konu)
        {
            if (
                konu != "ek16" &&
                konu != "staj" &&
                konu != "durum" &&
                konu != "giris"
            )
            {
                return RedirectToAction("Login");
            }

            ViewData["Konu"] = konu;

            return View("~/Views/FrontendPreview/YardimDetay.cshtml");
        }


        // ADMİN ÖNİZLEME SAYFALARI

        public IActionResult AdminPanel()
        {
            return View("~/Views/FrontendPreview/Admin.cshtml");
        }

        public IActionResult AdminVeriSeti()
        {
            return View("~/Views/FrontendPreview/AdminVeriSeti.cshtml");
        }

        public IActionResult AdminBasvurular()
        {
            ViewData["ListeBasligi"] = "Başvurular";
            ViewData["AktifMenu"] = "Basvurular";

            return View("~/Views/FrontendPreview/AdminListe.cshtml");
        }

        public IActionResult AdminTamamlananlar()
        {
            ViewData["ListeBasligi"] = "Tamamlananlar";
            ViewData["AktifMenu"] = "Tamamlananlar";

            return View("~/Views/FrontendPreview/AdminListe.cshtml");
        }
    }
}