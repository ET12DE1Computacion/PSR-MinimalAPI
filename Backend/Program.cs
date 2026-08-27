using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Backend.Repositories;
using Backend.Sockets;
using Backend.Endpoints;
using Scalar.AspNetCore; // <-- NUEVO 1: Importamos la librería de Scalar

var builder = WebApplication.CreateBuilder(args);

// --- NUEVO 2: Configuramos el generador del documento técnico (OpenAPI) ---
builder.Services.AddOpenApi(); 
// --------------------------------------------------------------------------

// Registro de repositorios y servicios de negocio
builder.Services.AddSingleton<IPedidoRepository, InMemoryPedidoRepository>();

// Registro de servicio de Sockets TCP para Cocina y Reparto
builder.Services.AddSingleton<SocketServerService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<SocketServerService>());

var app = builder.Build();

// --- NUEVO 3: Encendemos los endpoints de la interfaz gráfica ---
app.MapOpenApi(); // Crea un archivo .json oculto con la estructura de tu API
app.MapScalarApiReference(); // Lee ese .json y te crea la página web visual
// ----------------------------------------------------------------

// Endpoint de prueba rápido
app.MapGet("/", () => "API de Gestión de Pedidos - Servidor en línea.");

// Mapear los endpoints modularizados
app.MapPedidoEndpoints();

app.Run();