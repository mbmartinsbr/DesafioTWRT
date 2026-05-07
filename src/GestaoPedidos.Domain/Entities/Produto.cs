namespace GestaoPedidos.Domain.Entities;

public class Produto
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public int Estoque { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Produto() { }

    public static Produto Criar(string nome, string? descricao, decimal preco, int estoque)
    {
        return new Produto
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Descricao = descricao,
            Preco = preco,
            Estoque = estoque,
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };
    }

    public void Atualizar(string nome, string? descricao)
    {
        Nome = nome;
        Descricao = descricao;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AtualizarPreco(decimal preco)
    {
        Preco = preco;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AtualizarEstoque(int quantidade)
    {
        Estoque = quantidade;
        AtualizadoEm = DateTime.UtcNow;
    }

    public bool DebitarEstoque(int quantidade)
    {
        if (Estoque < quantidade) return false;
        Estoque -= quantidade;
        AtualizadoEm = DateTime.UtcNow;
        return true;
    }

    public void DevolverEstoque(int quantidade)
    {
        Estoque += quantidade;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Ativar()
    {
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Inativar()
    {
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }
}
