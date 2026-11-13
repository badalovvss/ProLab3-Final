-- Yalnızca demo/test içindir; gerçek öğrenci verisi içermez.
-- İlk kurulumda 01_sema.sql dosyasından SONRA bir kez çalıştırın.
-- Admin: admin_demo / AdminDemo!2026
-- Öğrenciler: ogrenci01 ... ogrenci05 / VizeDemo!2026
-- Parolalar tabloda yalnızca benzersiz tuzlu bcrypt (maliyet 12) olarak tutulur.
-- Bu yorumdaki demo parolalarını gerçek sisteme taşımayın.
SET NAMES utf8mb4;
SET SESSION time_zone = '+00:00';
USE prolab3_vize;
START TRANSACTION;
INSERT INTO KULLANICI (kullanici_adi, ad_soyad, eposta, telefon_no, sifre_hash, rol, tc_kimlik, uyruk, ogrenci_no, dogum_tarihi, ikametgah_adresi, bolum, teslim_yili, onceki_staj_gun_sayisi, saglik_yardimi_statusu)
VALUES
    ('admin_demo', 'Deneme Yöneticisi', 'admin@example.invalid', '+90 000 000 00 00', '$2b$12$K72f.lfpkCFiEsOg7FUhy.q39ww5KvJE9Bmgv.D7FEEC0nOjVwF3S', 'ROLE_ADMIN', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('ogrenci01', 'Deneme Öğrenci 1', 'ogrenci01@example.invalid', '+90 000 000 00 01', '$2b$12$bvj2L1iArLePnbxvlBwoKuNJO2B4S/vOI4eCkqsczlCUrSrwaqKOu', 'ROLE_STUDENT', '10000000001', 'T.C.', '026000001', '2002-02-15', 'Deneme Mahallesi, Örnek Sokak, Bina No: 1, Daire No: 1, İzmit/Kocaeli', 'Yazılım Mühendisliği', '2025-2026', 0, 'Kendi sigortasından yararlanıyor'),
    ('ogrenci02', 'Deneme Öğrenci 2', 'ogrenci02@example.invalid', '+90 000 000 00 02', '$2b$12$MgS7ZeepJLTW/LjRnHlmvOncGyyqGXQcO2stmzhgJJ78nEWzJ5PTC', 'ROLE_STUDENT', '10000000002', 'T.C.', '026000002', '2003-03-15', 'Deneme Mahallesi, Örnek Sokak, Bina No: 2, Daire No: 1, İzmit/Kocaeli', 'Yazılım Mühendisliği', '2026-2027', 10, 'Sağlık yardımı almıyor'),
    ('ogrenci03', 'Deneme Öğrenci 3', 'ogrenci03@example.invalid', '+90 000 000 00 03', '$2b$12$9acjDjEdJvBZhxaw9uzL.ONxK5/4HNff09kvKBs//CwSB7fh2VziW', 'ROLE_STUDENT', '10000000003', 'T.C.', '026000003', '2004-04-15', 'Deneme Mahallesi, Örnek Sokak, Bina No: 3, Daire No: 1, İzmit/Kocaeli', 'Yazılım Mühendisliği', '2026-2027', 20, 'Ailesi üzerinden yararlanıyor'),
    ('ogrenci04', 'Deneme Öğrenci 4', 'ogrenci04@example.invalid', '+90 000 000 00 04', '$2b$12$vZAgCDQIP7vlUNwZpUdjPe7Iy86N.tn.DdWwwZLtTQuuqzHZoQtN.', 'ROLE_STUDENT', '99000000004', 'Almanya', '026000004', '2001-05-15', 'Deneme Mahallesi, Örnek Sokak, Bina No: 4, Daire No: 1, İzmit/Kocaeli', 'Yazılım Mühendisliği', '2026-2027', 0, 'Kendi sigortasından yararlanıyor'),
    ('ogrenci05', 'Deneme Öğrenci 5', 'ogrenci05@example.invalid', '+90 000 000 00 05', '$2b$12$iX.l/2J5KwaStoUNPug8SO2D3PqsIuwexpW2V0Jk0EyD59JFOk9PO', 'ROLE_STUDENT', '99000000005', 'Azerbaycan', '026000005', '2002-06-15', 'Deneme Mahallesi, Örnek Sokak, Bina No: 5, Daire No: 1, İzmit/Kocaeli', 'Yazılım Mühendisliği', '2026-2027', 15, 'Sağlık yardımı almıyor');
COMMIT;

-- EVRAK, RAPOR, OTURUM ve GIRIS_DENEMESI başlangıçta boştur.
