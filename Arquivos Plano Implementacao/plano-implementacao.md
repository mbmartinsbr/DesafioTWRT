# Plano de Implementação — API de Gestão de Pedidos

## Stack Definida

| Componente | Escolha |
|---|---|
| Framework | .NET 8 (ASP.NET Core Web API) |
| ORM | Entity Framework Core 8 |
| Banco de dados | PostgreSQL (via Docker) |
| Validação | FluentValidation |
| Documentação | Swagger/OpenAPI (Swashbuckle) |
| Testes | xUnit + Moq + FluentAssertions |
| Valores monetários | `decimal` com `MidpointRounding.AwayFromZero` |

---

## Etapa 1 — Estrutura do Projeto e Configuração Base

**Objetivo:** Criar a solução com a arquitetura em camadas antes de qualquer código de negócio.

**Estrutura de projetos:**
```
GestãoPedidos.sln
├── src/
│   ├── GestãoPedidos.API            ← Controllers, Middlewares, Program.cs
│   ├── GestãoPedidos.Application    ← Services, DTOs, Validators, Interfaces
│   ├── GestãoPedidos.Domain         ← Entidades, Enums, Regras de domínio
│   └── GestãoPedidos.Infrastructure ← EF Core, Repositórios, Migrations
└── tests/
    └── GestãoPedidos.Tests          ← xUnit, testes de serviços e regras
```

**Tarefas:**
- [x] Criar a solução `.sln` e os 4 projetos
- [x] Referenciar projetos corretamente (API → Application → Domain, Infrastructure → Domain)
- [x] Adicionar pacotes NuGet base: EF Core, Npgsql, FluentValidation, Swashbuckle
- [x] Configurar `Program.cs` com DI, Swagger, Middleware de erros, Timezone
- [x] Configurar `docker-compose.yml` com PostgreSQL
- [x] Criar `AppDbContext` com a connection string via `appsettings.json`

**Commit:** `feat: scaffold solution structure with layered architecture` ✅

---

## Etapa 2 — Domínio: Entidades e Enums

**Objetivo:** Modelar todas as entidades de domínio com suas propriedades e regras internas.

**Entidades:**
```
Cliente         → Id, Nome, Email, Documento, Ativo, CriadoEm, AtualizadoEm
Produto         → Id, Nome, Descrição, Preço, Estoque, Ativo, CriadoEm, AtualizadoEm
Pedido          → Id, ClienteId, Status, ValorTotal, CriadoEm, [Itens], [Histórico]
ItemPedido      → Id, PedidoId, ProdutoId, Quantidade, PreçoUnitário, ValorTotal
HistóricoPedido → Id, PedidoId, StatusAnterior, NovoStatus, AlteradoEm, Motivo
```

**Enum:**
```csharp
public enum StatusPedido { Criado, Pago, Enviado, Cancelado }
```

**Regras de domínio no `Pedido`:**
```csharp
// Método que valida e executa transição de status
public Result AlterarStatus(StatusPedido novoStatus, string? motivo)
```

**Mapa de transições permitidas:**
```
Criado    → Pago, Cancelado
Pago      → Enviado
Enviado   → (nenhuma)
Cancelado → (nenhuma)
```

**Tarefas:**
- [x] Criar entidades com construtores protegidos (evitar estado inválido)
- [x] Criar enum `StatusPedido`
- [x] Implementar lógica de transição de status dentro do `Pedido`
- [x] Criar interfaces de repositório: `IClienteRepository`, `IProdutoRepository`, `IPedidoRepository`

**Commit:** `feat: add domain entities and order status state machine` ✅

---

## Etapa 3 — Infraestrutura: EF Core e Migrations

**Objetivo:** Mapear as entidades para o banco e criar as migrations iniciais.

