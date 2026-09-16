# APIRESTful

> Projeto de nível básico para demonstrar os conceitos de uma API RESTful,
> usando **VSCode**, **.NET 10** e **C#**.
>
> Diretriz REST, de Roy Fielding

> https://www.linkedin.com/in/royfielding/
> https://www.reddit.com/r/programacao/comments/1nbstr1/pare_de_chamar_tudo_de_restful_o_apelo_de_roy/

---

## 1. Criação do projeto, solution e pacotes

```bash
#> DIRETRIZ REST: Client-Server — separa o servidor (API) do(s) cliente(s) que irão
#> consumi-la, permitindo que evoluam de forma independente

#criar pata para o projeto e acessar
mkdir APIRESTful
cd APIRESTful

# criar a solution vazia
dotnet new sln -n slnAPIRESTful

# criar Web API com controllers
dotnet new webapi -n APIRESTful -controllers -o APIRESTful

# adicionar o projeto à solution
dotnet sln add APIRESTful/APIRESTful.csproj

# Acessar a pasta do seu projeto
cd APIRESTful

# pacote swagger
#> DIRETRIZ REST: Interface Uniforme (mensagens autodescritivas) — Swagger documenta
#> o contrato da API de forma que o próprio serviço se autodescreve para o cliente
dotnet add package Swashbuckle.AspNetCore 

# adicionar suporte a versionamento de API (parte da Interface Uniforme)
#> DIRETRIZ REST: Interface Uniforme — versionamento garante que a identificação
#> do recurso (via URI, ex: /api/v1/...) permaneça estável e explícita entre evoluções da API
dotnet add package Asp.Versioning.Mvc
dotnet add package Asp.Versioning.Mvc.ApiExplorer

# acessar o projeto no VSCode
code .

# executar via terminal 
dotnet run

# acessar documentacao 'sem swagger' via /openapi/v1.json
#> DIRETRIZ REST: Interface Uniforme (mensagens autodescritivas) — o documento OpenAPI
#> nativo já descreve os recursos disponíveis, mesmo antes do Swagger UI existir
```

---

## 2. Ajustes no `Program.cs` — Swagger e Versionamento

```csharp
# ajustes na program.cs para swagger e versionamento
+ using Asp.Versioning;

  var builder = WebApplication.CreateBuilder(args);

  // Add services to the container.
  builder.Services.AddControllers();

  // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
  builder.Services.AddOpenApi();

+ // Swagger UI (documentação interativa complementar ao OpenAPI nativo)
+ // DIRETRIZ REST: Interface Uniforme — mensagens autodescritivas (contrato explorável)
+ builder.Services.AddEndpointsApiExplorer();
+ builder.Services.AddSwaggerGen();
+
+ // Versionamento de API
+ // DIRETRIZ REST: Interface Uniforme — identificação de recursos por versão na URI
+ builder.Services.AddApiVersioning(options =>
+ {
+     options.DefaultApiVersion = new ApiVersion(1, 0);
+     options.AssumeDefaultVersionWhenUnspecified = true;
+     options.ReportApiVersions = true;
+ }).AddMvc().AddApiExplorer(options =>
+ {
+     options.GroupNameFormat = "'v'VVV";
+     options.SubstituteApiVersionInUrl = true;
+ });

  var app = builder.Build();

  // Configure the HTTP request pipeline.
  if (app.Environment.IsDevelopment())
  {
      app.MapOpenApi();
+     app.UseSwagger();
+     app.UseSwaggerUI();
  }

  // DIRETRIZ REST: Sistema em Camadas — middlewares (pipeline HTTP) atuam como
  // camadas intermediárias entre a requisição do cliente e a lógica final do controller
  app.UseHttpsRedirection();
  app.UseAuthorization();
  app.MapControllers();
  app.Run();
```

```bash
# executar via terminal 
dotnet run
```

---

## 3. Criação de pastas `Models` e `Dtos`

```bash
# criar pastas
mkdir Models
mkdir Dtos
```

### `Models/Product.cs`

```bash
# criar os arquivos
type nul > Models\Product.cs
```

```csharp
namespace APIRESTful.Models;

// DIRETRIZ REST: Interface Uniforme — este é o RECURSO em sua forma interna (estado real),
// que nunca é exposto diretamente ao cliente; ele é sempre traduzido em uma REPRESENTAÇÃO (DTO)
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### `Dtos/ProductDtos.cs`

```bash
# criar os arquivos
type nul > Dtos\ProductDtos.cs
```

```csharp
namespace APIRESTful.Dtos;

// diretrizes REST (Interface Uniforme → manipulação de recursos através de representações)
// Usado no POST — dados que o cliente envia para criar um produto
public record ProductCreateDto(string Name, decimal Price);

