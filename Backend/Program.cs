using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Backend.Repositories;
using Backend.Sockets;
using Backend.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Registro de repositorios y servicios de negocio
builder.Services.AddSingleton<IPedidoRepository, InMemoryPedidoRepository>();

// Registro de servicio de Sockets TCP para Cocina y Reparto
builder.Services.AddSingleton<SocketServerService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<SocketServerService>());

var app = builder.Build();

// Endpoint de prueba rápido
app.MapGet("/", () => "API de Gestión de Pedidos - Servidor en línea.");

// Mapear los endpoints modularizados
app.MapPedidoEndpoints();

app.Run();