**Configurações EF Core (`IEntityTypeConfiguration<T>`):**
- `Cliente`: índice único em `Email` filtrado por `Ativo = true`; índice único em `Documento` filtrado por `Ativo = true`
- `Produto`: `Preço` como `decimal(18,2)`; `Estoque` com check constraint `>= 0`
- `Pedido`: `ValorTotal` como `decimal(18,2)`; enum `Status` como `int`
- `ItemPedido`: `PreçoUnitário` e `ValorTotal` como `decimal(18,2)`
- `HistóricoPedido`: todos os timestamps em UTC

**Implementação de repositórios:**
- `ClienteRepository` com filtros e paginação
- `ProdutoRepository` com filtros e paginação
- `PedidoRepository` com `Include` dos itens e histórico

**Tarefas:**
- [x] Criar `AppDbContext` com os `DbSet<T>`
- [x] Criar configurações fluentes para cada entidade
- [x] Implementar os 3 repositórios concretos
- [x] Registrar repositórios no DI (`AddScoped`)
- [x] Adicionar migration inicial e verificar schema gerado
- [x] Configurar `dotnet-ef` no projeto

**Commit:** `feat: configure EF Core mappings, repositories and initial migration` ✅

---

## Etapa 4 — Application: Clientes

**Objetivo:** Implementar o CRUD de clientes com todas as regras de negócio.

**Endpoints:**
```
POST  /api/clientes               → Cadastrar cliente
GET   /api/clientes               → Listar (paginado)
GET   /api/clientes/{id}          → Buscar por ID
PATCH /api/clientes/{id}/status   → Ativar/Desativar
```

**Regras críticas a implementar:**
- Validação algorítmica de CPF (dígitos verificadores)
- Validação algorítmica de CNPJ (dígitos verificadores)
- Unicidade de email e documento entre clientes **ativos**
- Soft delete (desativar, não remover)
- Timestamps em UTC, resposta em `America/Sao_Paulo`

**Validators FluentValidation:**
```csharp
class CriarClienteValidator : AbstractValidator<CriarClienteRequest>
```

**Tarefas:**
- [x] Criar DTOs: `CriarClienteRequest`, `ClienteResponse`
- [x] Criar `ClienteService` com métodos: `CriarAsync`, `ListarAsync`, `ObterPorIdAsync`, `AlterarStatusAsync`
- [x] Implementar `CpfCnpjValidator` com validação algorítmica
- [x] Criar `ClientesController` com os 4 endpoints
- [x] Retornar `201 Created`, `200 OK`, `404 Not Found`, `409 Conflict`, `422 Unprocessable`

**Commit:** `feat: implement customer management with CPF/CNPJ validation` ✅

---

## Etapa 5 — Application: Produtos e Estoque

**Objetivo:** Implementar o CRUD de produtos com controle de estoque.

**Endpoints:**
```
POST  /api/produtos               → Cadastrar produto
GET   /api/produtos               → Listar (paginado)
GET   /api/produtos/{id}          → Buscar por ID
PUT   /api/produtos/{id}          → Atualizar dados (nome, descrição)
PATCH /api/produtos/{id}/status   → Ativar/Desativar
PATCH /api/produtos/{id}/estoque  → Ajustar estoque manualmente
```

**Regras críticas a implementar:**
- Preço > 0 obrigatório
- Estoque nunca negativo (validação em nível de serviço e banco)
- Produto inativo não pode ter estoque debitado
- Soft delete (produtos vinculados a pedidos não são removidos fisicamente)

**Tarefas:**
- [x] Criar DTOs: `CriarProdutoRequest`, `AtualizarProdutoRequest`, `AtualizarEstoqueRequest`, `ProdutoResponse`
- [x] Criar `ProdutoService` com todos os métodos
- [x] Criar `ProdutosController` com os 6 endpoints
- [x] Retornar HTTP codes corretos incluindo `400` para estoque negativo

**Commit:** `feat: implement product management and stock control` ✅

---

## Etapa 6 — Application: Pedidos (núcleo do sistema)

**Objetivo:** Implementar criação de pedidos com baixa de estoque transacional e máquina de estados.

**Endpoints:**
```
POST  /api/pedidos              → Criar pedido
GET   /api/pedidos              → Listar (paginado)
GET   /api/pedidos/{id}         → Buscar por ID (com itens e histórico)
PATCH /api/pedidos/{id}/status  → Alterar status
```

