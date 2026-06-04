using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsDb")));

// RabbitMqPublisher como singleton con inicialización asíncrona
builder.Services.AddSingleton<RabbitMqPublisher>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return RabbitMqPublisher.CreateAsync(config).GetAwaiter().GetResult();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar migraciones automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();