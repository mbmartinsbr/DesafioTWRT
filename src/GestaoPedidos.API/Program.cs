using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using GestaoPedidos.API.Converters;
using GestaoPedidos.API.Filters;
using GestaoPedidos.API.Middlewares;
using GestaoPedidos.Application.Services;
using GestaoPedidos.Infrastructure.Extensions;
using GestaoPedidos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new BrasiliaDateTimeConverter()))
    .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<GestaoPedidos.Application.AssemblyMarker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gestão de Pedidos API",
        Version = "v1",
        Description = """
            API RESTful para gestão de pedidos com controle de clientes, produtos e estoque.

            ## Funcionalidades principais
            - **Clientes** — cadastro com validação algorítmica de CPF/CNPJ, soft delete, unicidade entre ativos
            - **Produtos** — cadastro, atualização, controle de estoque (nunca negativo), soft delete
            - **Pedidos** — criação transacional com snapshot de preço e débito de estoque atômico; máquina de estados com histórico

            ## Formato de erros
            Todos os erros seguem o padrão:
            ```json
            {
              "tipo": "validation_error",
              "mensagem": "Dados inválidos",
              "erros": ["Campo X é obrigatório"]
            }
            ```

            ## Datas
            Todas as datas são persistidas em **UTC** e retornadas no fuso **America/Sao_Paulo**.
            """
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.UseInlineDefinitionsForEnums();
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<PedidoService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestão de Pedidos API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Gestão de Pedidos — API Docs";
    c.DefaultModelsExpandDepth(1);
    c.DefaultModelExpandDepth(2);
});
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
