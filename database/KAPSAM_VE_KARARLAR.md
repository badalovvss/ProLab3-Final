# Kaynak incelemesi ve veritabanı kararları

İncelenen kaynaklar: `26_27_guz_prolab_3_proje.pdf` (6 sayfanın tamamı), `WhatsApp Image 2026-10-21 at 14.51.53.jpeg` (vize listesi), `Database.jpg`, `Class.jpg`, `Flowchart.jpg`.

Öncelik: kullanıcının yalnızca vize/veritabanı sınırı; PDF'nin açık vize-final ayrımı; defterdeki vize listesi; diyagramların bu kapsamla uyumlu kısımları. Diyagramlar bütün projeyi gösterdiği için final dalları çalışır hale getirilmedi.

## Bütün proje maddelerinin karşılığı

| Kaynak / madde | İstenen bilgi veya davranış | Vize veritabanı kararı |
|---|---|---|
| PDF s.1 §1 | Ek-16 işleme, denetleme, rapor, Ek-17 | Ek-16 yükleme metaverisi var; analiz, rapor üretimi ve Ek-17 yok |
| PDF s.1 §2 | Yalnız Ek-16; temel giriş; güvenlik ayrıntıları sonraya | Belge türü sabit Ek-16. §4.1 açıkça hash istediğinden hash ve kilit desteği şimdi var |
| PDF s.1 §3 | Öğrenci/admin | `KULLANICI.rol`; yalnız `ROLE_STUDENT`, `ROLE_ADMIN` |
| PDF s.1-2 §4.1 | Kullanıcı adı/e-posta ve parola | Benzersiz `kullanici_adi`, benzersiz `eposta`, `sifre_hash` |
| PDF s.2 §4.1 | Parolaları hashleme | Örneklerde tuzlu bcrypt; doğrulama backend'de |
| PDF s.2 §4.1 | Hatalı giriş logu, 5 denemede kilit | `GIRIS_DENEMESI`, kullanıcı sayacı, kilit ve transaction'lı prosedür |
| PDF s.2 §4.1 | Rol session/JWT içinde, çıkışta iptal | JWT yerine şartnamede izin verilen sunucu oturumu seçildi; `OTURUM`, çıkış prosedürü |
| PDF s.2 §4.2 | Sayfa/endpoint rol denetimi, 403 | Veri ve geçerli oturum sorgusu sağlandı; HTTP davranışı backend'e ait |
| PDF s.2 §4.2 | Başvuru listeleme/filtreleme, OCR raporu | Öğrencinin kendi yüklemelerini görmesi için temel sorgu var; final admin filtre/rapor ekranları yok |
| PDF s.2 §4.2-4.3 | Admin veri seti yükleme | Tüm öğrenci sütunları, hazır SQL veri seti ve JSON girdi örneği var; dosya ayrıştırma/admin ekranı backend'e ait |
| PDF s.2 §4.3 | Ad soyad, kimlik, uyruk, öğrenci no, doğum tarihi | `KULLANICI` içinde tam karşılık; öğrencide zorunlu |
| PDF s.2 §4.3 | Adres, bölüm, teslim yılı | `ikametgah_adresi`, `bolum`, `teslim_yili`; yıl ardışık `YYYY-YYYY` |
| PDF s.2 §4.3 | Önceki staj gün sayısı | Negatif olamayan tam sayı; öğrencide zorunlu |
| PDF s.2 §4.3 | E-posta, telefon, sağlık yardımı statüsü | Alanların tamamı tutulur; e-posta/telefon zorunlu, sağlık bilgisi öğrencide zorunlu |
| PDF s.2 §4.3 | Ek-16 ile kullanıcı verisini karşılaştırma | Referans öğrenci verisi tutulur; OCR karşılaştırması final, uygulanmadı |
| PDF s.2 §4.4 | Word/PDF/JPEG/PNG dışını reddetme | Uzantı ve MIME kısıtları, pozitif boyut; gerçek bayt kontrolü backend'de |
| PDF s.3 §4.5 | OCR/HTR, boş alan, imza/kaşe raporu | Final; `RAPOR` sadece diyagram uyumlu boş kabuk, kayıt eklenmesi kapalı |
| PDF s.3 §4.6 | Google Form URL ve otomatik doldurma | Final; diyagramdaki `google_form_url` alanı tutuldu, vizede `NULL` zorunlu |
| PDF s.3 §4.7 | Tamamlananlar ve Ek-17 | Final; uygulama, veri üretimi veya tablo eklenmedi |
| PDF s.3 §5 | Yüklendi → İnceleniyor → Hatalı/Eksik → Onaylandı → Tamamlandı | Vizede yalnız ilk durum açık. Sonraki durumlara geçiş DB'de reddedilir |
| PDF s.3 §5 | Tamamlanan belgeleri değiştirememe, admin istisnası | Final. Kaynaktaki istisna çelişkisi aşağıda kayıtlı; vizede tamamlanan belge olmadığı için uygulanmadı |
| PDF s.4 §6.1 | Açık adres bileşenlerinin tamlığı | Tek adres alanı korundu; DB boş olmasını engeller, bileşen/anlam kontrolü backend'de |
| PDF s.4 §6.1 | +90 telefon, geçerli e-posta, ikisi zorunlu | DB'de temel biçim ve zorunluluk kontrolü; gerçek numara/e-posta sahipliği doğrulanmaz |
| PDF s.4 §6.1 | Faks nullable | İşveren/belge içeriği alanıdır; mevcut diyagram ve öğrenci veri setinde yok. Vizede faks tablosu/alanı eklenmedi |
| PDF s.4 §6.2 | T.C.: 11 rakam, ilk rakam sıfır değil | Öğrenci veri setine uygulanır; checksum isteğe bağlı olduğundan eklenmedi |
| PDF s.4 §6.2 | Yabancı: 11 rakam, 99 başlangıcı | Uyruk `T.C.` değilse bu kural uygulanır |
| PDF s.4 §6.3 | Ardışık öğretim yılı | Veri setine uygulanır; `2025-2026` ve `2026-2027` kabul edilir, sabit bir yıl dayatılmaz |
| PDF s.4 §6.3 | Yarıyıl ve staj tarihi uyumu | Belge içerik/OCR kontrolü; final. Dönem/staj planı tablosu eklenmedi |
| PDF s.4 §6.3 | Başlangıçtan en az 30 gün önce teslim, önerilen tarih | Belge içeriği kontrolü; final. Evrak yüklemeyi bu iş kuralıyla engelleyen vize kısıtı kurulmadı |
| PDF s.4 §6.3 | Haftalık gün seçimi, tatilleri çıkarma, tarih karşılaştırma | Final kapsamındaki OCR'ye bağlı kontrol olarak ayrıldı; takvim/tatil tablosu eklenmedi |
| PDF s.5 §6.4 | Öğrenci/danışman onayı boş, işveren kaşe/imzası dolu | Görüntü/alan analizi; final. Vizede çalıştırılmaz |
| PDF s.5 §6.5 | En az 1 mühendis; 5 altında bölüm uyarısı | İşveren/belge içeriği ve kullanıcı arayüzü kuralı; final, uygulanmadı |
| PDF s.5 §7 | Kapsülleme, soyutlama, kalıtım, çok biçimlilik | Veritabanı sınıf eşlemesini açıklar; NYP davranışları uygulama kodunda gösterilmelidir |
| PDF s.5 Vize listesi | Diyagramlar, giriş, roller, sayfalar, veri seti, dosya yükleme, rapor/kod | Veritabanı, diyagramı ve entegrasyon sorguları bu gereksinimleri destekler; sayfalar kapsam dışı |
| Defter fotoğrafı 1-7 | Vize listesi ve üç kişilik görev dağılımı | Kullanıcının veritabanı görevine odaklanıldı; arayüz/diğer ekip görevleri yapılmadı |
| PDF s.5-6 Final listesi | Google Form, Ek-17, admin rapor/filtre, OCR | Açıkça kapsam dışı |
| PDF s.6 §8 | IEEE en az 4 sayfa, demo ve grup şartları | Teslim raporu yazılmadı; açıklanabilir SQL ve teknik notlar verildi. PDF'nin raporun öğrencilere ait olması beklentisi korundu |

