
using APIRESTful.Models;

namespace APIRESTful.Services;

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