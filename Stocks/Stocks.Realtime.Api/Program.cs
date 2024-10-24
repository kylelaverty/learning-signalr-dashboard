using Npgsql;
using Stocks.Realtime.Api;
using Stocks.Realtime.Api.Stocks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton(_ =>
{
    string connectionString = builder.Configuration.GetConnectionString("Database")!;

    var npgsqlDataSource = NpgsqlDataSource.Create(connectionString);

    return npgsqlDataSource;
});
builder.Services.AddHostedService<DatabaseInitializer>();

builder.Services.AddHttpClient<StocksClient>(httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration["Stocks:ApiUrl"]!);
});
builder.Services.AddScoped<StockService>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(policy => policy
        .WithOrigins(builder.Configuration["Cors:AllowedOrigin"]!)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
}

app.MapGet("/api/stocks/{ticker}", async (string ticker, StockService stockService) =>
{
    StockPriceResponse? result = await stockService.GetLatestStockPrice(ticker);

    return result is null
        ? Results.NotFound($"No stock data available for ticker: {ticker}")
        : Results.Ok(result);
})
.WithName("GetLatestStockPrice")
.WithOpenApi();

app.UseHttpsRedirection();

app.Run();
