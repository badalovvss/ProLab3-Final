    namespace StajYönetimSistemi.Models
{
    // Ogrenci sınıfı mutlaka Kullanici sınıfından miras almalıdır
    public class Ogrenci : Kullanici
    {
        public string TcKimlik { get; set; }
        public string OgrenciNo { get; set; }
        public string Bolum { get; set; }
    }
}