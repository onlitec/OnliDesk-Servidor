using Microsoft.EntityFrameworkCore;
using OliAcessoRemoto.Servidor.Models;

namespace OliAcessoRemoto.Servidor.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ClientConnection> ClientConnections { get; set; }
    public DbSet<ConnectionAttempt> ConnectionAttempts { get; set; }
    public DbSet<SystemMetrics> SystemMetrics { get; set; }
    public DbSet<ServerConfiguration> ServerConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações para ClientConnection
        modelBuilder.Entity<ClientConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ClientName).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.ConnectionType).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ConnectedAt);
        });

        // Configurações para ConnectionAttempt
        modelBuilder.Entity<ConnectionAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.AttemptType).HasMaxLength(20);
            
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.AttemptTime);
            entity.HasIndex(e => e.Success);
        });

        // Configurações para SystemMetrics
        modelBuilder.Entity<SystemMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configurações para ServerConfiguration
        modelBuilder.Entity<ServerConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ModifiedBy).HasMaxLength(100);
            
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Dados iniciais
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServerConfiguration>().HasData(
            new ServerConfiguration
            {
                Id = 1,
                Key = "MaxConcurrentConnections",
                Value = "1000",
                Description = "Número máximo de conexões simultâneas",
                LastModified = DateTime.UtcNow,
                ModifiedBy = "System"
            },
            new ServerConfiguration
            {
                Id = 2,
                Key = "ClientTimeoutMinutes",
                Value = "30",
                Description = "Timeout de cliente em minutos",
                LastModified = DateTime.UtcNow,
                ModifiedBy = "System"
            },
            new ServerConfiguration
            {
                Id = 3,
                Key = "ScreenUpdateIntervalMs",
                Value = "100",
                Description = "Intervalo de atualização de tela em milissegundos",
                LastModified = DateTime.UtcNow,
                ModifiedBy = "System"
            },
            new ServerConfiguration
            {
                Id = 4,
                Key = "EnableLogging",
                Value = "true",
                Description = "Habilitar logging detalhado",
                LastModified = DateTime.UtcNow,
                ModifiedBy = "System"
            }
        );
    }
}