-- PROLAB III / Staj Evrak Doğrulama ve Yönetim Sistemi
-- Yalnızca VİZE veritabanı. MySQL 8.0.16+ / 8.4; MariaDB için hazırlanmadı.
-- İlk kurulum: yeni ve boş bir veritabanı oluşturur; mevcut veritabanını SİLMEZ.
-- Veritabanı zaten varsa DURUN. Bu dosya bir şema güncelleme betiği değildir.
SET NAMES utf8mb4 COLLATE utf8mb4_turkish_ci;
SET SESSION time_zone = '+00:00';
SET SESSION sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

CREATE DATABASE prolab3_vize CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci;
USE prolab3_vize;

CREATE TABLE KULLANICI (
    kullanici_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_adi VARCHAR(50) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
    ad_soyad VARCHAR(150) NOT NULL,
    eposta VARCHAR(254) COLLATE utf8mb4_0900_as_ci NOT NULL,
    telefon_no VARCHAR(17) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    sifre_hash VARCHAR(255) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    rol VARCHAR(20) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    tc_kimlik VARCHAR(11) CHARACTER SET ascii COLLATE ascii_bin NULL,
    uyruk VARCHAR(80) NULL,
    ogrenci_no VARCHAR(30) CHARACTER SET ascii COLLATE ascii_bin NULL,
    dogum_tarihi DATE NULL,
    ikametgah_adresi VARCHAR(500) NULL,
    bolum VARCHAR(150) NULL,
    teslim_yili CHAR(9) CHARACTER SET ascii COLLATE ascii_bin NULL,
    onceki_staj_gun_sayisi SMALLINT UNSIGNED NULL,
    saglik_yardimi_statusu VARCHAR(100) NULL,
    basarisiz_giris_sayisi TINYINT UNSIGNED NOT NULL DEFAULT 0,
    kilitli BOOLEAN NOT NULL DEFAULT FALSE,
    aktif BOOLEAN NOT NULL DEFAULT TRUE,
    olusturulma_tarihi DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (kullanici_id),
    UNIQUE KEY uq_kullanici_adi (kullanici_adi),
    UNIQUE KEY uq_kullanici_eposta (eposta),
    UNIQUE KEY uq_kullanici_tc (tc_kimlik),
    UNIQUE KEY uq_kullanici_ogrenci_no (ogrenci_no),
    CONSTRAINT ck_kullanici_adi CHECK (REGEXP_LIKE(kullanici_adi, '^[A-Za-z0-9_.-]{3,50}$', 'c')),
    CONSTRAINT ck_ad_soyad CHECK (CHAR_LENGTH(TRIM(ad_soyad)) > 0),
    CONSTRAINT ck_eposta CHECK (REGEXP_LIKE(eposta, '^[^[:space:]@]+@[^[:space:]@.]+([.][^[:space:]@.]+)+$', 'c')),
    CONSTRAINT ck_telefon CHECK (REGEXP_LIKE(telefon_no, '^[+]90 [0-9]{3} [0-9]{3} [0-9]{2} [0-9]{2}$', 'c')),
    -- Hash uygulamada bcrypt ile üretilir. SQL şifre doğrulaması yapmaz.
    CONSTRAINT ck_sifre_hash CHECK (REGEXP_LIKE(sifre_hash, '^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$', 'c')),
    CONSTRAINT ck_rol CHECK (rol IN ('ROLE_STUDENT', 'ROLE_ADMIN')),
    CONSTRAINT ck_kimlik_uyruk CHECK (
        (tc_kimlik IS NULL AND uyruk IS NULL) OR
        (tc_kimlik IS NOT NULL AND uyruk IS NOT NULL AND
         CHAR_LENGTH(TRIM(uyruk)) > 0 AND
         ((uyruk = 'T.C.' AND REGEXP_LIKE(tc_kimlik, '^[1-9][0-9]{10}$', 'c')) OR
          (uyruk <> 'T.C.' AND REGEXP_LIKE(tc_kimlik, '^99[0-9]{9}$', 'c'))))
    ),
    CONSTRAINT ck_ogrenci_no CHECK (ogrenci_no IS NULL OR REGEXP_LIKE(ogrenci_no, '^[0-9]+$', 'c')),
    CONSTRAINT ck_teslim_yili CHECK (
        teslim_yili IS NULL OR
        (REGEXP_LIKE(teslim_yili, '^[0-9]{4}-[0-9]{4}$', 'c') AND
         CAST(RIGHT(teslim_yili, 4) AS UNSIGNED) = CAST(LEFT(teslim_yili, 4) AS UNSIGNED) + 1)
    ),
    CONSTRAINT ck_ogrenci_zorunlu_alanlar CHECK (
        rol <> 'ROLE_STUDENT' OR
        (tc_kimlik IS NOT NULL AND uyruk IS NOT NULL AND ogrenci_no IS NOT NULL AND
         dogum_tarihi IS NOT NULL AND ikametgah_adresi IS NOT NULL AND
         CHAR_LENGTH(TRIM(ikametgah_adresi)) > 0 AND bolum IS NOT NULL AND
         CHAR_LENGTH(TRIM(bolum)) > 0 AND teslim_yili IS NOT NULL AND
         onceki_staj_gun_sayisi IS NOT NULL AND saglik_yardimi_statusu IS NOT NULL AND
         CHAR_LENGTH(TRIM(saglik_yardimi_statusu)) > 0)
    ),
    CONSTRAINT ck_kilit_sayac CHECK (
        (basarisiz_giris_sayisi < 5 AND kilitli = 0) OR
        (basarisiz_giris_sayisi = 5 AND kilitli = 1)
    ),
    CONSTRAINT ck_kullanici_aktif CHECK (aktif IN (0, 1))
) ENGINE=InnoDB COMMENT='Kullanici, Ogrenci ve Admin siniflarinin tek tabloda eslenmesi';

