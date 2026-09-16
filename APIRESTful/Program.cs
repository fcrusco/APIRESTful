using APIRESTful.Services;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

 // Swagger UI (documentação interativa complementar ao OpenAPI nativo)
 builder.Services.AddEndpointsApiExplorer();
 builder.Services.AddSwaggerGen();
 // DIRETRIZ REST: Cacheable — habilita o middleware que aplica os headers de cache
 builder.Services.AddResponseCaching();

 // Versionamento de API
 builder.Services.AddApiVersioning(options =>
 {
     options.DefaultApiVersion = new ApiVersion(1, 0);
     options.AssumeDefaultVersionWhenUnspecified = true;
     options.ReportApiVersions = true;
 }).AddMvc().AddApiExplorer(options =>
 {
     options.GroupNameFormat = "'v'VVV";
     options.SubstituteApiVersionInUrl = true;
 });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
