using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StajYönetimSistemi.Data;
using StajYönetimSistemi.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
// PDF Okuma İşlemleri İçin Gerekli Kütüphaneler (iText7)
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;

namespace StajYönetimSistemi.Controllers
{
    public class OgrenciController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OgrenciController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string OgrenciNoBelirle(int kullaniciId, string eposta)
        {
            string epostaOnEki = eposta.Split('@')[0];
            if (epostaOnEki.Length == 9 && int.TryParse(epostaOnEki, out _))
            {
                return epostaOnEki;
            }
            Random rnd = new Random(kullaniciId);
            int[] yillar = { 22, 23, 24, 25, 26 };
            int yil = yillar[rnd.Next(yillar.Length)];
            string bolumKodu = "0229";
            int sira = rnd.Next(1, 1000);
            return $"{yil}{bolumKodu}{sira:D3}";
        }

        // --- VİZE 1. MADDE: BELGE OKUMA VE KARŞILAŞTIRMA MOTORU ---
        private string BelgeIcerigiDogrula(string fizikselDosyaYolu, string ogrenciAdSoyad, string ogrenciNo)
        {
            try
            {
                string okunanMetin = "";

                // Sadece PDF dosyaları için okuma işlemi yapıyoruz
                if (fizikselDosyaYolu.EndsWith(".pdf"))
                {
                    using (PdfReader reader = new PdfReader(fizikselDosyaYolu))
                    using (PdfDocument pdfDoc = new PdfDocument(reader))
                    {
                        // PDF'in tüm sayfalarındaki metinleri çıkarıp birleştiriyoruz
                        for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                        {
                            okunanMetin += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
                        }
                    }
                }
                else
                {
                    return "Şu anki sistem sadece PDF dosyalarının içeriğini otomatik okuyabilmektedir. Word belgesi için manuel yönetici onayı bekleniyor.";
                }

                // --- KARŞILAŞTIRMA MANTIĞI ---
                // Metindeki boşlukları ve Türkçe karakter farklılıklarını tolere etmek için her şeyi küçültüyoruz
                string islenenMetin = okunanMetin.ToLower().Replace(" ", "").Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u").Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
                string arananAdSoyad = ogrenciAdSoyad.ToLower().Replace(" ", "").Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u").Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
                string arananNo = ogrenciNo;

                bool adEslesiyorMu = islenenMetin.Contains(arananAdSoyad);
                bool noEslesiyorMu = islenenMetin.Contains(arananNo);

                if (adEslesiyorMu && noEslesiyorMu)
                {
                    return "✅ BAŞARILI: Belgedeki ad-soyad ve öğrenci numarası sistemdeki bilgilerinizle uyuşuyor. Belge onaya hazır!";
                }
                else if (adEslesiyorMu && !noEslesiyorMu)
                {
                    return "⚠️ UYARI: Belgede isminiz bulundu ancak öğrenci numaranız sistemle eşleşmiyor veya okunamadı!";
                }
                else if (!adEslesiyorMu && noEslesiyorMu)
                {
                    return "⚠️ UYARI: Belgede öğrenci numaranız bulundu ancak ad-soyadınız sistemle eşleşmiyor!";
                }
                else
                {
                    return "❌ HATA: Yüklediğiniz belgede size ait ad-soyad veya öğrenci numarası tespit edilemedi. Lütfen başkasına ait veya boş belge yüklemeyin.";
                }
            }
            catch (Exception ex)
            {
                return "Belge okuma sırasında bir hata oluştu. Belge manuel incelemeye alındı. Hata Detayı: " + ex.Message;
            }
        }

        [HttpGet]
        public IActionResult Profil()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");

            int kullaniciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var ogrenci = _context.Kullanicilar.FirstOrDefault(k => k.Id == kullaniciId);

            if (ogrenci != null)
            {
                ViewBag.AdSoyad = ogrenci.AdSoyad;
                ViewBag.OgrenciNo = OgrenciNoBelirle(ogrenci.Id, ogrenci.Eposta);
                ViewBag.Bolum = "Yazılım Mühendisliği";
                ViewBag.Eposta = ogrenci.Eposta;
            }

