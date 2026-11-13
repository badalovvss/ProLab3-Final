using System.ComponentModel.DataAnnotations;

namespace StajYönetimSistemi.Models
{
    public class Evrak
    {
        [Key]
        public int Id { get; set; }
        public int OgrenciId { get; set; }
        public string DosyaYolu { get; set; }
        public string GoogleFormUrl { get; set; }
        public string Statu { get; set; } // "Onay Bekliyor", "Onaylandı", "Reddedildi"
    }
}