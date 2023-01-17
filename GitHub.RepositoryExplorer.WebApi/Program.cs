[assembly: ApiController]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(
    options => options.AddPolicy(
        CorsPolicy.Name,
        policy => policy.WithOrigins("https://localhost:7299")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IssueCountService>();
builder.Services.AddResponseCaching();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddCosmosRepository();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCaching();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(CorsPolicy.Name);
app.MapControllers();
app.Run();
