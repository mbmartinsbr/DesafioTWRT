using FluentAssertions;
using GestaoPedidos.Domain.Entities;
using GestaoPedidos.Domain.Enums;

namespace GestaoPedidos.Tests.Unit;

public class PedidoStatusMachineTests
{
    private static Pedido CriarPedidoBase()
    {
        var item = ItemPedido.Criar(Guid.NewGuid(), Guid.NewGuid(), 1, 10m);
        return Pedido.Criar(Guid.NewGuid(), [item]);
    }

    [Fact]
    public void Criado_para_Pago_deve_ser_permitido()
    {
        var pedido = CriarPedidoBase();
        var (sucesso, _) = pedido.AlterarStatus(StatusPedido.Pago);
        sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Pago);
    }

    [Fact]
    public void Criado_para_Cancelado_deve_ser_permitido()
    {
        var pedido = CriarPedidoBase();
        var (sucesso, _) = pedido.AlterarStatus(StatusPedido.Cancelado);
        sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Cancelado);
    }

    [Fact]
    public void Pago_para_Enviado_deve_ser_permitido()
    {
        var pedido = CriarPedidoBase();
        pedido.AlterarStatus(StatusPedido.Pago);
        var (sucesso, _) = pedido.AlterarStatus(StatusPedido.Enviado);
        sucesso.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Enviado);
    }

    [Fact]
    public void Criado_para_Enviado_deve_ser_negado()
    {
        var pedido = CriarPedidoBase();
        var (sucesso, erro) = pedido.AlterarStatus(StatusPedido.Enviado);
        sucesso.Should().BeFalse();
        erro.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Enviado_para_qualquer_status_deve_ser_negado()
    {
        var pedido = CriarPedidoBase();
        pedido.AlterarStatus(StatusPedido.Pago);
        pedido.AlterarStatus(StatusPedido.Enviado);

        pedido.AlterarStatus(StatusPedido.Cancelado).sucesso.Should().BeFalse();
    }

    [Fact]
    public void Cancelado_para_qualquer_status_deve_ser_negado()
    {
        var pedido = CriarPedidoBase();
        pedido.AlterarStatus(StatusPedido.Cancelado);

        pedido.AlterarStatus(StatusPedido.Pago).sucesso.Should().BeFalse();
    }

    [Fact]
    public void Status_igual_ao_atual_deve_retornar_erro_sem_gerar_historico()
    {
        var pedido = CriarPedidoBase();
        var historicoAntes = pedido.Historico.Count;

        var (sucesso, erro) = pedido.AlterarStatus(StatusPedido.Criado);

        sucesso.Should().BeFalse();
        erro.Should().Contain("já se encontra");
        pedido.Historico.Count.Should().Be(historicoAntes);
    }

    [Fact]
    public void Criar_pedido_deve_registrar_historico_inicial_com_StatusAnterior_nulo()
    {
        var pedido = CriarPedidoBase();

        pedido.Historico.Should().HaveCount(1);
        pedido.Historico.First().StatusAnterior.Should().BeNull();
        pedido.Historico.First().NovoStatus.Should().Be(StatusPedido.Criado);
    }

    [Fact]
    public void Transicao_valida_deve_registrar_historico()
    {
        var pedido = CriarPedidoBase();
        pedido.AlterarStatus(StatusPedido.Pago, "pagamento confirmado");

        pedido.Historico.Should().HaveCount(2);
        var ultimo = pedido.Historico.Last();
        ultimo.StatusAnterior.Should().Be(StatusPedido.Criado);
        ultimo.NovoStatus.Should().Be(StatusPedido.Pago);
        ultimo.Motivo.Should().Be("pagamento confirmado");
    }

    [Fact]
    public void PodeRetornarEstoque_deve_ser_falso_quando_Enviado()
    {
        var pedido = CriarPedidoBase();
        pedido.AlterarStatus(StatusPedido.Pago);
        pedido.AlterarStatus(StatusPedido.Enviado);

        pedido.PodeRetornarEstoque().Should().BeFalse();
    }

    [Fact]
    public void PodeRetornarEstoque_deve_ser_verdadeiro_quando_Criado()
    {
        var pedido = CriarPedidoBase();
        pedido.PodeRetornarEstoque().Should().BeTrue();
    }
}
