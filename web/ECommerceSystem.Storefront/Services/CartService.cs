using System.Text.Json;

namespace ECommerceSystem.Storefront.Services;

/// <summary>One cart line: product id and quantity only - never a price.</summary>
public sealed record CartLine(int ProductId, int Quantity);

/// <summary>Name and email remembered after checkout or an order lookup (pre-fills forms, shown in the sidebar).</summary>
public sealed record RememberedCustomer(string Name, string Email);

/// <summary>
/// Shopping cart kept in the ASP.NET Core session (server side; the browser only holds the
/// ".ECommerceSystem.Session" cookie). Prices are never stored: they are read live from the Catalog, and the
/// Order Service validates them again when the order is placed.
/// Limits mirror contracts\orders.yaml: at most 20 lines and 1..100 units per product.
/// </summary>
public sealed class CartService(IHttpContextAccessor httpContextAccessor)
{
    public const int MaxLines = 20;
    public const int MaxQuantityPerProduct = 100;

    private const string CartKey = "cart.v1";
    private const string CustomerKey = "customer.v1";

    private ISession Session => httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("The cart can only be used during an HTTP request.");

    public IReadOnlyList<CartLine> GetLines() => Read<List<CartLine>>(CartKey) ?? [];

    public int ItemCount => GetLines().Sum(l => l.Quantity);

    /// <summary>Adds units of a product. Returns null on success, or the reason it was refused.</summary>
    public string? Add(int productId, int quantity)
    {
        if (quantity < 1)
        {
            return "Quantity must be at least 1.";
        }

        var lines = GetLines().ToList();
        var index = lines.FindIndex(l => l.ProductId == productId);

        if (index < 0)
        {
            if (lines.Count >= MaxLines)
            {
                return $"Your cart can hold at most {MaxLines} different products.";
            }

            if (quantity > MaxQuantityPerProduct)
            {
                return $"You can order at most {MaxQuantityPerProduct} units of one product.";
            }

            lines.Add(new CartLine(productId, quantity));
        }
        else
        {
            var newQuantity = lines[index].Quantity + quantity;
            if (newQuantity > MaxQuantityPerProduct)
            {
                return $"You can order at most {MaxQuantityPerProduct} units of one product (you already have {lines[index].Quantity}).";
            }

            lines[index] = lines[index] with { Quantity = newQuantity };
        }

        Write(CartKey, lines);
        return null;
    }

    /// <summary>Sets the quantity of a line; 0 or less removes it, more than 100 is capped at 100.</summary>
    public void SetQuantity(int productId, int quantity)
    {
        var lines = GetLines().ToList();
        var index = lines.FindIndex(l => l.ProductId == productId);
        if (index < 0)
        {
            return;
        }

        if (quantity <= 0)
        {
            lines.RemoveAt(index);
        }
        else
        {
            lines[index] = lines[index] with { Quantity = Math.Min(quantity, MaxQuantityPerProduct) };
        }

        Write(CartKey, lines);
    }

    public void Remove(int productId) => SetQuantity(productId, 0);

    public void Clear() => Session.Remove(CartKey);

    public RememberedCustomer? GetCustomer() => Read<RememberedCustomer>(CustomerKey);

    public void RememberCustomer(string name, string email) => Write(CustomerKey, new RememberedCustomer(name, email));

    public void ForgetCustomer() => Session.Remove(CustomerKey);

    private T? Read<T>(string key) where T : class
    {
        var json = Session.GetString(key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            Session.Remove(key);
            return null;
        }
    }

    private void Write<T>(string key, T value) => Session.SetString(key, JsonSerializer.Serialize(value));
}