namespace APIRESTful.Dtos;

// diretrizes REST (Interface Uniforme → manipulação de recursos através de representações)
// Usado no POST — dados que o cliente envia para criar um produto
public record ProductCreateDto(string Name, decimal Price);

// Usado no PUT — dados que o cliente envia para atualizar um produto
public record ProductUpdateDto(string Name, decimal Price);

// Usado nas respostas GET/POST/PUT — o que o cliente recebe de volta
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
public record LinkDto(string Rel, string Href, string Method);
