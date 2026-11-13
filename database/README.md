# PROLAB III — Vize veritabanı

Bu paket, **Staj Evrak Doğrulama ve Yönetim Sistemi** için yalnızca veritabanı çalışmasıdır. Kaynaklar: verilen altı sayfalık proje PDF'sinin tamamı, vize görevlerinin yazıldığı defter fotoğrafı, Database.jpg, Class.jpg ve Flowchart.jpg. Hedef motor **MySQL 8.0.16 ve üzeri / MySQL 8.4**. MySQL 5.7 ve MariaDB hedeflenmedi.

## 1. MySQL Workbench ile kurulum

1. MySQL Server bağlantınızı Workbench'te açın. Workbench tek başına veritabanı sunucusu değildir.
2. `File > Open SQL Script` ile `01_sema.sql` dosyasını açın. Sorgu hatasında yürütmeyi durdurma seçeneği açıkken tüm dosyayı çalıştırın. `prolab3_vize` adında yeni veritabanı oluşur.
3. Aynı ad zaten varsa devam etmeyin. Betik mevcut veritabanını silmez; mevcut şemaya uygulanacak migration değildir. Bu paket için boş bir veritabanı kullanın.
4. `02_test_verisi.sql` dosyasını açıp çalıştırın. **Bir admin ve beş öğrenci** eklenir. Yalnızca ilk kurulumda bir kez çalıştırın.
5. `SCHEMAS` bölümünü yenileyin. `prolab3_vize > Tables` altında beş tablo görünür.
6. `03_ornek_sorgular.sql` içindeki ilk `SELECT` bloğunu çalıştırın. `KULLANICI = 6`, diğer dört tablo `0` olmalıdır.

Komut satırı alternatifi, dosyaların bulunduğu klasörde:

```bash
mysql --default-character-set=utf8mb4 -u root -p < 01_sema.sql
mysql --default-character-set=utf8mb4 -u root -p < 02_test_verisi.sql
```

Her komut başarılı olmadan sonrakine geçmeyin. `--force` kullanmayın. Bu komutlar Windows `cmd` veya Bash içindir; PowerShell'de Workbench yolunu kullanabilirsiniz.

## 2. Tablolar

| Tablo | Vizedeki görevi |
|---|---|
| `KULLANICI` | Ortak kullanıcı alanları, öğrenci veri setinin bütün alanları, roller ve giriş kilidi |
| `EVRAK` | Öğrencinin yüklediği Ek-16'nın dosya yolu, formatı, yükleme zamanı ve dosya metaverisi |
| `RAPOR` | Diyagram uyumu için boş yapı; vizede kayıt eklenmesi engellenir |
| `GIRIS_DENEMESI` | Başarılı/hatalı/kilitli/pasif/bilinmeyen kullanıcı giriş kayıtları; parola içermez |
| `OTURUM` | Rol taşıyan, süresi dolan ve çıkışta iptal edilen sunucu oturumları |

İlişkiler: **KULLANICI 1 → 0..N EVRAK**, **EVRAK 1 → 0..1 RAPOR**, **KULLANICI 1 → 0..N OTURUM**. Bir giriş denemesi en fazla bir kullanıcıya bağlıdır; bilinmeyen girişlerde kullanıcı kimliği `NULL` olur.

`veritabani_diyagrami.svg` ve `.png` şemanın görselleridir. `KAPSAM_VE_KARARLAR.md` bütün proje maddelerinin değerlendirmesini ve diyagramlardaki farkları açıklar.

## 3. Test hesapları

| Kullanıcı adı | E-posta | Demo parolası | Rol |
|---|---|---|---|
| `admin_demo` | `admin@example.invalid` | `AdminDemo!2026` | `ROLE_ADMIN` |
| `ogrenci01` ... `ogrenci05` | `ogrenci01@example.invalid` ... `ogrenci05@example.invalid` | `VizeDemo!2026` | `ROLE_STUDENT` |

Bunlar yalnızca deneme hesaplarıdır. Kimlik, adres ve telefon alanları yapay örneklerdir; gerçek kişi verisi olarak kullanılmaz. Kimlik numarası kontrolü şartnamedeki biçim kontrolüdür; isteğe bağlı T.C. kontrol basamağı algoritması uygulanmadı. Üç T.C. ve iki yabancı uyruklu öğrenci vardır. Öğrenci numaralarının başındaki `0` korunur.