**Fluxo de criação (transacional):**
```
1. Validar cliente existe e está ativo
2. Validar que o pedido tem >= 1 item
3. Para cada item:
   a. Validar produto existe e está ativo
   b. Validar estoque suficiente
   c. Obter preço atual do produto (snapshot)
   d. Calcular ValorTotal do item
4. Calcular ValorTotal do pedido (soma)
5. Em uma única transação:
   a. Debitar estoque de todos os produtos
   b. Criar pedido + itens
   c. Registrar histórico status "Criado"
6. Commit ou Rollback total
```

**Fluxo de alteração de status:**
```
1. Obter pedido com itens
2. Validar transição permitida (máquina de estados)
3. Se Cancelado E status anterior ≠ Enviado → devolver estoque
4. Registrar histórico
5. Atualizar status
```

**Decisão documentada — status igual ao atual:**
> Retornar `400 Bad Request` com mensagem explicativa. Não gerar histórico.

**Tarefas:**
- [x] Criar DTOs: `CriarPedidoRequest`, `CriarItemPedidoRequest`, `AlterarStatusRequest`, `PedidoResponse`, `ItemPedidoResponse`, `HistóricoPedidoResponse`
- [x] Criar `PedidoService` com `CriarAsync` e `AlterarStatusAsync`
- [x] Implementar lógica transacional no serviço (via `IUnitOfWork` / `IDbContextTransaction`)
- [x] Implementar devolução de estoque no cancelamento
- [x] Criar `PedidosController` com os 4 endpoints
- [x] Registrar histórico em todas as transições válidas

**Commit:** `feat: implement order creation with transactional stock deduction` ✅
**Commit:** `feat: implement order status machine with history tracking` ✅

---

## Etapa 7 — Middleware, Error Handling e Paginação

**Objetivo:** Padronizar respostas de erro e implementar paginação consistente.

**Formato de erro padrão:**
```json
{
  "tipo": "validation_error",
  "mensagem": "Dados inválidos",
  "erros": ["Nome é obrigatório", "Email inválido"]
}
```

**Paginação (offset-based):**
```
GET /api/clientes?pagina=1&tamanhoPagina=20

Resposta:
{
  "dados": [...],
  "pagina": 1,
  "tamanhoPagina": 20,
  "total": 150,
  "totalPaginas": 8
}
```

**Tarefas:**
- [x] Criar `GlobalExceptionMiddleware` para tratar exceções não capturadas
- [x] Criar `ValidationFilter` para FluentValidation retornar `422`
- [x] Criar `PagedResult<T>` genérico
- [x] Aplicar paginação em `GET /api/clientes`, `GET /api/produtos`, `GET /api/pedidos`
- [x] Configurar conversão de timezone (UTC → `America/Sao_Paulo`) via `BrasiliaDateTimeConverter` no JSON serializer

**Commit:** `feat: add global error handling, pagination and timezone conversion` ✅

---

## Etapa 8 — Concorrência e Integridade de Estoque

**Objetivo:** Garantir que dois pedidos simultâneos não debitam mais estoque do que o disponível.

**Estratégia — Pessimistic Locking com `SELECT FOR UPDATE`:**

```sql
-- EF Core com PostgreSQL
SELECT * FROM "Produtos" WHERE "Id" = @id FOR UPDATE
```

```csharp
// No repositório, dentro da transação
await context.Produtos
    .FromSqlRaw("SELECT * FROM \"Produtos\" WHERE \"Id\" = {0} FOR UPDATE", id)
    .FirstOrDefaultAsync();
```

**Por que pessimistic locking:**
- Garante que nenhuma outra transação leia/escreva o produto até o commit
- Evita o problema de two concurrent reads vendo estoque = 10, ambas debitando 10
- Simples de implementar com PostgreSQL + EF Core

**Tarefas:**
- [x] Implementar lock por linha no `ProdutoRepository` dentro da transação de criação de pedido
- [x] Documentar a estratégia no README (Etapa 10)

