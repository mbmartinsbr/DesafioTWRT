using GestaoPedidos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoPedidos.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Documento).IsRequired().HasMaxLength(20);

        builder.HasIndex(c => new { c.Email, c.Ativo })
            .HasFilter("\"Ativo\" = true")
            .IsUnique();

        builder.HasIndex(c => new { c.Documento, c.Ativo })
            .HasFilter("\"Ativo\" = true")
            .IsUnique();
    }
}
