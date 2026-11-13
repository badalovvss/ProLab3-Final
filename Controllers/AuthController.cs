using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StajYönetimSistemi.Data;
using StajYönetimSistemi.Models;
using System.Linq;

namespace StajYönetimSistemi.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string adSoyad, string eposta, string sifre, string sifreTekrar)
        {
            // 1. Şifre eşleşme kontrolü
            if (sifre != sifreTekrar)
            {
                ViewBag.Hata = "Girdiğiniz şifreler birbiriyle uyuşmuyor.";
                return View();
            }

            // 2. E-posta daha önce alınmış mı kontrolü
            bool epostaVarMi = _context.Kullanicilar.Any(k => k.Eposta == eposta);
            if (epostaVarMi)
            {
                ViewBag.Hata = "Bu e-posta adresi zaten sisteme kayıtlı.";
                return View();
            }

            // 3. Yeni Öğrenci nesnesini oluştur ve şifreyi BCrypt ile hash'le
            var yeniOgrenci = new Ogrenci
            {
                AdSoyad = string.IsNullOrWhiteSpace(adSoyad) ? "Öğrenci" : adSoyad,
                Eposta = eposta,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(sifre),
                HataliGirisSayisi = 0,
                HesapKilitliMi = false,
                // Sisteme dışarıdan kayıt olan herkes varsayılan olarak öğrencidir
            };

            // 4. Veritabanına kaydet
            _context.Kullanicilar.Add(yeniOgrenci);
            _context.SaveChanges();

            // 5. Kayıt başarılıysa giriş ekranına yönlendir
            return RedirectToAction("Login", "Auth");
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string eposta, string sifre)
        {
            // Sayfa yenilendiğinde e-postanın kutuda kalması için:
            ViewBag.GirilenEposta = eposta;

            // ==========================================
            // VİZE TESTİ İÇİN SABİT ADMİN GİRİŞİ
            // ==========================================
            if (eposta == "admin@staj.com" && sifre == "admin123")
            {
                HttpContext.Session.SetString("Rol", "Admin");
                HttpContext.Session.SetString("OgrenciAdSoyad", "Sistem Yöneticisi");
                return RedirectToAction("Index", "Admin");
            }

            var kullanici = _context.Kullanicilar.FirstOrDefault(k => k.Eposta == eposta);

            if (kullanici == null)
            {
                ViewBag.Hata = "Böyle bir e-posta adresi bulunamadı veya hatalı giriş.";
                return View();
            }

            if (kullanici.HesapKilitliMi)
            {
                ViewBag.Hata = "Hesabınız çok fazla hatalı giriş nedeniyle kilitlenmiştir.";
                return View();
            }

            // Şifre Kontrolü (BCrypt ve Düz Metin destekli)
            bool sifreDogruMu = false;
            try
            {
                // Eğer şifre hash'lenmişse BCrypt ile doğrula
                sifreDogruMu = BCrypt.Net.BCrypt.Verify(sifre, kullanici.SifreHash);
            }
            catch
            {
                // Eğer veritabanına elle '123456' gibi düz metin yazıldıysa patlamaması için:
                sifreDogruMu = (sifre == kullanici.SifreHash);
            }

            if (!sifreDogruMu)
            {
                kullanici.HataliGirisSayisi++;

                if (kullanici.HataliGirisSayisi >= 5)
                {
                    kullanici.HesapKilitliMi = true;
                    ViewBag.Hata = "5 kez hatalı giriş yaptınız. Hesabınız kilitlendi.";
                }
                else
                {
                    ViewBag.Hata = $"Hatalı şifre. Kalan deneme hakkınız: {5 - kullanici.HataliGirisSayisi}";
                }

                _context.SaveChanges();
                return View();
            }

            // Başarılı giriş
            kullanici.HataliGirisSayisi = 0;
            _context.SaveChanges();

            HttpContext.Session.SetInt32("KullaniciId", kullanici.Id);

            if (kullanici is Admin)
            {
                HttpContext.Session.SetString("Rol", "Admin");
                return RedirectToAction("Index", "Admin");
            }
            else if (kullanici is Ogrenci)
            {
                HttpContext.Session.SetString("Rol", "Ogrenci");
                return RedirectToAction("Index", "Ogrenci");
            }

            return View();
        }

        public IActionResult Logout()
        {
            // Tüm oturum verilerini sil ve çıkış yap
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}