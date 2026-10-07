using System.ComponentModel.DataAnnotations;

namespace MyPOSApp.Models;

public class ProductFormModel
{
    [Required(ErrorMessage = "Nama produk wajib diisi.")]
    [StringLength(200, ErrorMessage = "Nama produk maksimal 200 karakter.")]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Harga harus lebih besar dari 0.")]
    public decimal Price { get; set; }
}
