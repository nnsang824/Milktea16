using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("Topping")]
public partial class Topping
{
    [Key]
    public int MaTopping { get; set; }

    [StringLength(100)]
    public string TenTopping { get; set; } = null!;

    [Column(TypeName = "decimal(18, 0)")]
    public decimal Gia { get; set; }

    [InverseProperty("MaToppingNavigation")]
    public virtual ICollection<ChiTietTopping> ChiTietToppings { get; set; } = new List<ChiTietTopping>();
}
