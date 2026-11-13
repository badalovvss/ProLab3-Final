using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StajYönetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class StajBilgileriSutunlariEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tablo oluşturmak yerine, var olan Kullanicilar tablosuna yeni sütunları ekliyoruz:
            migrationBuilder.AddColumn<DateTime>(
                name: "StajBaslangicTarihi",
                table: "Kullanicilar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StajBitisTarihi",
                table: "Kullanicilar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StajGunleri",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hata durumunda (geri alma işleminde) sadece bu sütunlar silinir.
            migrationBuilder.DropColumn(
                name: "StajBaslangicTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "StajBitisTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "StajGunleri",
                table: "Kullanicilar");
        }
    }
}