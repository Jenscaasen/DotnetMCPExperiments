using AspNetApiSse.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure application services using extension methods
builder.Services.AddMCPLogging(builder.Logging);
builder.Services.AddMCPServices();
builder.Services.AddMCPCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();

// Enable serving static files from wwwroot
app.UseStaticFiles();

// Configure endpoints using extension methods
app.ConfigureMCPEndpoints();
app.ConfigureLegacyEndpoints();

app.Run();
