using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using StajYönetimSistemi.Data;
using StajYönetimSistemi.Models;
using System.Collections.Generic;
using System;
using System.IO;

namespace StajYönetimSistemi.Controllers
{
    public class BasvuruViewModel
    {
        public int EvrakId { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public string DosyaYolu { get; set; }
        public string GoogleFormUrl { get; set; }
        public string Statu { get; set; }
    }

    public class VeriSetiRapor
    {
        public string OgrenciNo { get; set; }
        public string AdSoyad { get; set; }
        public string CsvDurum { get; set; }
        public string SistemDurum { get; set; }
        public string SonucMesaji { get; set; }
        public bool EslesiyorMu { get; set; }
    }

    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Kullanıcının e-posta adresine göre 9 haneli benzersiz öğrenci numarası üretir
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

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("Login", "Auth");
            ViewData["AktifMenu"] = "AnaSayfa";
            ViewBag.ToplamBasvuru = _context.Kullanicilar.Count();
            ViewBag.YuklenenEk16 = _context.Evraklar.Count();
            ViewBag.Tamamlanan = _context.Evraklar.Count(e => e.Statu == "Onaylandı" || e.Statu == "Reddedildi");

            return View("~/Views/Admin/Index.cshtml");
        }

        [HttpGet]
        public IActionResult VeriSeti()
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("Login", "Auth");
            return View("AdminVeriSeti");
        }

        [HttpGet]
        public IActionResult AdminProfil()
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("Login", "Auth");

            // Yönetici bilgilerini listeleme işlemi
            var admin = _context.Kullanicilar.FirstOrDefault(k => !(k is Ogrenci));

            if (admin != null)
            {
                ViewBag.AdminAdSoyad = admin.AdSoyad;
                ViewBag.AdminEposta = admin.Eposta;
            }

            return View();
        }

        [HttpGet]
        public IActionResult AdminListe()
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("Login", "Auth");

            var hamVeri = (from e in _context.Evraklar
                           join k in _context.Kullanicilar on e.OgrenciId equals k.Id
                           select new { e, k }).ToList();

            var tumBasvurular = hamVeri.Select(x => new BasvuruViewModel
            {
                EvrakId = x.e.Id,
                OgrenciAdSoyad = x.k.AdSoyad,
                OgrenciNo = OgrenciNoBelirle(x.k.Id, x.k.Eposta),
                DosyaYolu = x.e.DosyaYolu,
                GoogleFormUrl = x.e.GoogleFormUrl,
                Statu = x.e.Statu
            }).ToList();

            ViewBag.TumBasvurular = tumBasvurular;
            return View();
        }

        [HttpPost]
        public IActionResult EvrakDurumGuncelle(int evrakId, string yeniDurum)
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("AdminListe");

            var evrak = _context.Evraklar.FirstOrDefault(e => e.Id == evrakId);
            if (evrak != null)
            {
                evrak.Statu = yeniDurum;
                _context.SaveChanges();
            }

            return RedirectToAction("AdminListe");
        }

        // Yönetici için kayıtlı evrakı sistemden tamamen silme işlemi
        [HttpPost]
        public IActionResult EvrakSil(int evrakId)
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("AdminListe");

            var evrak = _context.Evraklar.FirstOrDefault(e => e.Id == evrakId);
            if (evrak != null)
            {
                // Sunucuda tutulan fiziksel dosyayı temizle
                var dosyaYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", evrak.DosyaYolu.TrimStart('/'));
                if (System.IO.File.Exists(dosyaYolu))
                {
                    System.IO.File.Delete(dosyaYolu);
                }

                _context.Evraklar.Remove(evrak);
                _context.SaveChanges();
            }

            return RedirectToAction("AdminListe");
        }

        [HttpGet]
        public IActionResult Tamamlananlar()
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("Login", "Auth");
            ViewData["AktifMenu"] = "Tamamlananlar";

            var hamVeri = (from e in _context.Evraklar
                           join k in _context.Kullanicilar on e.OgrenciId equals k.Id
                           where e.Statu != "Onay Bekliyor"
                           select new { e, k }).ToList();

            var tamamlananBasvurular = hamVeri.Select(x => new BasvuruViewModel
            {
                EvrakId = x.e.Id,
                OgrenciAdSoyad = x.k.AdSoyad,
                OgrenciNo = OgrenciNoBelirle(x.k.Id, x.k.Eposta),
                DosyaYolu = x.e.DosyaYolu,
                GoogleFormUrl = x.e.GoogleFormUrl,
                Statu = x.e.Statu
            }).ToList();

            ViewBag.TumBasvurular = tamamlananBasvurular;
            return View("AdminListe");
        }

        [HttpGet]
        public IActionResult AdminBasvuruDetay(int id)
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("Login", "Auth");

            var hamDetay = (from e in _context.Evraklar
                            join k in _context.Kullanicilar on e.OgrenciId equals k.Id
                            where e.Id == id
                            select new { e, k }).FirstOrDefault();

            if (hamDetay == null) return RedirectToAction("AdminListe");

            ViewBag.Detay = new
            {
                AdSoyad = hamDetay.k.AdSoyad,
                Eposta = hamDetay.k.Eposta,
                OgrenciNo = OgrenciNoBelirle(hamDetay.k.Id, hamDetay.k.Eposta),
                Bolum = "Yazılım Mühendisliği",
                DosyaYolu = hamDetay.e.DosyaYolu,
                Statu = hamDetay.e.Statu
            };

            ViewData["AktifMenu"] = "Basvurular";
            return View("~/Views/Admin/AdminBasvuruDetay.cshtml");
        }

        // CSV formatındaki test veri seti ile sistem verilerinin karşılaştırılması
        [HttpPost]
        public IActionResult VeriSetiKarsilastir(IFormFile veriDosyasi)
        {
            if (HttpContext.Session.GetString("Rol") != "Admin") return RedirectToAction("Login", "Auth");
            ViewData["AktifMenu"] = "VeriSeti";

            if (veriDosyasi == null || veriDosyasi.Length == 0)
            {
                ViewBag.Hata = "Lütfen karşılaştırma için bir test veri seti (CSV dosyası) seçin.";
                return View("AdminVeriSeti");
            }

            if (!veriDosyasi.FileName.EndsWith(".csv"))
            {
                ViewBag.Hata = "Hata: Sistem sadece .csv uzantılı test veri setlerini kabul etmektedir.";
                return View("AdminVeriSeti");
            }

            var raporListesi = new List<VeriSetiRapor>();

            using (var reader = new StreamReader(veriDosyasi.OpenReadStream()))
            {
                // Başlık satırını atla
                if (!reader.EndOfStream) reader.ReadLine();

                var sistemVerileri = (from e in _context.Evraklar
                                      join k in _context.Kullanicilar on e.OgrenciId equals k.Id
                                      select new { e, k }).ToList();

                while (!reader.EndOfStream)
                {
                    var satir = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(satir)) continue;

                    var sutunlar = satir.Split(new[] { ',', ';' });

                    if (sutunlar.Length >= 3)
                    {
                        string csvNo = sutunlar[0].Trim();
                        string csvAd = sutunlar[1].Trim();
                        string csvDurum = sutunlar[2].Trim();

                        var sistemKaydi = sistemVerileri.FirstOrDefault(x => OgrenciNoBelirle(x.k.Id, x.k.Eposta) == csvNo);

                        var rapor = new VeriSetiRapor
                        {
                            OgrenciNo = csvNo,
                            AdSoyad = csvAd,
                            CsvDurum = csvDurum
                        };

                        // Sistem verisi ile CSV verisinin statü eşleşme kontrolü
                        if (sistemKaydi != null)
                        {
                            rapor.SistemDurum = sistemKaydi.e.Statu;

                            if (rapor.SistemDurum.ToLower() == csvDurum.ToLower())
                            {
                                rapor.SonucMesaji = "Eşleşti";
                                rapor.EslesiyorMu = true;
                            }
                            else
                            {
                                rapor.SonucMesaji = "Uyuşmazlık: Sistemdeki statü farklı";
                                rapor.EslesiyorMu = false;
                            }
                        }
                        else
                        {
                            rapor.SistemDurum = "Bulunamadı";
                            rapor.SonucMesaji = "Hata: Kayıt sistemde bulunamadı";
                            rapor.EslesiyorMu = false;
                        }

                        raporListesi.Add(rapor);
                    }
                }
            }

            ViewBag.Rapor = raporListesi;
            return View("AdminVeriSeti");
        }

        // Sunum öncesi test kayıtlarının temizlenmesi ve veritabanı sıfırlama
        [HttpGet]
        public IActionResult SunumIcinTemizle()
        {
            // 1. Veritabanındaki mevcut tüm evrak kayıtlarını sil
            var tumEvraklar = _context.Evraklar.ToList();
            _context.Evraklar.RemoveRange(tumEvraklar);

            // 2. Öğrenci rolündeki tüm kullanıcıları temizle
            var silinecekOgrenciler = _context.Kullanicilar.OfType<Ogrenci>().ToList();
            _context.Kullanicilar.RemoveRange(silinecekOgrenciler);

            // 3. Yönetici (Admin) hesabını varsayılan sunum bilgilerine döndür
            var admin = _context.Kullanicilar.FirstOrDefault(k => !(k is Ogrenci));
            if (admin != null)
            {
                admin.AdSoyad = "İrfan Kösesoy";
                admin.Eposta = "irfankosesoy@kocaeli.edu.tr";
            }

            _context.SaveChanges();

            // 4. wwwroot/uploads klasöründeki fiziksel dosyaları temizle
            var uploadsKlasoru = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (Directory.Exists(uploadsKlasoru))
            {
                var dosyalar = Directory.GetFiles(uploadsKlasoru);
                foreach (var dosya in dosyalar)
                {
                    System.IO.File.Delete(dosya);
                }
            }

            return Content("Sistem başarıyla sıfırlandı. Test kayıtları temizlendi.");
        }
    }
}