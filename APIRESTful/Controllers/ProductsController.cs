
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
