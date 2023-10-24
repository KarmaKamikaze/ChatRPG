namespace ChatRPG.API;

public interface IFoodWasteClient
{
    Task<List<FoodWasteResponse>> GetFoodwasteResponse(string zip);
}

public record Offer(string Currency, double Discount, string Ean, DateTime EndTime, DateTime LastUpdate,
    double NewPrice, double OriginalPrice, double PercentDiscount, DateTime StartTime, double Stock, string StockUnit);

public record Product(string Description, string Ean, string Image);

public record Clearance(Offer Offer, Product Product);

public record Address(string City, string Country, string Extra, string Street, string Zip);

public record Hour(DateOnly Date, string Type, DateTime Open, string Close, bool Closed, List<double> CustomerFlow);

public record Store(Address Address, string Brand, List<double> Coordinates, List<Hour> Hours, string Name, string Id, string Type);

public record FoodWasteResponse(List<Clearance> Clearances, Store Store);
