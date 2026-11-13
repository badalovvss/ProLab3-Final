"""Gerçek MySQL entegrasyon testleri; yalnızca kendi geçici şemasını değiştirir."""
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timedelta, timezone
from pathlib import Path
import hashlib
import json
import os
import secrets
import unittest
import uuid

import bcrypt
import pymysql

ROOT = Path(__file__).resolve().parents[1]
CONFIG = dict(host=os.getenv('PROLAB_DB_HOST', '127.0.0.1'),
              port=int(os.getenv('PROLAB_DB_PORT', '3306')),
              user=os.getenv('PROLAB_DB_USER', 'root'),
              password=os.getenv('PROLAB_DB_PASSWORD', ''),
              charset='utf8mb4', autocommit=True)
if os.getenv('PROLAB_DB_SOCKET'):
    CONFIG['unix_socket'] = os.environ['PROLAB_DB_SOCKET']


def execute_script(conn, text):
    """Paketin mysql-client DELIMITER ifadelerini MySQL'e tek tek yollar."""
    delimiter, pending = ';', []
    for line in text.splitlines():
        if not line.strip() or line.lstrip().startswith('--'):
            continue
        if line.strip().upper().startswith('DELIMITER '):
            if pending:
                raise ValueError('Eksik SQL ifadesi')
            delimiter = line.strip().split()[1]
            continue
        pending.append(line)
        if line.rstrip().endswith(delimiter):
            statement = '\n'.join(pending).rstrip()[:-len(delimiter)]
            with conn.cursor() as cursor:
                cursor.execute(statement)
            pending.clear()
    if pending:
        raise ValueError('SQL sonunda eksik ayraç')


def scalar(conn, sql, params=()):
    with conn.cursor() as c:
        c.execute(sql, params)
        return c.fetchone()[0]


def login(conn, name, correct=False, token=None, expiry=None):
    token = token or hashlib.sha256(secrets.token_bytes(32)).hexdigest()
    expiry = expiry or datetime.now(timezone.utc).replace(tzinfo=None) + timedelta(hours=1)
    with conn.cursor() as c:
        c.execute('CALL sp_giris_sonucu_kaydet(%s,%s,%s,%s,%s,@ok,@sid)',
                  (name, correct, '127.0.0.1', token if correct else None, expiry if correct else None))
        while c.nextset():
            pass
        c.execute('SELECT @ok, @sid')
        ok, sid = c.fetchone()
    return ok, sid, token


VALID_SESSION = """SELECT COUNT(*) FROM OTURUM o JOIN KULLANICI k USING(kullanici_id)
WHERE o.token_hash=%s AND o.iptal_tarihi IS NULL
AND o.son_kullanma_tarihi>UTC_TIMESTAMP(6) AND k.aktif=1 AND k.kilitli=0 AND o.rol=k.rol"""


class DatabaseTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.db = 'prolab3_vize_test_' + uuid.uuid4().hex[:12]
        cls.conn = pymysql.connect(**CONFIG)
        cls.version = scalar(cls.conn, 'SELECT VERSION()')
        try:
            for name in ['01_sema.sql', '02_test_verisi.sql']:
                source = (ROOT / name).read_text(encoding='utf-8')
                source = source.replace('CREATE DATABASE prolab3_vize ', f'CREATE DATABASE `{cls.db}` ')
                source = source.replace('USE prolab3_vize;', f'USE `{cls.db}`;')
                execute_script(cls.conn, source)
            cls.student = scalar(cls.conn, "SELECT kullanici_id FROM KULLANICI WHERE kullanici_adi='ogrenci01'")
            cls.admin = scalar(cls.conn, "SELECT kullanici_id FROM KULLANICI WHERE kullanici_adi='admin_demo'")
        except Exception:
            with cls.conn.cursor() as c:
                c.execute(f'DROP DATABASE IF EXISTS `{cls.db}`')
            cls.conn.close()
            raise

    @classmethod
    def tearDownClass(cls):
        cls.conn.rollback()
        with cls.conn.cursor() as c:
            c.execute(f'DROP DATABASE `{cls.db}`')
        cls.conn.close()

    def setUp(self):
        self.conn.rollback()
        with self.conn.cursor() as c:
            for table in ['RAPOR', 'EVRAK', 'GIRIS_DENEMESI', 'OTURUM']:
                c.execute(f'DELETE FROM {table}')
            c.execute('UPDATE KULLANICI SET basarisiz_giris_sayisi=0,kilitli=0,aktif=1')
        self.conn.begin()

    def tearDown(self):
        self.conn.rollback()

    def sql(self, sql, params=()):
        with self.conn.cursor() as c:
            c.execute(sql, params)
            return c.fetchall()

    def reject(self, sql, params=(), codes=(3819, 1644, 1062, 1048, 1406, 1264, 1292, 1451, 1452)):
        with self.assertRaises(pymysql.MySQLError) as failure:
            self.sql(sql, params)
        self.assertIn(failure.exception.args[0], codes, str(failure.exception))

    def upload(self, ext='pdf', mime='application/pdf', owner=None, suffix=None):
        path = f'test/{suffix or uuid.uuid4().hex}.{ext}'
        self.sql('INSERT INTO EVRAK(kullanici_id,dosya_yolu,format,dosya_adi,mime_turu,dosya_boyutu) '
                 'VALUES(%s,%s,%s,%s,%s,%s)',
                 (owner or self.student, path, ext, 'test.' + ext, mime, 128))
        return scalar(self.conn, 'SELECT LAST_INSERT_ID()')

    def test_01_schema_and_seed(self):
        """Beş tablo, altı kullanıcı ve tam öğrenci alanları oluşturulur."""
        tables = {r[0] for r in self.sql('SHOW TABLES')}
        self.assertEqual(tables, {'KULLANICI','EVRAK','RAPOR','GIRIS_DENEMESI','OTURUM'})
        self.assertEqual(scalar(self.conn, 'SELECT COUNT(*) FROM KULLANICI'), 6)
        self.assertEqual(scalar(self.conn, "SELECT COUNT(*) FROM KULLANICI WHERE rol='ROLE_STUDENT'"), 5)
        columns = {r[0] for r in self.sql('SHOW COLUMNS FROM KULLANICI')}
        self.assertTrue(set(json.loads((ROOT/'ogrenci_test_verisi.json').read_text())[0]) <= columns)

    def test_02_hashes(self):
        """Demo parolalarının gerçek bcrypt özetleri doğrulanır; tuzlar farklıdır."""
        hashes = []
        for role, value in self.sql('SELECT rol,sifre_hash FROM KULLANICI'):
            pw = b'AdminDemo!2026' if role == 'ROLE_ADMIN' else b'VizeDemo!2026'
            self.assertTrue(bcrypt.checkpw(pw, value.encode()))
            self.assertFalse(bcrypt.checkpw(b'yanlis', value.encode()))
            hashes.append(value)
        self.assertEqual(len(set(hashes)), 6)

    def test_03_unique_keys(self):
        """Kimlik, öğrenci no, e-posta ve kullanıcı adı tekrarları reddedilir."""
        for col,value in [('tc_kimlik','10000000002'),('ogrenci_no','026000002'),
                          ('eposta','OGRENCI02@example.invalid'),('kullanici_adi','OGRENCI02')]:
            with self.subTest(column=col):
                self.reject(f'UPDATE KULLANICI SET {col}=%s WHERE kullanici_id=%s', (value,self.student), (1062,))

    def test_04_required_student_fields(self):
        """Öğrenci veri setinin zorunlu alanları NULL olamaz."""
        columns = ['ad_soyad','eposta','telefon_no','tc_kimlik','uyruk','ogrenci_no','dogum_tarihi',
                   'ikametgah_adresi','bolum','teslim_yili','onceki_staj_gun_sayisi','saglik_yardimi_statusu']
        for col in columns:
            with self.subTest(column=col):
                self.reject(f'UPDATE KULLANICI SET {col}=NULL WHERE kullanici_id=%s',(self.student,),(3819,1048))

    def test_05_admin_nullable_student_fields(self):
        """Admin için sahte öğrenci/kimlik bilgisi zorunlu değildir."""
        self.assertEqual(self.sql('SELECT tc_kimlik,ogrenci_no,teslim_yili FROM KULLANICI WHERE kullanici_id=%s',
                                  (self.admin,)), ((None,None,None),))

    def test_06_identity_format(self):
        """T.C. ve yabancı kimlik biçimleri uyrukla birlikte denetlenir."""
        for country, number in [('T.C.','00000000001'),('T.C.','10000x00001'),('T.C.','123'),('Almanya','10000000009')]:
            with self.subTest(country=country, number=number):
                self.reject('UPDATE KULLANICI SET uyruk=%s,tc_kimlik=%s WHERE kullanici_id=%s',
                            (country,number,self.student),(3819,))
        self.assertEqual(scalar(self.conn,"SELECT COUNT(*) FROM KULLANICI WHERE uyruk<>'T.C.'"),2)

    def test_07_leading_zero(self):
        """Öğrenci numarasındaki baştaki sıfır korunur."""
        self.assertEqual(scalar(self.conn,'SELECT ogrenci_no FROM KULLANICI WHERE kullanici_id=%s',
                                (self.student,)),'026000001')

    def test_08_phone_email(self):
        """Hatalı telefon/e-posta ve boş ad reddedilir."""
        for col,value in [('telefon_no','05001234567'),('telefon_no','+90 abc 000 00 01'),
                          ('eposta','ad@alan'),('eposta','a b@example.com'),('ad_soyad','  ')]:
            with self.subTest(column=col,value=value):
                self.reject(f'UPDATE KULLANICI SET {col}=%s WHERE kullanici_id=%s',(value,self.student),(3819,))

    def test_09_year_and_negative_days(self):
        """Ardışık olmayan öğretim yılı ve negatif staj günü reddedilir."""
        self.reject('UPDATE KULLANICI SET teslim_yili=%s WHERE kullanici_id=%s',('2025-2027',self.student),(3819,))
        self.reject('UPDATE KULLANICI SET onceki_staj_gun_sayisi=-1 WHERE kullanici_id=%s',(self.student,),(1264,))
        self.sql("UPDATE KULLANICI SET teslim_yili='2027-2028' WHERE kullanici_id=%s",(self.student,))

    def test_10_password_and_role(self):
        """Açık parola ve tanımsız rol reddedilir."""
        for col,value in [('sifre_hash','acik-parola'),('rol','SUPER_ADMIN')]:
            self.reject(f'UPDATE KULLANICI SET {col}=%s WHERE kullanici_id=%s',(value,self.student),(3819,))

    def test_11_future_birth(self):
        """Gelecekteki doğum tarihi reddedilir."""
        self.reject('UPDATE KULLANICI SET dogum_tarihi=DATE_ADD(UTC_DATE(),INTERVAL 1 DAY) WHERE kullanici_id=%s',
                    (self.student,),(1644,))

    def test_12_all_upload_formats(self):
        """Word/PDF/JPG/JPEG/PNG metaverisi kabul edilir; birden çok evrak yüklenir."""
        pairs = [('doc','application/msword'),('docx','application/vnd.openxmlformats-officedocument.wordprocessingml.document'),
                 ('pdf','application/pdf'),('jpg','image/jpeg'),('jpeg','image/jpeg'),('png','image/png')]
        for ext,mime in pairs:
            self.upload(ext,mime)
        self.assertEqual(scalar(self.conn,'SELECT COUNT(*) FROM EVRAK'),6)
        self.assertEqual(scalar(self.conn,"SELECT COUNT(*) FROM EVRAK WHERE statu='Yüklendi'"),6)

    def test_13_upload_rejections(self):
        """Uygun olmayan format, MIME, dosya yolu, boyut ve belge türü reddedilir."""
        eid = self.upload()
        for col,value in [('format','exe'),('mime_turu','image/png'),('dosya_yolu','test/yanlis.exe'),
                          ('dosya_boyutu',0),('belge_turu','EK-17')]:
            with self.subTest(column=col):
                self.reject(f'UPDATE EVRAK SET {col}=%s WHERE evrak_id=%s',(value,eid),(3819,))

    def test_14_owner(self):
        """Admin veya olmayan kullanıcı evrak sahibi olamaz."""
        for owner in [self.admin,999999]:
            self.reject('INSERT INTO EVRAK(kullanici_id,dosya_yolu,format,dosya_adi,mime_turu,dosya_boyutu) '
                        "VALUES(%s,'test/a.pdf','pdf','a.pdf','application/pdf',1)",(owner,),(1644,))

    def test_15_owner_integrity(self):
        """Evrak sahibinin silinmesi, admin yapılması ve evrak devri reddedilir."""
        eid=self.upload()
        self.reject('DELETE FROM KULLANICI WHERE kullanici_id=%s',(self.student,),(1451,))
        self.reject("UPDATE KULLANICI SET rol='ROLE_ADMIN' WHERE kullanici_id=%s",(self.student,),(1644,))
        self.reject('UPDATE EVRAK SET kullanici_id=%s WHERE evrak_id=%s',(self.admin,eid),(1644,))

    def test_16_midterm_boundary(self):
        """Final durumları, Google Form URL ve OCR raporu vizede engellenir."""
        eid=self.upload()
        for status in ['İnceleniyor','Hatalı / Eksik','Onaylandı','Tamamlandı']:
            self.reject('UPDATE EVRAK SET statu=%s WHERE evrak_id=%s',(status,eid),(3819,))
        self.reject('UPDATE EVRAK SET google_form_url=%s WHERE evrak_id=%s',('https://example.invalid/form',eid),(3819,))
        self.reject('INSERT INTO RAPOR(evrak_id) VALUES(%s)',(eid,),(1644,))

    def test_17_unknown_user_logged(self):
        """Bilinmeyen kullanıcı girişleri kullanıcı FK'sı NULL olarak loglanır."""
        ok,sid,_=login(self.conn,'olmayan_kullanici',False)
        self.assertEqual((ok,sid),(0,None))
        self.assertEqual(self.sql('SELECT kullanici_id,sonuc FROM GIRIS_DENEMESI'),((None,'BULUNAMADI'),))

    def test_18_successful_login(self):
        """Başarılı giriş rol taşıyan geçerli oturum oluşturur."""
        ok,sid,token=login(self.conn,'ogrenci01',True)
        self.assertEqual(ok,1);self.assertIsNotNone(sid)
        self.assertEqual(scalar(self.conn,VALID_SESSION,(token,)),1)
        self.assertEqual(scalar(self.conn,'SELECT rol FROM OTURUM WHERE oturum_id=%s',(sid,)),'ROLE_STUDENT')

    def test_19_success_resets_counter(self):
        """Kilitlenmeden önce doğru parola hata sayacını sıfırlar."""
        for _ in range(3):login(self.conn,'ogrenci01',False)
        self.assertEqual(scalar(self.conn,'SELECT basarisiz_giris_sayisi FROM KULLANICI WHERE kullanici_id=%s',(self.student,)),3)
        self.assertEqual(login(self.conn,'ogrenci01',True)[0],1)
        self.assertEqual(scalar(self.conn,'SELECT basarisiz_giris_sayisi FROM KULLANICI WHERE kullanici_id=%s',(self.student,)),0)

    def test_20_five_failures_lock(self):
        """Tam beş hatada kilitlenir; kilitli hesap doğru parolayla açılmaz."""
        for i in range(1,6):
            login(self.conn,'ogrenci01',False)
            self.assertEqual(self.sql('SELECT basarisiz_giris_sayisi,kilitli FROM KULLANICI WHERE kullanici_id=%s',
                                      (self.student,)),((i,int(i==5)),))
        self.assertEqual(login(self.conn,'ogrenci01',True)[0],0)
        self.assertEqual(scalar(self.conn,'SELECT COUNT(*) FROM OTURUM'),0)
        self.assertEqual(scalar(self.conn,"SELECT COUNT(*) FROM GIRIS_DENEMESI WHERE sonuc='KILITLI'"),1)

    def test_21_aliases_share_counter(self):
        """Kullanıcı adı ve e-posta aynı kilit sayacını kullanır."""
        for name in ['ogrenci01','ogrenci01@example.invalid','OGRENCI01','OGRENCI01@example.invalid','ogrenci01']:
            login(self.conn,name,False)
        self.assertEqual(scalar(self.conn,'SELECT kilitli FROM KULLANICI WHERE kullanici_id=%s',(self.student,)),1)

    def test_22_logout(self):
        """Çıkış oturumu iptal eder ve tekrar çağrılabilir."""
        _,_,token=login(self.conn,'ogrenci01',True)
        self.sql('CALL sp_cikis_yap(%s)',(token,))
        self.assertEqual(scalar(self.conn,VALID_SESSION,(token,)),0)
        first=scalar(self.conn,'SELECT iptal_tarihi FROM OTURUM WHERE token_hash=%s',(token,))
        self.sql('CALL sp_cikis_yap(%s)',(token,))
        self.assertEqual(scalar(self.conn,'SELECT iptal_tarihi FROM OTURUM WHERE token_hash=%s',(token,)),first)

    def test_23_expiry_and_inactive(self):
        """Süresi dolmuş oturum ve pasif kullanıcı erişemez."""
        _,_,token=login(self.conn,'ogrenci01',True)
        self.sql('UPDATE OTURUM SET olusturulma_tarihi=UTC_TIMESTAMP()-INTERVAL 2 HOUR,'
                 'son_kullanma_tarihi=UTC_TIMESTAMP()-INTERVAL 1 HOUR WHERE token_hash=%s',(token,))
        self.assertEqual(scalar(self.conn,VALID_SESSION,(token,)),0)
        self.sql('UPDATE KULLANICI SET aktif=0 WHERE kullanici_id=%s',(self.student,))
        self.assertEqual(login(self.conn,'ogrenci01',True)[0],0)
        self.assertEqual(scalar(self.conn,"SELECT COUNT(*) FROM GIRIS_DENEMESI WHERE sonuc='PASIF'"),1)

    def test_24_admin_unlock(self):
        """Kilit yalnız geçerli admin oturumuyla açılır; eski oturumlar iptal edilir."""
        _,_,old_student=login(self.conn,'ogrenci01',True)
        _,_,other_student=login(self.conn,'ogrenci02',True)
        _,_,admin_token=login(self.conn,'admin_demo',True)
        for _ in range(5):login(self.conn,'ogrenci01',False)
        self.reject('CALL sp_hesap_kilidini_ac(%s,%s)',(other_student,self.student),(1644,))
        self.sql('CALL sp_hesap_kilidini_ac(%s,%s)',(admin_token,self.student))
        self.assertEqual(self.sql('SELECT basarisiz_giris_sayisi,kilitli FROM KULLANICI WHERE kullanici_id=%s',
                                  (self.student,)),((0,0),))
        self.assertEqual(scalar(self.conn,VALID_SESSION,(old_student,)),0)
        self.assertEqual(login(self.conn,'ogrenci01',True)[0],1)

    def test_25_concurrent_failed_logins(self):
        """Eşzamanlı beş hatalı girişte sayaç artışları kaybolmaz."""
        self.conn.commit()
        def attempt(_):
            conn=pymysql.connect(**CONFIG,database=self.db)
            try:return login(conn,'ogrenci01',False)[0]
            finally:conn.close()
        with ThreadPoolExecutor(max_workers=5) as pool:
            self.assertEqual(list(pool.map(attempt,range(5))),[0]*5)
        self.assertEqual(self.sql('SELECT basarisiz_giris_sayisi,kilitli FROM KULLANICI WHERE kullanici_id=%s',
                                  (self.student,)),((5,1),))
        self.assertEqual(scalar(self.conn,'SELECT COUNT(*) FROM GIRIS_DENEMESI'),5)

    def test_26_import_transaction_rollback(self):
        """Veri setinin son satırı hatalıysa önceki ekleme de geri alınabilir."""
        self.sql("INSERT INTO KULLANICI(kullanici_adi,ad_soyad,eposta,telefon_no,sifre_hash,rol) "
                 "SELECT 'test_import','Test Import','test_import@example.invalid',telefon_no,sifre_hash,rol "
                 'FROM KULLANICI WHERE kullanici_id=%s',(self.admin,))
        self.reject('UPDATE KULLANICI SET onceki_staj_gun_sayisi=-1 WHERE kullanici_id=%s',(self.student,),(1264,))
        self.conn.rollback()
        self.assertEqual(scalar(self.conn,"SELECT COUNT(*) FROM KULLANICI WHERE kullanici_adi='test_import'"),0)

    def test_27_bad_session_rolls_back_login(self):
        """Geçersiz oturum hash'i giriş güncellemesini/logunu atomik geri alır."""
        login(self.conn,'ogrenci01',False)
        with self.assertRaises(pymysql.MySQLError):
            login(self.conn,'ogrenci01',True,token='not-a-hash')
        self.assertEqual(scalar(self.conn,'SELECT basarisiz_giris_sayisi FROM KULLANICI WHERE kullanici_id=%s',(self.student,)),1)
        self.assertEqual(scalar(self.conn,'SELECT COUNT(*) FROM GIRIS_DENEMESI'),1)
        self.assertEqual(scalar(self.conn,'SELECT COUNT(*) FROM OTURUM'),0)