// Usado no PUT — dados que o cliente envia para atualizar um produto
public record ProductUpdateDto(string Name, decimal Price);

// Usado nas respostas GET/POST/PUT — o que o cliente recebe de volta
// DIRETRIZ REST: Interface Uniforme — HATEOAS (campo Links abaixo permite que o cliente
// navegue pela API dinamicamente, sem conhecer as URIs de antemão)
public record ProductResponseDto(
    int Id,
    string Name,
    decimal Price,
    DateTime UpdatedAt,
    List<LinkDto> Links
);

// Representa um link de navegação (HATEOAS) dentro da resposta
//forma resumida de declarar o record com propriedades já prontas
//public record é um tipo imutável do C# usado para representar dados (não comportamento) com igualdade por valor automática — use-o em DTOs, mensagens e qualquer objeto que só transporta informação
// DIRETRIZ REST: Interface Uniforme — HATEOAS (Hypermedia As The Engine Of Application State)
public record LinkDto(string Rel, string Href, string Method);
```

```bash
# build - validar se esta ok
dotnet build
```

---

## 4. Repositório em memória (`Services`)

```bash
# repositorio em memória
mkdir Services
type nul > Services\IProductRepository.cs
```

### `Services/IProductRepository.cs`

```csharp
using APIRESTful.Models;

namespace APIRESTful.Services;

// DIRETRIZ REST: Stateless — o contrato do repositório trabalha apenas com o ESTADO
// DO RECURSO (produtos), nunca com estado de sessão/conversação do cliente
public interface IProductRepository
{
    IEnumerable<Product> GetAll();
    Product? GetById(int id);
    Product Add(Product product);
    bool Update(Product product);
    bool Delete(int id);
}
```

### `Services/ProductRepository.cs`

```bash
# Repositorio em memória
type nul > Services\ProductRepository.cs
```

```csharp
using APIRESTful.Models;

namespace APIRESTful.Services;

// DIRETRIZ REST: Stateless — cada chamada aos métodos abaixo é autocontida;
// o servidor guarda o estado do RECURSO (List<Product>), não o estado do CLIENTE
// (nenhuma informação de "quem pediu" ou "o que foi pedido antes" é armazenada aqui)
public class ProductRepository : IProductRepository
{
    private readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Teclado Mecânico", Price = 350.00m },
        new Product { Id = 2, Name = "Mouse Gamer", Price = 180.00m }
    };

    private int _nextId = 3;

    public IEnumerable<Product> GetAll() => _products;

    public Product? GetById(int id) =>
        _products.FirstOrDefault(p => p.Id == id);

    public Product Add(Product product)
    {
        product.Id = _nextId++;
        product.UpdatedAt = DateTime.UtcNow;
        _products.Add(product);
        return product;
    }

    public bool Update(Product product)
    {
        var existing = GetById(product.Id);
        if (existing is null) return false;

        existing.Name = product.Name;
        existing.Price = product.Price;
        existing.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public bool Delete(int id)
    {
        var existing = GetById(id);
        if (existing is null) return false;

        _products.Remove(existing);
        return true;
    }
}
```

### Registrar o repositório em `Program.cs`

```csharp
# registrar na program.cs, adicionar:

+ using APIRESTful.Services;
  using Asp.Versioning;

  var builder = WebApplication.CreateBuilder(args);

  // Add services to the container.

+ // DIRETRIZ REST: Stateless — Singleton mantém o ESTADO DO RECURSO vivo entre requisições
+ // (não confundir com estado de sessão do cliente, que o REST proíbe manter no servidor)
+ builder.Services.AddSingleton<IProductRepository, ProductRepository>();

  builder.Services.AddControllers();

  // ... resto igual
