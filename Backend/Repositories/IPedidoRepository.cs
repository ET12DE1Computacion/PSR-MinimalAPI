using System.Collections.Generic;
using Shared.Models;

namespace Backend.Repositories;

/// <summary>
/// Define las operaciones del repositorio para administrar los pedidos.
/// </summary>
public interface IPedidoRepository
{
    Pedido Crear(Pedido pedido);
    Pedido? ObtenerPorId(int id);
    IEnumerable<Pedido> ObtenerTodos();
    void Actualizar(Pedido pedido);
}