CREATE TABLE EVRAK (
    evrak_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id INT UNSIGNED NOT NULL,
    dosya_yolu VARCHAR(512) COLLATE utf8mb4_bin NOT NULL,
    format VARCHAR(4) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    statu VARCHAR(30) COLLATE utf8mb4_bin NOT NULL DEFAULT 'Yüklendi',
    yuklenme_tarihi DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    google_form_url VARCHAR(2048) NULL,
    belge_turu VARCHAR(5) CHARACTER SET ascii COLLATE ascii_bin NOT NULL DEFAULT 'EK-16',
    dosya_adi VARCHAR(255) NOT NULL,
    mime_turu VARCHAR(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    dosya_boyutu BIGINT UNSIGNED NOT NULL,
    PRIMARY KEY (evrak_id),
    UNIQUE KEY uq_evrak_dosya_yolu (dosya_yolu),
    KEY ix_evrak_kullanici_tarih (kullanici_id, yuklenme_tarihi),
    CONSTRAINT fk_evrak_kullanici FOREIGN KEY (kullanici_id)
        REFERENCES KULLANICI (kullanici_id) ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT ck_evrak_turu CHECK (belge_turu = 'EK-16'),
    CONSTRAINT ck_evrak_format CHECK (format IN ('doc', 'docx', 'pdf', 'jpg', 'jpeg', 'png')),
    CONSTRAINT ck_evrak_mime CHECK (
        (format = 'doc' AND mime_turu = 'application/msword') OR
        (format = 'docx' AND mime_turu = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document') OR
        (format = 'pdf' AND mime_turu = 'application/pdf') OR
        (format IN ('jpg', 'jpeg') AND mime_turu = 'image/jpeg') OR
        (format = 'png' AND mime_turu = 'image/png')
    ),
    CONSTRAINT ck_evrak_dosya_yolu CHECK (
        CHAR_LENGTH(TRIM(dosya_yolu)) > 0 AND LOCATE('.', dosya_yolu) > 0 AND
        LOWER(SUBSTRING_INDEX(dosya_yolu, '.', -1)) = format
    ),
    CONSTRAINT ck_evrak_dosya_adi CHECK (CHAR_LENGTH(TRIM(dosya_adi)) > 0),
    CONSTRAINT ck_evrak_boyut CHECK (dosya_boyutu > 0),
    -- Vize sınırı: OCR, onay, tamamlanma ve Google Form işlemleri çalışmaz.
    CONSTRAINT ck_evrak_vize_statu CHECK (statu = 'Yüklendi'),
    CONSTRAINT ck_evrak_vize_google CHECK (google_form_url IS NULL)
) ENGINE=InnoDB COMMENT='Yalnizca EK-16 dosyalarinin metaverisi; dosya baytlari dis depolamada';

CREATE TABLE RAPOR (
    rapor_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    evrak_id INT UNSIGNED NOT NULL,
    eksik_alanlar TEXT NULL,
    ocr_ham_veri LONGTEXT NULL,
    olusturulma_tarihi DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (rapor_id),
    UNIQUE KEY uq_rapor_evrak (evrak_id),
    CONSTRAINT fk_rapor_evrak FOREIGN KEY (evrak_id)
        REFERENCES EVRAK (evrak_id) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB COMMENT='Diyagram uyumu icin bos yapi; vizede rapor/OCR kaydi olusturulmaz';

CREATE TABLE GIRIS_DENEMESI (
    deneme_id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id INT UNSIGNED NULL,
    giris_bilgisi VARCHAR(254) NOT NULL,
    sonuc VARCHAR(15) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    ip_adresi VARCHAR(45) CHARACTER SET ascii NULL,
    deneme_tarihi DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (deneme_id),
    KEY ix_deneme_kullanici_tarih (kullanici_id, deneme_tarihi),
    CONSTRAINT fk_deneme_kullanici FOREIGN KEY (kullanici_id)
        REFERENCES KULLANICI (kullanici_id) ON DELETE SET NULL ON UPDATE RESTRICT,
    CONSTRAINT ck_deneme_sonuc CHECK (sonuc IN ('BASARILI', 'HATALI', 'KILITLI', 'PASIF', 'BULUNAMADI')),
    CONSTRAINT ck_deneme_bilgi CHECK (CHAR_LENGTH(TRIM(giris_bilgisi)) > 0)
) ENGINE=InnoDB COMMENT='Parola icermez; bilinmeyen kullanici denemeleri de loglanir';

CREATE TABLE OTURUM (
    oturum_id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id INT UNSIGNED NOT NULL,
    token_hash CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    rol VARCHAR(20) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    olusturulma_tarihi DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    son_kullanma_tarihi DATETIME(6) NOT NULL,
    iptal_tarihi DATETIME(6) NULL,
    PRIMARY KEY (oturum_id),
    UNIQUE KEY uq_oturum_token (token_hash),
    KEY ix_oturum_kullanici (kullanici_id),
    CONSTRAINT fk_oturum_kullanici FOREIGN KEY (kullanici_id)
        REFERENCES KULLANICI (kullanici_id) ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT ck_oturum_hash CHECK (REGEXP_LIKE(token_hash, '^[0-9a-f]{64}$', 'c')),
    CONSTRAINT ck_oturum_rol CHECK (rol IN ('ROLE_STUDENT', 'ROLE_ADMIN')),
    CONSTRAINT ck_oturum_sure CHECK (son_kullanma_tarihi > olusturulma_tarihi),
    CONSTRAINT ck_oturum_iptal CHECK (iptal_tarihi IS NULL OR iptal_tarihi >= olusturulma_tarihi)
) ENGINE=InnoDB COMMENT='Sunucu tarafli oturum; ham token degil SHA-256 ozeti saklanir';

DELIMITER $$

CREATE TRIGGER trg_kullanici_bi BEFORE INSERT ON KULLANICI FOR EACH ROW
BEGIN
    IF NEW.dogum_tarihi IS NOT NULL AND NEW.dogum_tarihi > UTC_DATE() THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Dogum tarihi gelecekte olamaz.';
    END IF;
END$$

CREATE TRIGGER trg_kullanici_bu BEFORE UPDATE ON KULLANICI FOR EACH ROW
BEGIN
    IF NEW.dogum_tarihi IS NOT NULL AND NEW.dogum_tarihi > UTC_DATE() THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Dogum tarihi gelecekte olamaz.';
    END IF;
    IF NEW.rol <> 'ROLE_STUDENT' AND EXISTS (
        SELECT 1 FROM EVRAK WHERE kullanici_id = OLD.kullanici_id
    ) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Evrak sahibi kullanici ogrenci rolunde kalmalidir.';
    END IF;
END$$

CREATE TRIGGER trg_evrak_bi BEFORE INSERT ON EVRAK FOR EACH ROW
BEGIN
    DECLARE v_rol VARCHAR(20) DEFAULT NULL;
    SELECT rol INTO v_rol FROM KULLANICI WHERE kullanici_id = NEW.kullanici_id FOR SHARE;
    IF v_rol IS NULL OR v_rol <> 'ROLE_STUDENT' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Evrak yalnizca mevcut bir ogrenciye ait olabilir.';
    END IF;
END$$

CREATE TRIGGER trg_evrak_bu BEFORE UPDATE ON EVRAK FOR EACH ROW
BEGIN
    IF NEW.kullanici_id <> OLD.kullanici_id THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Evrak sahibi sonradan degistirilemez.';
    END IF;
END$$

CREATE TRIGGER trg_rapor_vize BEFORE INSERT ON RAPOR FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Rapor ve OCR kaydi final kapsamindadir; vizede olusturulamaz.';
END$$

-- YALNIZCA güvenilir backend çağırır. p_sifre_dogru, backend bcrypt doğrulamasıdır.
-- Tarayıcıdan gelen bir boolean değer ASLA doğrudan buraya aktarılmaz.
-- Bu prosedür kendi transaction'ını yönetir; başka transaction içine koymayın.
CREATE PROCEDURE sp_giris_sonucu_kaydet (
    IN p_giris_bilgisi VARCHAR(254) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci,
    IN p_sifre_dogru BOOLEAN,
    IN p_ip_adresi VARCHAR(45),
    IN p_token_hash CHAR(64),
    IN p_son_kullanma DATETIME(6),
    OUT p_giris_kabul BOOLEAN,
    OUT p_oturum_id BIGINT UNSIGNED
)
SQL SECURITY INVOKER
BEGIN
    DECLARE v_id INT UNSIGNED DEFAULT NULL;
    DECLARE v_sayac TINYINT UNSIGNED;
    DECLARE v_kilitli BOOLEAN;
    DECLARE v_aktif BOOLEAN;
    DECLARE v_rol VARCHAR(20);
    DECLARE v_sonuc VARCHAR(15);
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_id = NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET p_giris_kabul = FALSE, p_oturum_id = NULL;
    IF p_sifre_dogru IS NULL OR p_sifre_dogru NOT IN (0, 1) OR
       p_giris_bilgisi IS NULL OR CHAR_LENGTH(TRIM(p_giris_bilgisi)) = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Giris parametreleri eksik veya gecersiz.';
    END IF;

    START TRANSACTION;
    -- Kullanıcı adı @ içeremez; ad/e-posta ad alanları birbirine karışmaz.
    IF LOCATE('@', p_giris_bilgisi) > 0 THEN
        SELECT kullanici_id, basarisiz_giris_sayisi, kilitli, aktif, rol
        INTO v_id, v_sayac, v_kilitli, v_aktif, v_rol
        FROM KULLANICI WHERE eposta = TRIM(p_giris_bilgisi) FOR UPDATE;
    ELSE
        SELECT kullanici_id, basarisiz_giris_sayisi, kilitli, aktif, rol
        INTO v_id, v_sayac, v_kilitli, v_aktif, v_rol
        FROM KULLANICI WHERE kullanici_adi = TRIM(p_giris_bilgisi) FOR UPDATE;
    END IF;

    IF v_id IS NULL THEN
        SET v_sonuc = 'BULUNAMADI';
    ELSEIF v_aktif = FALSE THEN
        SET v_sonuc = 'PASIF';
    ELSEIF v_kilitli = TRUE THEN
        SET v_sonuc = 'KILITLI';
    ELSEIF p_sifre_dogru = FALSE THEN
        SET v_sayac = v_sayac + 1;
        UPDATE KULLANICI SET basarisiz_giris_sayisi = v_sayac,
            kilitli = (v_sayac >= 5) WHERE kullanici_id = v_id;
        SET v_sonuc = 'HATALI';
    ELSE
        IF p_token_hash IS NULL OR p_son_kullanma IS NULL OR p_son_kullanma <= UTC_TIMESTAMP(6) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Basarili giris icin token ozeti ve gelecek bitis zamani gerekir.';
        END IF;
        UPDATE KULLANICI SET basarisiz_giris_sayisi = 0, kilitli = FALSE WHERE kullanici_id = v_id;
        INSERT INTO OTURUM (kullanici_id, token_hash, rol, olusturulma_tarihi, son_kullanma_tarihi)
        VALUES (v_id, p_token_hash, v_rol, UTC_TIMESTAMP(6), p_son_kullanma);
        SET p_oturum_id = LAST_INSERT_ID(), p_giris_kabul = TRUE, v_sonuc = 'BASARILI';
    END IF;

    INSERT INTO GIRIS_DENEMESI (kullanici_id, giris_bilgisi, sonuc, ip_adresi, deneme_tarihi)
    VALUES (v_id, TRIM(p_giris_bilgisi), v_sonuc, p_ip_adresi, UTC_TIMESTAMP(6));
    COMMIT;
END$$

CREATE PROCEDURE sp_cikis_yap (IN p_token_hash CHAR(64))
SQL SECURITY INVOKER
BEGIN
    UPDATE OTURUM SET iptal_tarihi = UTC_TIMESTAMP(6)
    WHERE token_hash = p_token_hash AND iptal_tarihi IS NULL;
END$$

-- Süreli otomatik kilit açma şartnamede yoktur. Geçerli admin oturumu ile açılır.
CREATE PROCEDURE sp_hesap_kilidini_ac (
    IN p_admin_token_hash CHAR(64), IN p_hedef_kullanici_id INT UNSIGNED
)
SQL SECURITY INVOKER
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM OTURUM o JOIN KULLANICI k ON k.kullanici_id = o.kullanici_id
        WHERE o.token_hash = p_admin_token_hash AND o.rol = 'ROLE_ADMIN' AND k.rol = o.rol
          AND o.iptal_tarihi IS NULL AND o.son_kullanma_tarihi > UTC_TIMESTAMP(6)
          AND k.aktif = TRUE AND k.kilitli = FALSE
    ) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Gecerli admin oturumu gerekli.';
    END IF;
    -- Eski oturumlar kilit açılınca tekrar kullanılamasın.
    START TRANSACTION;
    BEGIN
        DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END;
        UPDATE KULLANICI SET basarisiz_giris_sayisi = 0, kilitli = FALSE
        WHERE kullanici_id = p_hedef_kullanici_id;
        UPDATE OTURUM SET iptal_tarihi = UTC_TIMESTAMP(6)
        WHERE kullanici_id = p_hedef_kullanici_id AND iptal_tarihi IS NULL;
        COMMIT;
    END;
END$$

DELIMITER ;

-- Bağlantı havuzundaki HER bağlantıda UTC ve strict SQL mode ayarlanmalıdır.
-- HTTP 403, bcrypt doğrulama, dosya imzası kontrolü ve admin veri seti yetkisi backend'e aittir.
