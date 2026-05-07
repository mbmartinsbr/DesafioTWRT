using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace GestaoPedidos.Tests.Integration;

public class ClientesApiFactory : ApiFactory { }

public class ClientesIntegrationTests : IClassFixture<ClientesApiFactory>
{
    private readonly HttpClient _client;

    // CPFs gerados e validados pelos testes unitários de CpfCnpjValidator
    private const string Cpf1 = "529.982.247-25";
    private const string Cpf2 = "111.444.777-35";

    public ClientesIntegrationTests(ClientesApiFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task POST_cliente_valido_retorna_201()
    {
        var response = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "João Silva",
            email = $"joao_{Guid.NewGuid()}@teste.com",
            documento = Cpf1
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_cliente_com_CPF_invalido_retorna_422()
    {
        var response = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "João",
            email = $"invalido_{Guid.NewGuid()}@teste.com",
            documento = "111.111.111-11"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_cliente_com_email_duplicado_retorna_409()
    {
        var email = $"dup_{Guid.NewGuid()}@teste.com";

        // Primeiro cliente usa Cpf2 para não conflitar com Cpf1 usado em outro teste
        var r1 = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "A",
            email,
            documento = Cpf2
        });
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Segundo cliente com mesmo email (CPF diferente → conflito de email)
        var cpfUnico = GerarCpfUnico();
        var response = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "B",
            email,
            documento = cpfUnico
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_cliente_com_documento_duplicado_retorna_409()
    {
        var cpfUnico = GerarCpfUnico();

        var r1 = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "A",
            email = $"doc1_{Guid.NewGuid()}@teste.com",
            documento = cpfUnico
        });
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "B",
            email = $"doc2_{Guid.NewGuid()}@teste.com",
            documento = cpfUnico
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GET_cliente_inexistente_retorna_404()
    {
        var response = await _client.GetAsync($"/api/clientes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string GerarCpfUnico()
    {
        var rand = new Random();
        int[] d;
        string cpf;
        do
        {
            d = Enumerable.Range(0, 9).Select(_ => rand.Next(0, 9)).ToArray();
            int Dig(int[] nums, int[] mult) { var s = nums.Take(mult.Length).Select((v, i) => v * mult[i]).Sum() % 11; return s < 2 ? 0 : 11 - s; }
            var d1 = Dig(d, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
            var all = d.Concat([d1]).ToArray();
            var d2 = Dig(all, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
            cpf = string.Join("", d) + d1 + d2;
        } while (cpf.Distinct().Count() == 1); // rejeita sequências inválidas como 00000000000

        return cpf;
    }
}
