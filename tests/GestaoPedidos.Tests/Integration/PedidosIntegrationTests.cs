using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace GestaoPedidos.Tests.Integration;

public class PedidosApiFactory : ApiFactory { }

public class PedidosIntegrationTests : IClassFixture<PedidosApiFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PedidosIntegrationTests(PedidosApiFactory factory)
        => _client = factory.CreateClient();

    private async Task<Guid> CriarClienteAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Cliente Teste",
            email = $"cliente_{Guid.NewGuid()}@teste.com",
            documento = GerarCpfUnico()
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private static string GerarCpfUnico()
    {
        // Gera um CPF válido a partir de 9 dígitos aleatórios
        var rand = new Random();
        var digits = Enumerable.Range(0, 9).Select(_ => rand.Next(0, 9)).ToArray();

        static int Digito(int[] d, int[] mult)
        {
            var soma = d.Take(mult.Length).Select((v, i) => v * mult[i]).Sum();
            var r = soma % 11;
            return r < 2 ? 0 : 11 - r;
        }

        var d1 = Digito(digits, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var all9 = digits.Concat([d1]).ToArray();
        var d2 = Digito(all9, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

        return string.Join("", digits) + d1 + d2;
    }

    private async Task<(Guid id, int estoque)> CriarProdutoAsync(int estoque = 10, decimal preco = 50m)
    {
        var resp = await _client.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Produto Teste",
            descricao = "desc",
            preco,
            estoque
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("id").GetGuid(), estoque);
    }

    [Fact]
    public async Task Criar_pedido_valido_retorna_201_e_debita_estoque()
    {
        var clienteId = await CriarClienteAsync();
        var (produtoId, _) = await CriarProdutoAsync(estoque: 5);

        var resp = await _client.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 3 } }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var produto = await _client.GetFromJsonAsync<JsonElement>($"/api/produtos/{produtoId}");
        produto.GetProperty("estoque").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Criar_pedido_com_estoque_insuficiente_retorna_400()
    {
        var clienteId = await CriarClienteAsync();
        var (produtoId, _) = await CriarProdutoAsync(estoque: 2);

        var resp = await _client.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 5 } }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Preco_do_item_e_congelado_no_momento_da_criacao()
    {
        var clienteId = await CriarClienteAsync();
        var (produtoId, _) = await CriarProdutoAsync(preco: 100m);

        var pedidoResp = await _client.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 1 } }
        });
        pedidoResp.EnsureSuccessStatusCode();
        var pedidoBody = await pedidoResp.Content.ReadFromJsonAsync<JsonElement>();
        var pedidoId = pedidoBody.GetProperty("id").GetGuid();

        var precoNoCriacao = pedidoBody
            .GetProperty("itens")[0]
            .GetProperty("precoUnitario").GetDecimal();

        precoNoCriacao.Should().Be(100m);
    }

    [Fact]
    public async Task Cancelamento_antes_de_envio_devolve_estoque()
    {
        var clienteId = await CriarClienteAsync();
        var (produtoId, estoqueInicial) = await CriarProdutoAsync(estoque: 10);

        var pedidoResp = await _client.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 4 } }
        });
        pedidoResp.EnsureSuccessStatusCode();
        var pedidoId = (await pedidoResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await _client.PatchAsJsonAsync($"/api/pedidos/{pedidoId}/status", new
        {
            novoStatus = 4,
            motivo = "cliente desistiu"
        });

        var produto = await _client.GetFromJsonAsync<JsonElement>($"/api/produtos/{produtoId}");
        produto.GetProperty("estoque").GetInt32().Should().Be(estoqueInicial);
    }

    [Fact]
    public async Task Cancelamento_apos_envio_nao_devolve_estoque()
    {
        var clienteId = await CriarClienteAsync();
        var (produtoId, _) = await CriarProdutoAsync(estoque: 10);

        var pedidoResp = await _client.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 3 } }
        });
        pedidoResp.EnsureSuccessStatusCode();
        var pedidoId = (await pedidoResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Criado → Pago → Enviado
        await _client.PatchAsJsonAsync($"/api/pedidos/{pedidoId}/status", new { novoStatus = 2 });
        await _client.PatchAsJsonAsync($"/api/pedidos/{pedidoId}/status", new { novoStatus = 3 });

        // Enviado → Cancelado deve ser negado pela máquina de estados
        var cancelResp = await _client.PatchAsJsonAsync($"/api/pedidos/{pedidoId}/status", new { novoStatus = 4 });
        cancelResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var produto = await _client.GetFromJsonAsync<JsonElement>($"/api/produtos/{produtoId}");
        produto.GetProperty("estoque").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task Cliente_inativo_nao_pode_criar_pedido()
    {
        var clienteId = await CriarClienteAsync();
        var (produtoId, _) = await CriarProdutoAsync();

        await _client.PatchAsJsonAsync($"/api/clientes/{clienteId}/status", new { ativo = false });

        var resp = await _client.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 1 } }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Produto_inativo_nao_pode_ser_adicionado_ao_pedido()
    {
        var clienteId = await CriarClienteAsync();
        var (produtoId, _) = await CriarProdutoAsync();

        await _client.PatchAsJsonAsync($"/api/produtos/{produtoId}/status", new { ativo = false });

        var resp = await _client.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 1 } }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unicidade_de_documento_entre_clientes_ativos()
    {
        var cpf = "529.982.247-25";
        await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "A",
            email = $"a_{Guid.NewGuid()}@teste.com",
            documento = cpf
        });

        var resp = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "B",
            email = $"b_{Guid.NewGuid()}@teste.com",
            documento = cpf
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