**Commit:** `feat: add pessimistic locking to prevent concurrent stock overselling` ✅

---

## Etapa 9 — Testes Automatizados

**Objetivo:** Cobrir as principais regras de negócio com testes de unidade e integração.

**Estratégia de testes:**
- **Testes de unidade** (xUnit + Moq + FluentAssertions): regras de domínio isoladas
- **Testes de integração** (xUnit + `WebApplicationFactory` + banco SQLite in-memory ou PostgreSQL via Testcontainers): endpoints críticos ponta a ponta

**Casos de teste prioritários:**

| Cenário | Tipo |
|---|---|
| Transições de status válidas e inválidas | Unidade |
| Criação de pedido debita estoque | Integração |
| Pedido falha se estoque insuficiente | Integração |
| Cancelamento devolve estoque (status Criado) | Integração |
| Cancelamento NÃO devolve estoque (status Enviado) | Integração |
| Preço do item congelado no momento da criação | Integração |
| Validação CPF/CNPJ algorítmica | Unidade |
| Unicidade de email/documento entre ativos | Integração |
| Cliente inativo não pode criar pedido | Integração |
| Produto inativo não pode ser adicionado | Integração |

**Tarefas:**
- [x] Configurar projeto de testes com xUnit, Moq, FluentAssertions
- [x] Criar testes de unidade para `StatusPedido` (máquina de estados)
- [x] Criar testes de unidade para `CpfCnpjValidator`
- [x] Criar `WebApplicationFactory` com banco de testes (InMemory)
- [x] Implementar todos os casos de integração acima

**Commit:** `test: add unit tests for domain rules and validators` ✅
**Commit:** `test: add integration tests for order and stock flows` ✅

---

## Etapa 10 — Swagger, README e Entregáveis Finais

**Objetivo:** Documentar a API e escrever o README completo conforme exigido.

**Swagger:**
- Schemas tipados para todos os requests/responses
- Exemplos de requisição e resposta nos endpoints principais
- Descrição das respostas de erro (400, 404, 409, 422)
- Agrupamento por tag: Clientes, Produtos, Pedidos

**README obrigatório (todos os tópicos do PDF):**
- Como executar localmente (Docker + `dotnet run`)
- Tecnologias utilizadas
- Estrutura da solução
- Decisões técnicas e trade-offs
- Estratégia de validação (FluentValidation)
- Estratégia de persistência (EF Core + PostgreSQL)
- Estratégia de estoque (pessimistic locking)
- Valores monetários (`decimal`, `MidpointRounding.AwayFromZero`)
- Datas/UTC/fuso horário (persistência UTC, exibição `America/Sao_Paulo`)
- Comportamento sob requisições simultâneas
- Status igual ao atual → retorna `400`, sem registro de histórico
- Histórico na criação → **sim**, registra status "Criado" com `StatusAnterior = null`
- Pontos fora do escopo e como abordaríamos

**Tarefas:**
- [x] Configurar Swagger com XMLComments e exemplos
- [x] Escrever README completo
- [x] Revisar todos os endpoints contra os requisitos do PDF
- [x] Verificar que não há nenhum dado calculado aceito do cliente (ValorTotal, PreçoUnitário)
- [x] Testar fluxo completo manualmente via Swagger UI

**Commit:** `docs: add README with technical decisions and API documentation`

---

## Resumo das Etapas

| # | Etapa | Commits esperados |
|---|---|---|
| 1 | Estrutura do projeto | 1 |
| 2 | Domínio e entidades | 1 |
| 3 | EF Core e migrations | 1 |
| 4 | Clientes | 1 |
| 5 | Produtos e estoque | 1 |
| 6 | Pedidos (criação + status) | 2 |
| 7 | Middleware, erros e paginação | 1 |
| 8 | Concorrência e locking | 1 |
| 9 | Testes automatizados | 2 |
| 10 | Swagger e README | 1 |
| **Total** | | **~12 commits** |