## Diyagramlara uygulanan açık düzeltmeler

| Kaynaktaki durum | Uygulanan karar | Gerekçe |
|---|---|---|
| `Database.jpg`: `KULLANICI`, `EVRAK`, `RAPOR` | Tablo ve alan adları korundu | Mevcut sınıflarla eşleştirmeyi kolaylaştırır |
| `teslim_yili` tipi `int` | `CHAR(9)` | Tek tam sayı `2025-2026` bilgisini kayıpsız saklayamaz |
| Öğrenci alanları ortak kullanıcı tablosunda | Admin için nullable, öğrenci için koşullu zorunlu | Admin'e sahte TC, öğrenci numarası veya staj bilgisi yazılmaz |
| `Class.jpg`: `Kullanici` soyut; `Ogrenci` ve `Admin` alt sınıf | Tek tablo + rol ayracı | Soyut sınıf için ayrı kayıt/tablo gerekmez. Uygulama kalıtımı bu eşlemeyle korunabilir |
| `Class.jpg`: öğrenci-evrak bağlantısı 1/1; `Database.jpg`: 1/çok | Veritabanı diyagramındaki 1/çok seçildi | PDF birden çok yüklemeyi ve yeniden yüklemeyi dışlamaz. Sınıf diyagramında evrak çokluğu `0..*` olarak düzeltilmeli |
| `Database.jpg`: evrak-rapor zorunlu 1/1 | Evrak tarafında `0..1` rapor, `RAPOR.evrak_id UNIQUE` | Yeni yüklenen evrakta henüz OCR raporu olmaz; vizede hiçbir evrakta rapor yoktur. Her raporun bir evrakı olması FK ile korunur |
| `Flowchart.jpg`: dosya formatı kararında iki “Evet” çıkışı | Vize için uygun dosya → Yüklendi; uygun değil → ret | Soldaki ek “Evet” döngüsü belirsizdir. OCR ve sonraki dallar vize akışına alınmadı |
| Giriş alanı yalnız e-posta | Ek `kullanici_adi` | PDF hem kullanıcı adı hem e-posta seçeneği ister |
| Giriş logu/kilit/oturum diyagramda yok | İki destek tablosu ve kullanıcı kilit alanları | Vizede istenen §4.1 davranışlarını saklayabilmek için gerekli |
| Yükleme zamanı `date` | `DATETIME(6)` | Aynı gün yapılan farklı yüklemeleri ve denemeleri ayırır |
| Dosya metaverisi sınırlı | `dosya_adi`, `mime_turu`, `dosya_boyutu`, sabit `belge_turu` | Vize yükleme kontrollerini destekler; belge içeriğini analiz etmez |