Tabloda açık parola yoktur. Her hesap için ayrı tuzlu **bcrypt, maliyet 12** özeti vardır. Demo parolaları yalnızca bu örnek hesapların denenebilmesi için dokümantasyonda yer alır. SQL'deki hash biçim kontrolü, gerçek parola doğrulamasının yerini tutmaz.

## 4. Giriş/çıkış ekibine bağlantı sözleşmesi

1. Backend, kullanıcıyı kullanıcı adı veya e-postayla bulur. `sifre_hash` istemciye gönderilmez.
2. Parolayı kendi bcrypt kütüphanesiyle doğrular. Bilinmeyen kullanıcıda da genel bir hata mesajı döndürür.
3. Başarılı doğrulamada kriptografik rastgele en az 32 baytlık bir oturum token'ı üretir. Token'ın SHA-256 özeti 64 küçük harfli hex karakter olarak veritabanına gider. Ham token yalnızca uygun korumaları olan oturum çerezinde taşınır.
4. Her deneme için aşağıdaki prosedürü çağırır. **`p_sifre_dogru` tarayıcıdan alınmaz; backend'in bcrypt sonucudur.** DB bağlantısı yalnızca güvenilir backend'de bulunur.

```sql
CALL sp_giris_sonucu_kaydet(
    ?, -- kullanıcı adı veya e-posta
    ?, -- backend doğrulama sonucu: 0 / 1
    ?, -- istemci IP'si; bilinmiyorsa NULL
    ?, -- başarılıysa yeni token'ın SHA-256 özeti; başarısızsa NULL
    ?, -- başarılıysa UTC son kullanma zamanı; başarısızsa NULL
    @giris_kabul, @oturum_id
);
SELECT @giris_kabul, @oturum_id;
```

`?` işaretleri uygulamadaki prepared statement parametreleridir; bu örnek doğrudan Workbench'te değer atamadan çalıştırılmaz. Prosedür kendi transaction'ını yönetir, başka transaction içine konmaz. Aynı kullanıcıya gelen eşzamanlı denemeler `SELECT ... FOR UPDATE` ile sıraya alınır. Kullanıcı adı ve e-posta aynı sayacı kullanır.

Beş ardışık hatalı parola denemesinde hesap kilitlenir. Kilitlenmeden önce başarılı giriş sayacı sıfırlar. Kilitli hesap doğru parolayla da açılmaz. Şartnamede süre verilmediğinden otomatik 15/30 dakika gibi bir süre eklenmedi. Geçerli admin oturumuyla `sp_hesap_kilidini_ac(admin_token_hash, hedef_id)` çağrılabilir. Bu işlem hedef hesabın eski oturumlarını da iptal eder. Tüm admin hesapları kilitliyse yerel DB yöneticisinin müdahalesi gerekir; otomatik kurtarma akışı bu pakette yoktur.

Her korumalı istekte `03_ornek_sorgular.sql` içindeki geçerli oturum sorgusu çalıştırılır. Rol, aktiflik, kilit, süre ve iptal birlikte kontrol edilir. Çıkışta `CALL sp_cikis_yap(?)` çağrılır ve tarayıcıdaki çerez temizlenir. **Sadece tabloda `rol` alanı olması sayfaları korumaz:** endpoint yetkisi ve HTTP 403 uygulama sorumluluğudur. MySQL `root` hesabı web uygulamasına gömülmez; backend için ayrı, gerekli izinlere sahip DB kullanıcısı açılır.

## 5. Admin veri seti yükleme ekibine bağlantı sözleşmesi

`ogrenci_test_verisi.json`, kaynak PDF 4.3'teki tüm sütunları içeren dosya girdisidir. Dosya biçimi kaynakta belirtilmediğinden JSON seçildi; CSV zorunluluğu varsayılmadı. `kullanici_adi` giriş için eklenmiştir. `sifre_hash` ve `rol` dosyadan güvenilerek alınmaz: backend her öğrenciye hash üretir, rolü sabit `ROLE_STUDENT` yapar.

Geçerli admin oturumunu doğrulayın; bütün satırları tek transaction içinde, parametreli `INSERT` ile ekleyin. Bir satır hatalıysa tüm dosya geri alınır. Aynı TC, öğrenci no, kullanıcı adı veya e-posta tekrarında hata verilir; sessizce üzerine yazılmaz. Seed SQL ile eklenmiş aynı öğrencileri JSON'dan tekrar yüklemeyin. Hazır SQL veri seti veritabanını hemen deneyebilmeniz içindir; web sayfasındaki dosya ayrıştırıcı ve yükleme düğmesi ekibinizin uygulama kısmıdır.