            return View();
        }

        [HttpGet]
        public IActionResult EvrakYukle()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");

            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId != null)
            {
                var yuklenenEvraklar = _context.Evraklar
                    .Where(e => e.OgrenciId == kullaniciId)
                    .OrderByDescending(e => e.Id)
                    .ToList();
                ViewBag.Evraklarim = yuklenenEvraklar;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EvrakYukle(IFormFile evrakDosyasi, string googleFormUrl)
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");

            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId == null) return RedirectToAction("Login", "Auth");

            if (evrakDosyasi == null || evrakDosyasi.Length == 0)
            {
                ViewBag.Hata = "Lütfen yüklenecek bir belge (PDF, DOCX vb.) seçiniz.";
                return View();
            }

            var izinVerilenUzantilar = new[] { ".pdf", ".docx", ".doc" };
            var dosyaUzanti = Path.GetExtension(evrakDosyasi.FileName).ToLower();

            if (!izinVerilenUzantilar.Contains(dosyaUzanti))
            {
                ViewBag.Hata = "Hatalı dosya formatı! Lütfen sadece PDF veya Word (.docx) dosyası yükleyin.";
                return View();
            }

            var uploadsKlasoru = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsKlasoru)) Directory.CreateDirectory(uploadsKlasoru);

            var benzersizDosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(evrakDosyasi.FileName);
            var fizikselDosyaYolu = Path.Combine(uploadsKlasoru, benzersizDosyaAdi);

            using (var stream = new FileStream(fizikselDosyaYolu, FileMode.Create))
            {
                await evrakDosyasi.CopyToAsync(stream);
            }

            // ---- YENİ EKLENEN KARŞILAŞTIRMA ADIMI ----
            var sistemeGirenOgrenci = _context.Kullanicilar.FirstOrDefault(k => k.Id == kullaniciId.Value);
            string sistemOgrenciAd = sistemeGirenOgrenci.AdSoyad;
            string sistemOgrenciNo = OgrenciNoBelirle(sistemeGirenOgrenci.Id, sistemeGirenOgrenci.Eposta);

            // PDF'i okutuyoruz
            string analizSonucu = BelgeIcerigiDogrula(fizikselDosyaYolu, sistemOgrenciAd, sistemOgrenciNo);

            // Sonucu ekrana göndermek için ViewBag'e atıyoruz
            ViewBag.AnalizRaporu = analizSonucu;
            // ------------------------------------------

            var yeniEvrak = new Evrak
            {
                OgrenciId = kullaniciId.Value,
                DosyaYolu = "/uploads/" + benzersizDosyaAdi,
                GoogleFormUrl = string.IsNullOrEmpty(googleFormUrl) ? "Belirtilmedi" : googleFormUrl,
                Statu = "Onay Bekliyor"
            };

            _context.Evraklar.Add(yeniEvrak);
            await _context.SaveChangesAsync();

            ViewBag.Basari = "Evrağınız sisteme başarıyla yüklendi!";

            // Listeyi sayfa yenilendiğinde tekrar göstermek için
            ViewBag.Evraklarim = _context.Evraklar.Where(e => e.OgrenciId == kullaniciId).OrderByDescending(e => e.Id).ToList();

            return View();
        }

        [HttpGet]
        public IActionResult StajBilgileri()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        public IActionResult StajBilgileri(DateTime? baslangicTarihi, DateTime? bitisTarihi, List<string> stajGunleri)
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");

            if (!baslangicTarihi.HasValue || !bitisTarihi.HasValue)
            {
                ViewBag.Hata = "Lütfen başlangıç ve bitiş tarihlerini tam olarak seçiniz.";
                return View();
            }
            if (bitisTarihi.Value < baslangicTarihi.Value)
            {
                ViewBag.Hata = "Bitiş tarihi, başlangıç tarihinden önce olamaz. Lütfen tarihleri düzeltiniz.";
                return View();
            }
            if (stajGunleri == null || stajGunleri.Count == 0)
            {
                ViewBag.Hata = "Lütfen en az bir staj günü seçiniz.";
                return View();
            }

            int? kullaniciId = HttpContext.Session.GetInt32("KullaniciId");
            if (kullaniciId != null)
            {
                var ogrenci = _context.Kullanicilar.FirstOrDefault(k => k.Id == kullaniciId);
                if (ogrenci != null)
                {
                    ogrenci.StajBaslangicTarihi = baslangicTarihi.Value;
                    ogrenci.StajBitisTarihi = bitisTarihi.Value;
                    ogrenci.StajGunleri = string.Join(", ", stajGunleri);
                    _context.SaveChanges();
                    ViewBag.Basari = "Staj tarihleriniz ve çalışma günleriniz başarıyla veritabanına kaydedildi!";
                }
            }
            else
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        [HttpGet]
        public IActionResult KontrolSonucu()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");

            int ogrenciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var sonEvrak = _context.Evraklar
                .Where(e => e.OgrenciId == ogrenciId)
                .OrderByDescending(e => e.Id)
                .FirstOrDefault();

            if (sonEvrak != null)
            {
                ViewBag.BelgeAdi = "Ek-16.pdf";
                ViewBag.SonucMesaji = sonEvrak.Statu == "Onaylandı" ? "Belgeniz kontrol edildi ve onaylandı." :
                                      sonEvrak.Statu == "Reddedildi" ? "Belgeniz reddedildi. Lütfen tekrar yükleyin." :
                                      "Belgeniz inceleniyor.";
            }
            return View();
        }

        [HttpGet]
        public IActionResult BasvuruDurumu()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");

            int ogrenciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var sonEvrak = _context.Evraklar
                .Where(e => e.OgrenciId == ogrenciId)
                .OrderByDescending(e => e.Id)
                .FirstOrDefault();

            if (sonEvrak != null)
            {
                ViewBag.GuncelDurum = sonEvrak.Statu;
                ViewBag.BelgeAdi = "Ek-16.pdf";
                ViewBag.YuklenmeTarihi = DateTime.Now.ToShortDateString();
            }
            else
            {
                ViewBag.GuncelDurum = "Başvuru Yok";
                ViewBag.BelgeAdi = "—";
                ViewBag.YuklenmeTarihi = "—";
            }
            return View();
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Rol") != "Ogrenci") return RedirectToAction("Login", "Auth");

            int ogrenciId = HttpContext.Session.GetInt32("KullaniciId") ?? 0;
            var sonEvrak = _context.Evraklar
                .Where(e => e.OgrenciId == ogrenciId)
                .OrderByDescending(e => e.Id)
                .FirstOrDefault();

            if (sonEvrak != null)
            {
                ViewBag.BasvuruDurumu = sonEvrak.Statu;
                ViewBag.BelgeAdi = "Ek-16.pdf";
            }
            else
            {
                ViewBag.BasvuruDurumu = "Henüz başvuru yapılmadı";
                ViewBag.BelgeAdi = "—";
            }
            return View();
        }
    }
}   