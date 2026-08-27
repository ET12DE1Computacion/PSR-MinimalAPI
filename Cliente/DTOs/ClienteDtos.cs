using System.Collections.Generic;
using Shared.Models;

namespace Cliente.DTOs;

/// <summary>
/// DTO local del cliente para la creación del pedido.
/// </summary>
public record CrearPedidoRequestDto(string Cliente, string Direccion, List<ItemDto> Items);

/// <summary>
/// DTO local que representa un plato y cantidad en el pedido del cliente.
/// </summary>
public record ItemDto(string NombrePlato, int Cantidad);

/// <summary>
/// DTO que mapea la respuesta devuelta por el Backend cuando la cocina está fuera de línea.
/// </summary>
public record RespuestaAceptadaDto(string Mensaje, Pedido Pedido);