Tarih biçimi `YYYY-MM-DD`, öğretim yılı `YYYY-YYYY`, uyruk T.C. öğrencilerinde tam `T.C.`, diğerlerinde ülke adıdır. Kimlik ve öğrenci numaraları JSON içinde de tırnaklı metindir. Telefon örneği `+90 000 000 00 01` biçimindedir.

## 6. Evrak yükleme ekibine bağlantı sözleşmesi

Yalnızca `.doc`, `.docx`, `.pdf`, `.jpg`, `.jpeg`, `.png` kaydı kabul edilir. JPG ve JPEG aynı görüntü türünün uzantılarıdır. DB uzantı/MIME eşleşmesini, pozitif boyutu ve öğrenci sahipliğini kontrol eder. Boyut üst sınırı şartnamede verilmediğinden eklenmedi. Dosyanın ikili içeriği tabloda tutulmaz; `dosya_yolu` backend'in ürettiği benzersiz saklama yoludur.

**DB, dosya yolundan dosyanın gerçekten PDF veya Ek-16 olduğunu anlayamaz.** Uzantısı değiştirilmiş dosyayı, dosya imzasını ve içerik türünü backend denetler. `belge_turu='EK-16'` veri sınıflandırmasını sınırlar; OCR ile içerik tanıdığı anlamına gelmez. `03_ornek_sorgular.sql` içindeki oturumla sahiplik bağlayan `INSERT ... SELECT` örneğini kullanın. SQL başarısızsa yeni kaydedilmiş dosyayı temizleyin; etkilenen satır 0 ise başarı bildirmeyin.

Vizede evrakın tek durumu **`Yüklendi`** olur. `google_form_url` daima `NULL` kalır. `RAPOR` boştur ve ekleme tetikleyicisiyle korunur. OCR, imza/kaşe, işveren kontrolü, staj takvimi, otomatik onay, Google Form ve Ek-17 uygulanmamıştır.

## 7. Teknik notlar ve testler

- InnoDB, yabancı anahtarlar, benzersiz anahtarlar, `CHECK` kısıtları ve gerekli tetikleyiciler kullanılır.
- Metinlerde `utf8mb4` kullanılır. E-posta ve kullanıcı adı büyük/küçük harfe duyarsız benzersizdir; dosya yolu ve teknik kodlar duyarlıdır.
- Her backend bağlantısında `SET time_zone = '+00:00'` ve şemadaki strict SQL mode uygulanır. Ekran tarihleri Türkiye saatine uygulamada çevrilir.
- Evrakı bulunan kullanıcı yanlışlıkla silinemez. Evrak sahibi sonradan değiştirilemez. Evrak silmek fiziksel dosyayı otomatik silmez.
- Sağlık yardımı statüsünün seçenekleri kaynakta tanımlanmadığından keyfî bir enum oluşturulmadı.
- Adres boş olamaz; mahalle/sokak/bina/daire/ilçe/il bileşenlerinin gerçek anlamda tamlığı uygulamada denetlenir.

Testi yeniden çalıştırmak için, yalnızca geliştirme MySQL bağlantısı kullanın:

```bash
python -m pip install -r tests/requirements.txt
python tests/test_database.py
```

Bağlantı ortam değişkenleri `PROLAB_DB_HOST`, `PROLAB_DB_PORT`, `PROLAB_DB_USER`, `PROLAB_DB_PASSWORD`; yerel Unix socket için `PROLAB_DB_SOCKET` kullanılabilir. Varsayılan host `127.0.0.1`, port `3306`, kullanıcı `root`tur. Parolayı komut dosyasına yazmayın. Test, kendi rastgele adlı geçici veritabanını oluşturup kaldırır; `prolab3_vize`ye yazmaz. Sonuçlar `TEST_SONUCLARI.json` içindedir.

MySQL teknik başvuru kaynakları:

- [CHECK kısıtları](https://dev.mysql.com/doc/refman/8.4/en/create-table-check-constraints.html)
- [Yabancı anahtarlar](https://dev.mysql.com/doc/refman/8.4/en/create-table-foreign-keys.html)
- [Kilitlemeli okumalar](https://dev.mysql.com/doc/refman/8.4/en/innodb-locking-reads.html)

Bu belge kurulum ve ekip entegrasyonu notudur. Projenin istenen IEEE teslim raporu yerine hazırlanmış bir rapor değildir.
