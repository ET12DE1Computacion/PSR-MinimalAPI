using System.Collections.Generic;

namespace Backend.DTOs;

/// <summary>
/// DTO para la solicitud de creación de un nuevo pedido.
/// </summary>
public record CrearPedidoRequest(string Cliente, string Direccion, List<ItemRequest> Items);

/// <summary>
/// DTO que representa un ítem dentro de la solicitud de creación del pedido.
/// </summary>
public record ItemRequest(string NombrePlato, int Cantidad);
