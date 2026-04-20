using System.Reflection;
using System.Text.Json;
using CompaniesApi.Services;
using CompaniesApi.Services.DataSources;
using CompaniesApi.Services.Parsers;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options => 
    {
        options.ValidateScopes = true; // a service must not depend on somthing with a shorter lifetime than itself
        options.ValidateOnBuild = true; // captures during build instead of runtime
    }
);

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    
    if(File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var upstreamBaseUrl = builder.Configuration["UpstreamBaseUrl"]
                      ?? throw new InvalidOperationException("UpstreamBaseUrl is not configured.");

builder.Services.AddHttpClient<HttpCompanyDataSource>(client => { client.BaseAddress = new Uri(upstreamBaseUrl); })
    .AddStandardResilienceHandler();

builder.Services.AddSingleton<ICompanyDataSource>(sp => sp.GetRequiredService<HttpCompanyDataSource>());
builder.Services.AddScoped<ICompanyParser, XmlCompanyParser>();
builder.Services.AddScoped<ICompanyClient, CompanyClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;