class RecordingResult(unittest.TextTestResult):
    def __init__(self,*args,**kwargs):
        super().__init__(*args,**kwargs)
        self.records=[]
    def addSuccess(self,test):
        super().addSuccess(test)
        self.records.append(dict(test=test._testMethodName,aciklama=test.shortDescription(),sonuc='GECTI'))
    def addFailure(self,test,err):
        super().addFailure(test,err)
        self.records.append(dict(test=str(test),sonuc='BASARISIZ'))
    def addError(self,test,err):
        super().addError(test,err)
        self.records.append(dict(test=str(test),sonuc='HATA'))


if __name__=='__main__':
    suite=unittest.defaultTestLoader.loadTestsFromTestCase(DatabaseTests)
    result=unittest.TextTestRunner(verbosity=2,resultclass=RecordingResult).run(suite)
    report=dict(tarih_utc=datetime.now(timezone.utc).isoformat(),mysql_surumu=getattr(DatabaseTests,'version',None),
                test_sayisi=result.testsRun,basarili=result.wasSuccessful(),testler=result.records,
                notlar='Gerçek MySQL üzerinde yürütülmüştür. Yalnızca rastgele geçici test şeması kullanılır.')
    (ROOT/'TEST_SONUCLARI.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    raise SystemExit(0 if result.wasSuccessful() else 1)
