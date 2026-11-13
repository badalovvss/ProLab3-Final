using System.ComponentModel.DataAnnotations;

namespace StajYönetimSistemi.Models
{
    public abstract class Kullanici
    {
        [Key]
        // Kapsülleme (Encapsulation) - Sadece sınıfın içinde erişilebilir
        public int Id { get; set; }
        public string AdSoyad { get; set; }
        public string Eposta { get; set; }
        public string SifreHash { get; set; }
        public int HataliGirisSayisi { get; set; } = 0;
        public bool HesapKilitliMi { get; set; } = false;
        // Staj Bilgileri (Boş geçilebilir olmaları için ? koyuyoruz)
        public DateTime? StajBaslangicTarihi { get; set; }
        public DateTime? StajBitisTarihi { get; set; }
        public string? StajGunleri { get; set; }
    }
}