using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Models;
using Backend.Repositories;
using Backend.Sockets;
using Backend.DTOs;

namespace Backend.Endpoints;

/// <summary>
/// Proporciona métodos de extensión para modularizar y registrar las rutas de la Minimal API.
/// </summary>
public static class PedidoEndpoints
{
    public static void MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos");

        // Obtener todos los pedidos
        group.MapGet("/", (IPedidoRepository repo) =>
        {
            return Results.Ok(repo.ObtenerTodos());
        });

        // Obtener un pedido por ID
        group.MapGet("/{id:int}", (int id, IPedidoRepository repo) =>
        {
            var pedido = repo.ObtenerPorId(id);
            return pedido is not null ? Results.Ok(pedido) : Results.NotFound($"El pedido #{id} no existe.");
        });

        // Crear un nuevo pedido
        group.MapPost("/", async (CrearPedidoRequest request, IPedidoRepository repo, SocketServerService socketServer) =>
        {
            try
            {
                var itemsDominio = request.Items
                    .Select(i => new ItemPedido(i.NombrePlato, i.Cantidad))
                    .ToList();

                var nuevoPedido = new Pedido(0, request.Cliente, request.Direccion, itemsDominio);
                var pedidoCreado = repo.Crear(nuevoPedido);

                bool enviado = await socketServer.EnviarACocinaAsync(pedidoCreado);

                if (enviado)
                {
                    return Results.Created($"/api/pedidos/{pedidoCreado.Id}", pedidoCreado);
                }
                else
                {
                    return Results.Json(new 
                    { 
                        Mensaje = "Pedido registrado, pero el servicio de Cocina no está disponible temporalmente. Se procesará cuando se conecte.",
                        Pedido = pedidoCreado
                    }, statusCode: StatusCodes.Status202Accepted);
                }
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        });

        // Reintentar cocinar un pedido pendiente
        group.MapPost("/{id:int}/cocinar", async (int id, IPedidoRepository repo, SocketServerService socketServer) =>
        {
            var pedido = repo.ObtenerPorId(id);
            if (pedido == null)
            {
                return Results.NotFound($"El pedido #{id} no existe.");
            }

            if (pedido.Estado != EstadoPedido.Pendiente)
            {
                return Results.BadRequest($"El pedido #{id} ya fue procesado o está en preparación. Estado actual: {pedido.Estado}");
            }

            bool enviado = await socketServer.EnviarACocinaAsync(pedido);
            return enviado 
                ? Results.Ok(new { Mensaje = "Pedido enviado a Cocina exitosamente.", Pedido = pedido })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });
    }
}