```

---

## 5. Criação do `ProductsController`

```bash
# criar controllers
type nul > Controllers\ProductsController.cs
```

```bash
# apagar template
del Controllers\WeatherForecastController.cs
del WeatherForecast.cs
```

### Conteúdo do `Controllers/ProductsController.cs`

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using APIRESTful.Dtos;
using APIRESTful.Models;
using APIRESTful.Services;

namespace APIRESTful.Controllers;

[ApiController]
[ApiVersion("1.0")]
// DIRETRIZ REST: Interface Uniforme — identificação de recursos por URI
// (a versão faz parte da própria identificação do recurso)
[Route("api/v{version:apiVersion}/products")]
// DIRETRIZ REST: Interface Uniforme — mensagens autodescritivas
// (o cliente sabe, pelo header Content-Type, que a resposta é JSON)
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    // DIRETRIZ REST: Client-Server — o controller depende apenas da abstração
    // (IProductRepository), sem conhecer detalhes de como os dados são persistidos
    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    // GET api/v1/products
    [HttpGet]
    // DIRETRIZ REST: Cacheable — resposta pode ser armazenada em cache por 30s
    [ResponseCache(Duration = 30)]
    public ActionResult<IEnumerable<ProductResponseDto>> GetAll()
    {
        var products = _repository.GetAll().Select(ToDto);
        return Ok(products); // 200 OK
    }

    // GET api/v1/products/1
    [HttpGet("{id:int}")]
    // DIRETRIZ REST: Cacheable
    [ResponseCache(Duration = 30)]
    public ActionResult<ProductResponseDto> GetById(int id)
    {
        var product = _repository.GetById(id);
        if (product is null)
            return NotFound(new { message = $"Produto {id} não encontrado" }); // 404

        // DIRETRIZ REST: Cacheable — ETag permite cache condicional
        // (cliente pode enviar If-None-Match em requisições futuras)
        var etag = $"\"{product.UpdatedAt.Ticks}\"";
        Response.Headers.ETag = etag;

        return Ok(ToDto(product)); // 200 OK
    }

    // POST api/v1/products
    [HttpPost]
    public ActionResult<ProductResponseDto> Create([FromBody] ProductCreateDto dto)
    {
        // DIRETRIZ REST: Interface Uniforme — manipulação de recursos através de
        // representações (o cliente envia um DTO, nunca a entidade interna Product)
        var product = new Product { Name = dto.Name, Price = dto.Price };
        var created = _repository.Add(product);

        // DIRETRIZ REST: Interface Uniforme — mensagens autodescritivas
        // (201 Created + header Location apontando para o novo recurso)
        return CreatedAtAction(nameof(GetById), new { id = created.Id, version = "1.0" }, ToDto(created));
    }

    // PUT api/v1/products/1
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] ProductUpdateDto dto)
    {
        var product = new Product { Id = id, Name = dto.Name, Price = dto.Price };
        var updated = _repository.Update(product);

        if (!updated) return NotFound(); // 404
        return NoContent(); // 204 — mensagem autodescritiva: atualizado, sem corpo de retorno
    }

    // DELETE api/v1/products/1
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var deleted = _repository.Delete(id);
        if (!deleted) return NotFound(); // 404
        return NoContent(); // 204
    }

    // DIRETRIZ REST: Interface Uniforme — HATEOAS
    // (a resposta inclui os links de navegação disponíveis para aquele recurso,
    // permitindo que o cliente descubra ações possíveis sem "adivinhar" URIs)
    private ProductResponseDto ToDto(Product p)
    {
        var links = new List<LinkDto>
        {
            new("self",   Url.Action(nameof(GetById), new { id = p.Id, version = "1.0" })!, "GET"),
            new("update", Url.Action(nameof(Update),   new { id = p.Id, version = "1.0" })!, "PUT"),
            new("delete", Url.Action(nameof(Delete),   new { id = p.Id, version = "1.0" })!, "DELETE"),
        };

        return new ProductResponseDto(p.Id, p.Name, p.Price, p.UpdatedAt, links);
    }
}
```

### Ajuste em `Program.cs` — habilitar cache

```csharp
# Ajuste na program.cs
  builder.Services.AddOpenApi();
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen();
+ // DIRETRIZ REST: Cacheable — habilita o middleware que aplica os headers de cache
+ builder.Services.AddResponseCaching();
```

---

## 6. Resumo — diretrizes REST aplicadas neste projeto

| Diretriz REST | Onde está aplicada |
|---|---|
| **Client-Server** | Separação entre `Controllers` (servidor) e qualquer cliente HTTP que consome a API |
| **Stateless** | Nunca gera sessão do cliente |
| **Cacheable** | `[ResponseCache(Duration = 30)]`, `AddResponseCaching()`, header `ETag` manual |
| **Interface Uniforme** — identificação de recursos | Rotas `/api/v{version}/products/{id}` |
| — manipulação via representações | DTOs (`ProductCreateDto`, `ProductUpdateDto`, `ProductResponseDto`) |
| — mensagens autodescritivas | `[Produces("application/json")]`, status codes corretos, Swagger/OpenAPI |
| — HATEOAS | é a prática de incluir, dentro da própria resposta da API, os links das ações possíveis para aquele recurso — serve para o cliente descobrir "o que posso fazer a partir daqui" sem precisar ter as URLs fixas no código dele, como fizemos com o Links (self, update, delete) no ProductResponseDto. |
| **Sistema em Camadas** | Middlewares (`UseResponseCaching`, `UseHttpsRedirection`, versionamento via `Asp.Versioning`) |

