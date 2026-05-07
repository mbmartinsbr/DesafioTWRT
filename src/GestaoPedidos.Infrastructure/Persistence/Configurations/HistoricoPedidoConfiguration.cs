using GestaoPedidos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoPedidos.Infrastructure.Persistence.Configurations;

public class HistoricoPedidoConfiguration : IEntityTypeConfiguration<HistoricoPedido>
{
    public void Configure(EntityTypeBuilder<HistoricoPedido> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.StatusAnterior).HasConversion<int?>();
        builder.Property(h => h.NovoStatus).HasConversion<int>().IsRequired();
        builder.Property(h => h.Motivo).HasMaxLength(500);
    }
}
