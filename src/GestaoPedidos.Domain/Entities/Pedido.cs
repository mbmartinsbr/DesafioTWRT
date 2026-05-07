using GestaoPedidos.Domain.Enums;

namespace GestaoPedidos.Domain.Entities;

public class Pedido
{
    private static readonly Dictionary<StatusPedido, StatusPedido[]> _transicoesPermitidas = new()
    {
        [StatusPedido.Criado]    = [StatusPedido.Pago, StatusPedido.Cancelado],
        [StatusPedido.Pago]      = [StatusPedido.Enviado],
        [StatusPedido.Enviado]   = [],
        [StatusPedido.Cancelado] = []
    };

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public StatusPedido Status { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public Cliente Cliente { get; private set; } = null!;

    private readonly List<ItemPedido> _itens = [];
    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    private readonly List<HistoricoPedido> _historico = [];
    public IReadOnlyCollection<HistoricoPedido> Historico => _historico.AsReadOnly();

    private Pedido() { }

    public static Pedido Criar(Guid clienteId, List<ItemPedido> itens, Guid? id = null)
    {
        var pedido = new Pedido
        {
            Id = id ?? Guid.NewGuid(),
            ClienteId = clienteId,
            Status = StatusPedido.Criado,
            CriadoEm = DateTime.UtcNow
        };

        pedido._itens.AddRange(itens);
        pedido.ValorTotal = Math.Round(itens.Sum(i => i.ValorTotal), 2, MidpointRounding.AwayFromZero);
        pedido._historico.Add(HistoricoPedido.Criar(pedido.Id, null, StatusPedido.Criado));

        return pedido;
    }

    public (bool sucesso, string? erro) AlterarStatus(StatusPedido novoStatus, string? motivo = null)
    {
        if (Status == novoStatus)
            return (false, $"Pedido já se encontra no status '{novoStatus}'.");

        if (!_transicoesPermitidas[Status].Contains(novoStatus))
            return (false, $"Transição de '{Status}' para '{novoStatus}' não é permitida.");

        var anterior = Status;
        Status = novoStatus;
        _historico.Add(HistoricoPedido.Criar(Id, anterior, novoStatus, motivo));

        return (true, null);
    }

    public bool PodeRetornarEstoque() => Status != StatusPedido.Enviado;
}
