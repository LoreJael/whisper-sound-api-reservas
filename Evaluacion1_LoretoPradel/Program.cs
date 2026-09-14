using Microsoft.EntityFrameworkCore;
using Evaluacion1_LoretoPradel.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ReservasContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReservasContext") ?? throw new InvalidOperationException("Connection string 'ReservasContext' not found.")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
