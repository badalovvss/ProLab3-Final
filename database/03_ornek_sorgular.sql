-- Bu dosyanın tamamını topluca çalıştırmayın; ilgili bölümü seçin.
-- @... değerleri backend'in doğruladığı bilgilerden gelir.
-- Web uygulamasında SQL metni birleştirmeyin, parametreli sorgu kullanın.
USE prolab3_vize;
SET NAMES utf8mb4;
SET SESSION time_zone = '+00:00';

-- 1) İlk kurulumun özeti (beklenen: 6 kullanıcı, diğer tablolar 0).
SELECT 'KULLANICI' AS tablo, COUNT(*) AS kayit FROM KULLANICI
UNION ALL SELECT 'EVRAK', COUNT(*) FROM EVRAK
UNION ALL SELECT 'RAPOR', COUNT(*) FROM RAPOR
UNION ALL SELECT 'GIRIS_DENEMESI', COUNT(*) FROM GIRIS_DENEMESI
UNION ALL SELECT 'OTURUM', COUNT(*) FROM OTURUM;

-- 2) Giriş için kayıt bulma. Hash yalnızca backend içinde okunur.
SET @giris_bilgisi = 'ogrenci01';
SELECT kullanici_id, kullanici_adi, sifre_hash, rol, aktif, kilitli
FROM KULLANICI
WHERE (LOCATE('@', @giris_bilgisi) = 0 AND kullanici_adi = TRIM(@giris_bilgisi))
   OR (LOCATE('@', @giris_bilgisi) > 0 AND eposta = TRIM(@giris_bilgisi));

-- 3) Her korumalı istekte geçerli oturum ve GÜNCEL kullanıcı rolünü kontrol edin.
-- @token_hash = backend'in çerezdeki rastgele token için hesapladığı SHA-256.
-- Eşleşme yoksa oturumu kabul etmeyin. Yetkisiz role HTTP 403 backend'de verilir.
SELECT k.kullanici_id, k.ad_soyad, k.rol
FROM OTURUM o JOIN KULLANICI k ON k.kullanici_id = o.kullanici_id
WHERE o.token_hash = @token_hash
  AND o.iptal_tarihi IS NULL AND o.son_kullanma_tarihi > UTC_TIMESTAMP(6)
  AND k.aktif = TRUE AND k.kilitli = FALSE AND k.rol = o.rol;

-- 4) Öğrenci yalnızca kendi evraklarını görür. Rol ve sahiplik aynı sorgudadır.
SELECT e.evrak_id, e.dosya_adi, e.format, e.statu, e.yuklenme_tarihi
FROM EVRAK e
JOIN KULLANICI k ON k.kullanici_id = e.kullanici_id
JOIN OTURUM o ON o.kullanici_id = k.kullanici_id
WHERE o.token_hash = @token_hash
  AND o.iptal_tarihi IS NULL AND o.son_kullanma_tarihi > UTC_TIMESTAMP(6)
  AND k.aktif = TRUE AND k.kilitli = FALSE
  AND k.rol = 'ROLE_STUDENT' AND o.rol = k.rol
ORDER BY e.yuklenme_tarihi DESC;

-- 5) Evrak metaverisini güvenli sahiplikle ekleme ŞABLONU.
-- Önce backend dosyanın imzasını/içeriğini denetler, EK-16 yükleme bağlamını
-- ve uzantı/MIME eşleşmesini doğrular, dosyayı benzersiz sunucu yoluna kaydeder.
-- Aşağıdaki değişkenlerin hepsini backend doldurmalıdır; bu blok yorumdadır.
/*
INSERT INTO EVRAK (kullanici_id, dosya_yolu, format, dosya_adi, mime_turu, dosya_boyutu)
SELECT k.kullanici_id, @dosya_yolu, @format, @dosya_adi, @mime_turu, @dosya_boyutu
FROM KULLANICI k JOIN OTURUM o ON o.kullanici_id = k.kullanici_id
WHERE o.token_hash = @token_hash
  AND o.iptal_tarihi IS NULL AND o.son_kullanma_tarihi > UTC_TIMESTAMP(6)
  AND k.aktif = TRUE AND k.kilitli = FALSE
  AND k.rol = 'ROLE_STUDENT' AND o.rol = k.rol;
-- Etkilenen satır tam 1 olmalı. 0 ise yükleme başarılı gösterilmez.
-- SQL başarısız olursa backend yeni kaydedilen dosyayı temizler.
*/

-- 6) Test veri seti içe aktarma sözleşmesi:
-- a) 3. sorguyla geçerli ROLE_ADMIN oturumunu doğrula.
-- b) JSON'u ayrıştır, her satırdaki tüm alanları doğrula.
-- c) Şifreyi backend'de üret + bcrypt hashle; ham parola veritabanına girmez.
-- d) Tek transaction'da parametreli INSERT'lerle bütün öğrencileri ekle.
-- e) Tek satır bile geçersiz/tekrarlıysa ROLLBACK; başarılıysa COMMIT.
--    02_test_verisi.sql bunun hazır SQL veri setidir. JSON alternatif girdidir;
--    aynı öğrencileri hem seed SQL hem JSON ile tekrar eklemeyin.
/*
START TRANSACTION;
INSERT INTO KULLANICI (
    kullanici_adi, ad_soyad, eposta, telefon_no, sifre_hash, rol,
    tc_kimlik, uyruk, ogrenci_no, dogum_tarihi, ikametgah_adresi,
    bolum, teslim_yili, onceki_staj_gun_sayisi, saglik_yardimi_statusu
) VALUES (
    ?, ?, ?, ?, ?, 'ROLE_STUDENT', ?, ?, ?, ?, ?, ?, ?, ?, ?
);
-- Her öğrenci için aynı prepared statement; hata yoksa:
COMMIT;
*/

-- 7) Giriş sonuçları (yalnızca backend/DB yöneticisi incelemesi; web raporu değil).
SELECT deneme_id, kullanici_id, giris_bilgisi, sonuc, deneme_tarihi
FROM GIRIS_DENEMESI ORDER BY deneme_id DESC LIMIT 20;

-- 8) Veri sözlüğü ve ilişkiler; salt okunur.
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_KEY
FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = 'prolab3_vize'
ORDER BY TABLE_NAME, ORDINAL_POSITION;
SELECT TABLE_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'prolab3_vize' AND REFERENCED_TABLE_NAME IS NOT NULL;