## Kaynaklardaki belirsizlikler ve seçilen yorum

- “5 hatalı kullanıcı adı şifre denemesinde sistem kilitlenir” ifadesi kapsam ve süre söylemiyor. **Bilinen kullanıcı hesabı bazında beş ardışık hata** olarak yorumlandı. Bilinmeyen adlar loglanır; olmayan hesabın kilidi tutulmaz. Tüm siteyi kilitleyen ortak sayaç oluşturulmadı. Başarılı giriş, kilitlenmeden önce sayacı sıfırlar. Kilit admin müdahalesiyle açılır. Bu bir uygulama tercihidir; öğretim elemanı farklı yorumlarsa değiştirilecek noktalar bellidir.
- PDF §2 güvenlik ayrıntılarını sonraya bırakırken §4.1 açıkça hash ve kilit ister; vize listesinde de oturum/yetkilendirme vardır. Bu nedenle açık §4.1 gerekleri uygulandı.
- PDF §4.2 tamamlanan başvuruların güncellenemeyeceğini, §5 ise adminin güncelleyip silebileceğini söylüyor. Final kapsamındaki bu istisna için şimdiden yetki kuralı uydurulmadı.
- `google_form_url` ve `RAPOR` fotoğrafta mevcut fakat işlevleri finalde. Şema uyumunu korumak için boş bırakılıp vize dışı kullanım engellendi. İşlev eklemek için gelecekte açık bir migration gerekir; bu pakette bulunmaz.
- PDF Google Form sütunlarını haber veriyor ancak listelemiyor; Ek-17 formatının ekte olduğunu söylüyor ancak verilen dosyalar arasında ayrı bir form şablonu yok. Bu eksikler finalle ilgili ve doldurulmadı.
- Sağlık statüsü için resmî seçenek listesi, maksimum dosya boyutu, oturum süresi ve veri seti dosya biçimi belirtilmiyor. Sağlık statüsü boş olmayan metin; boyut pozitif; oturum bitişi backend parametresi; test dosyası JSON olarak seçildi. Bunlar PDF'de verilmiş sabitler gibi sunulmadı.

## Veritabanı ile uygulamanın sınırı

Veritabanı; veri bütünlüğünü, ilişkileri, benzersizliği, temel biçimleri, sayaç/log/oturum kayıtlarını korur. Backend; bcrypt doğrulamasını, dosya baytlarını, dosya saklamayı, oturum çerezini, HTTP yetkilendirmesini ve admin yükleme arayüzünü yönetir. Bu ayrım nedeniyle SQL paketinin tek başına çalışan bir web uygulaması olduğu iddia edilmez.

NYP ilkeleri SQL tablosu oluşturularak kanıtlanmış olmaz. Veritabanı eşlemesi sınıfları destekler; soyut sınıf, kalıtım, interface ve çok biçimlilik ekip uygulamasında ayrıca gösterilmelidir.
