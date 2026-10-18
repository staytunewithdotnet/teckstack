using BuildingBlocks.Middleware; using Serilog;
Log.Logger=new LoggerConfiguration().WriteTo.Console().CreateLogger();
var b=WebApplication.CreateBuilder(args); b.AddServiceDefaults(); b.Host.UseSerilog(); b.Services.AddSingleton<CatalogStore>();
var app=b.Build(); app.UseMiddleware<CorrelationIdMiddleware>(); app.MapDefaultEndpoints();
app.MapGet("/catalog/{id:int}", (int id,CatalogStore s,HttpContext c)=> s.Get(id) is {} i ? Results.Ok(new{item=i,correlationId=c.GetHttpCorrelationId()}):Results.NotFound());
app.MapGet("/catalog",(CatalogStore s)=>Results.Ok(s.GetAll())); app.Run();
public record CatalogItem(int Id,string Name,decimal Price);
public class CatalogStore{private readonly Dictionary<int,CatalogItem> _d=new(){[1]=new(1,"Laptop",1200),[2]=new(2,"Mouse",25),[3]=new(3,"Keyboard",75)}; public CatalogItem? Get(int id)=>_d.TryGetValue(id,out var v)?v:null; public IEnumerable<CatalogItem> GetAll()=>_d.Values;}
