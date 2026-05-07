using GestaoPedidos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoPedidos.Infrastructure.Persistence.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Descricao).HasMaxLength(1000);
        builder.Property(p => p.Preco).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Estoque).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("CK_Produto_Estoque", "\"Estoque\" >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Produto_Preco", "\"Preco\" > 0"));
    }
}
