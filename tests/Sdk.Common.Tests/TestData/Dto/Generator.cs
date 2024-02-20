using Bogus;

namespace Sdk.Common.Tests.TestData.Dto;

public static class Generator
{
    public static Order GenerateOrder()
    {
        Randomizer.Seed = new Random(8675309);
        var customerFaker = new Faker<Customer>()
            .RuleFor(c => c.Id, f => f.Random.Int(1, 100))
            .RuleFor(c => c.Name, f => f.Person.FullName)
            .RuleFor(c => c.EMail, f => f.Person.Email)
            .RuleFor(c => c.Tel1, f => f.Person.Phone)
            .RuleFor(c => c.Tel2, f => f.Person.Phone)
            .RuleFor(c => c.VatId, f => f.Random.AlphaNumeric(10));
        var productFaker = new Faker<Product>()
            .RuleFor(p => p.Id, f => f.Random.AlphaNumeric(15))
            .RuleFor(p => p.Title, f => f.Commerce.ProductName())
            .RuleFor(p => p.Weight, f => f.Random.Int(1, 100))
            .RuleFor(p => p.Sku, f => f.Random.AlphaNumeric(10));
        var orderItemFaker = new Faker<OrderItem>()
            .RuleFor(oi => oi.OrderItemId, f => f.Random.Int(1, 100))
            .RuleFor(oi => oi.TransactionId, f => f.Random.AlphaNumeric(15))
            .RuleFor(oi => oi.Product, f => productFaker.Generate())
            .RuleFor(oi => oi.Quantity, f => f.Random.Int(1, 100))
            .RuleFor(oi => oi.TotalPrice, f => f.Random.Decimal(1, 100));
        var addressFaker = new Faker<Address>()
            .RuleFor(a => a.Street, f => f.Address.StreetAddress())
            .RuleFor(a => a.City, f => f.Address.City())
            .RuleFor(a => a.Zip, f => f.Address.ZipCode())
            .RuleFor(a => a.Country, f => f.Address.Country());
        var orderFaker = new Faker<Order>()
            .RuleFor(o => o.InvoiceNumber, f => f.Random.Int(1, 100))
            .RuleFor(o => o.InvoiceDate, f => f.Date.Past())
            .RuleFor(o => o.InvoiceAddress, f => addressFaker.Generate())
            .RuleFor(o => o.ShippingAddress, f => addressFaker.Generate())
            .RuleFor(o => o.Customer, f => customerFaker.Generate())
            .RuleFor(o => o.Items, f => f.Make(3, () => orderItemFaker.Generate()).ToArray());
        
        
        var order = orderFaker.Generate();
        return order;
    }
}