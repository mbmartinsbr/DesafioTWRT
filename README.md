# Gestão de Pedidos — API

API RESTful para gestão de pedidos, desenvolvida em .NET 10 / ASP.NET Core como solução ao desafio técnico.

---

## Como executar localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Passo a passo

```bash
# 1. Subir o banco PostgreSQL via Docker
docker compose up -d

# 2. Restaurar dependências e executar a API
dotnet run --project src/GestaoPedidos.API

# 3. Acessar o Swagger UI
# http://localhost:5000/swagger
```

As migrations são aplicadas automaticamente na inicialização. Não é necessário rodar `dotnet ef database update` manualmente.

### Executar os testes

```bash
dotnet test
```

---

## Tecnologias utilizadas

| Componente       | Escolha                                      |
|------------------|----------------------------------------------|
| Framework        | .NET 10 / ASP.NET Core Web API               |
| ORM              | Entity Framework Core 10                     |
| Banco de dados   | PostgreSQL 16 (via Docker)                   |
| Validação        | FluentValidation 11                          |
| Documentação     | Swagger / OpenAPI (Swashbuckle 6)            |
| Testes           | xUnit + Moq + FluentAssertions               |

---

## Estrutura da solução

```
GestaoPedidos.sln
├── src/
│   ├── GestaoPedidos.API            ← Controllers, Middlewares, Program.cs
│   ├── GestaoPedidos.Application    ← Services, DTOs, Validators
│   ├── GestaoPedidos.Domain         ← Entidades, Enums, Interfaces de repositório
│   └── GestaoPedidos.Infrastructure ← EF Core, Repositórios, Migrations
└── tests/
    └── GestaoPedidos.Tests          ← Testes de unidade e integração (xUnit)
```

A arquitetura segue separação em camadas onde **Application** não referencia **Infrastructure** — a inversão de dependência é feita via interfaces de repositório definidas no **Domain** e implementadas em **Infrastructure**, registradas no DI em `Program.cs`.

---

## Endpoints disponíveis

### Clientes

| Método  | Rota                         | Descrição                      |
|---------|------------------------------|--------------------------------|
| POST    | `/api/clientes`              | Cadastrar cliente              |
| GET     | `/api/clientes`              | Listar (paginado)              |
| GET     | `/api/clientes/{id}`         | Buscar por ID                  |
| PATCH   | `/api/clientes/{id}/status`  | Ativar / Desativar             |

### Produtos

| Método  | Rota                          | Descrição                       |
|---------|-------------------------------|---------------------------------|
| POST    | `/api/produtos`               | Cadastrar produto               |
| GET     | `/api/produtos`               | Listar (paginado)               |
| GET     | `/api/produtos/{id}`          | Buscar por ID                   |
| PUT     | `/api/produtos/{id}`          | Atualizar nome e descrição      |
| PATCH   | `/api/produtos/{id}/preco`    | Atualizar preço                 |
| PATCH   | `/api/produtos/{id}/status`   | Ativar / Desativar              |
| PATCH   | `/api/produtos/{id}/estoque`  | Ajustar estoque manualmente     |

### Pedidos

| Método  | Rota                         | Descrição                         |
|---------|------------------------------|-----------------------------------|
| POST    | `/api/pedidos`               | Criar pedido                      |
| GET     | `/api/pedidos`               | Listar (paginado)                 |
| GET     | `/api/pedidos/{id}`          | Buscar por ID (itens + histórico) |
| PATCH   | `/api/pedidos/{id}/status`   | Alterar status                    |

---

## Decisões técnicas e trade-offs

### Arquitetura em camadas

Optou-se por separação em quatro projetos (API, Application, Domain, Infrastructure) para tornar as regras de negócio testáveis sem depender de infraestrutura. A camada **Application** só conhece interfaces definidas no **Domain**; o EF Core fica confinado à **Infrastructure**.

**Trade-off:** maior número de arquivos e indireções em comparação a um projeto único. Para a escala deste desafio, um monolito seria suficiente, mas a separação facilita a evolução e a escrita de testes de unidade puros.

---

### Estratégia de validação — FluentValidation

Toda validação de entrada é feita com **FluentValidation**, registrado via `AddFluentValidationAutoValidation()`. Um `ValidationFilter` global captura falhas de validação e retorna `422 Unprocessable Entity` com o formato padronizado:

```json
{
  "tipo": "validation_error",
  "mensagem": "Dados inválidos",
  "erros": ["Nome é obrigatório", "CPF inválido"]
}
```

Regras de negócio que dependem de estado persistido (ex.: unicidade de email) são verificadas nos **Services** e lançam `ConflictException`, tratada pelo `GlobalExceptionMiddleware` com `409 Conflict`.

---

### Validação algorítmica de CPF e CNPJ

O campo `Documento` aceita CPF (11 dígitos) ou CNPJ (14 dígitos) — apenas números, sem pontuação. A validação vai além do formato: os **dígitos verificadores** são calculados e conferidos, rejeitando sequências trivialmente inválidas como `11111111111`.

---

### Estratégia de persistência — EF Core + PostgreSQL

O mapeamento usa **Fluent API** (`IEntityTypeConfiguration<T>`) em vez de Data Annotations, mantendo as entidades de domínio livres de dependências de infraestrutura. Destaques:

- Índices únicos **filtrados por `Ativo = true`** em `Cliente.Email` e `Cliente.Documento`, permitindo que o mesmo email/documento seja reutilizado após desativação.
- `Produto.Preco` e `ItemPedido.ValorTotal` mapeados como `decimal(18,2)`.
- Check constraint `Produto.Estoque >= 0` a nível de banco como segunda linha de defesa.
- Enum `StatusPedido` persistido como `int`.

---

### Máquina de estados de pedido

As transições são validadas na entidade de domínio `Pedido`, não nos controllers ou services:

```
Criado    → Pago | Cancelado
Pago      → Enviado
Enviado   → (nenhuma transição permitida)
Cancelado → (nenhuma transição permitida)
```

**Status igual ao atual:** retorna `400 Bad Request` com mensagem explicativa. Nenhum registro de histórico é gerado.

**Histórico na criação:** ao criar um pedido, o primeiro registro de histórico é gravado com `StatusAnterior = null` e `NovoStatus = Criado`.

---

### Criação de pedido — fluxo transacional

Toda a criação de pedido ocorre em **uma única transação de banco**:

1. Validar cliente (existe e está ativo)
2. Para cada item: validar produto (existe e ativo), verificar estoque, capturar preço atual (snapshot)
3. Calcular `ValorTotal` do pedido
4. Debitar estoque de todos os produtos
5. Persistir pedido + itens + histórico inicial
6. Commit ou rollback total

O `ValorTotal` e o `PrecoUnitario` dos itens **nunca são aceitos do cliente** — são sempre calculados internamente. Isso garante integridade mesmo que o preço do produto seja alterado depois.

---

### Cancelamento e devolução de estoque

Ao cancelar um pedido:
- Se o status anterior era `Criado` ou `Pago` → o estoque de cada item é devolvido.
- Se o status anterior era `Enviado` → a transição é bloqueada (pedido já despachado não pode ser cancelado).

---

### Valores monetários

Todos os valores monetários usam o tipo `decimal` com precisão `decimal(18,2)` no banco. Cálculos de arredondamento intermediários utilizam `MidpointRounding.AwayFromZero` (arredondamento bancário padrão), evitando acúmulo de erro em somas com muitos itens.

---

### Datas, UTC e fuso horário

- **Persistência:** todos os timestamps são armazenados em **UTC** no banco.
- **Respostas:** um `JsonConverter` global (`BrasiliaDateTimeConverter`) converte automaticamente `DateTime UTC → America/Sao_Paulo` em todas as respostas JSON, sem necessidade de conversão manual nos services ou controllers.
- **Timezone ID no Windows:** `"E. South America Standard Time"` (diferente do Linux que usa `"America/Sao_Paulo"`). O converter usa `TimeZoneInfo.FindSystemTimeZoneById` com o ID correto para cada plataforma.

---

### Estratégia de estoque sob requisições simultâneas — Pessimistic Locking

O principal risco de concorrência é *two concurrent reads*: duas transações leem `Estoque = 5`, ambas validam que há estoque suficiente para 5 unidades e ambas debitam, resultando em estoque `-5`.

A solução adotada é **pessimistic locking com `SELECT FOR UPDATE`** do PostgreSQL:

```sql
SELECT * FROM "Produtos" WHERE "Id" = @id FOR UPDATE
```

Implementado via `FromSqlRaw` dentro da transação de criação de pedido. Isso garante que a linha do produto seja bloqueada para leitura/escrita até o commit da transação, eliminando a race condition sem necessidade de retry ou lógica de compensação.

**Por que pessimistic e não optimistic?**
- Optimistic locking (rowversion/concurrency token) exigiria lógica de retry no cliente e poderia causar degradação sob alta contenção.
- Pessimistic é mais simples, previsível e adequado para um cenário onde conflitos de estoque são esperados (ex.: flash sales).

---

### Paginação

Paginação offset-based em todos os endpoints de listagem:

```
GET /api/clientes?pagina=1&tamanhoPagina=20
```

Resposta:
```json
{
  "dados": [...],
  "pagina": 1,
  "tamanhoPagina": 20,
  "total": 150,
  "totalPaginas": 8
}
```

---

## Pontos fora do escopo e como abordaríamos

| Ponto                         | Abordagem sugerida                                                                                   |
|-------------------------------|------------------------------------------------------------------------------------------------------|
| **Autenticação / Autorização**| JWT Bearer com ASP.NET Core Identity ou um IdP externo (Keycloak, Auth0). Roles para separar perfis de operador e admin. |
| **Observabilidade**           | OpenTelemetry com exportação para Jaeger/Grafana Tempo (traces), Prometheus (métricas), Serilog estruturado (logs). |
| **Cache**                     | Redis para listas paginadas de produtos e clientes, invalidado nos endpoints de escrita.             |
| **Mensageria / eventos**      | Publicar evento `PedidoCriado` / `PedidoCancelado` via RabbitMQ ou Kafka para desacoplar integrações (ex.: serviço de e-mail, financeiro). |
| **Rate limiting**             | `AddRateLimiter` do ASP.NET Core 8+ com policy por IP para proteger os endpoints públicos.          |
| **Cursor-based pagination**   | Para tabelas muito grandes, substituir offset por keyset pagination (cursor no último `Id`) para performance constante. |
| **Health checks**             | `AddHealthChecks().AddNpgsql(...)` exposto em `/health` para integração com orquestradores (K8s liveness/readiness). |
