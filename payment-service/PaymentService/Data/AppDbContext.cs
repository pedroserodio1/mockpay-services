using Microsoft.EntityFrameworkCore;
using PaymentService.Models; // Importe seu modelo

namespace PaymentService.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Payment> Payments { get; set; }
}