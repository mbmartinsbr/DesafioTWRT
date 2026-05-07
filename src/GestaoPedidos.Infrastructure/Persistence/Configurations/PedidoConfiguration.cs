using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoPedidos.Infrastructure.Persistence.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).HasConversion<int>().IsRequired();
        builder.Property(p => p.ValorTotal).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(p => p.Cliente)
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Historico)
            .WithOne()
            .HasForeignKey(h => h.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Itens).HasField("_itens").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Historico).HasField("_historico").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
