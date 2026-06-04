using Microsoft.EntityFrameworkCore;
using BillingService.Data;
using BillingService.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BillingDb")));

// Worker que escucha RabbitMQ en background
builder.Services.AddHostedService<PaymentConsumerWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar migraciones automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();