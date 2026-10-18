using BuildingBlocks.Middleware; using System.Net.Http.Json;
var builder = WebApplication.CreateBuilder(args); builder.AddServiceDefaults();
builder.Services.AddHttpClient("catalog", c => c.BaseAddress = new Uri("http://catalog-api"));
builder.Services.AddHttpClient("inventory", c => c.BaseAddress = new Uri("http://inventory-api"));
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build(); app.UseMiddleware<CorrelationIdMiddleware>(); app.MapDefaultEndpoints();
app.MapGet("/product-details/{id:int}", async (int id, IHttpClientFactory f, HttpContext ctx, CancellationToken ct) => {
 var corr = ctx.GetHttpCorrelationId(); var cat = f.CreateClient("catalog"); var inv = f.CreateClient("inventory");
 cat.DefaultRequestHeaders.Add("X-Correlation-Id", corr); inv.DefaultRequestHeaders.Add("X-Correlation-Id", corr);
 var cTask = cat.GetFromJsonAsync<CatalogDto>($"/catalog/{id}", ct); var iTask = inv.GetFromJsonAsync<InventoryDto>($"/inventory/{id}", ct);
 await Task.WhenAll(cTask, iTask); var catalog = await cTask; if (catalog == null) return Results.NotFound();
 var stock = await iTask; return Results.Ok(new { catalog.Id, catalog.Name, catalog.Price, Stock = stock?.stock ?? 0, InStock = (stock?.stock ?? 0) > 0, CorrelationId = corr });
});
app.MapReverseProxy(); app.Run();
public record CatalogDto(int Id,string Name,decimal Price); public record InventoryDto(int productId,int stock,string correlationId);
