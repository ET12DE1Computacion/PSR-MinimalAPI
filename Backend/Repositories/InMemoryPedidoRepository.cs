using System.Collections.Concurrent;
using System.Collections.Generic;
using Shared.Models;

namespace Backend.Repositories;

/// <summary>
/// Implementación del repositorio en memoria utilizando una estructura de datos segura para subprocesos.
/// </summary>
public class InMemoryPedidoRepository : IPedidoRepository
{
    private readonly ConcurrentDictionary<int, Pedido> _pedidos = new();
    private int _proximoId = 1;
    private readonly object _lock = new();

    public Pedido Crear(Pedido pedido)
    {
        lock (_lock)
        {
            pedido.Id = _proximoId++;
            _pedidos[pedido.Id] = pedido;
            return pedido;
        }
    }

    public Pedido? ObtenerPorId(int id)
    {
        _pedidos.TryGetValue(id, out var pedido);
        return pedido;
    }

    public IEnumerable<Pedido> ObtenerTodos()
    {
        return _pedidos.Values;
    }

    public void Actualizar(Pedido pedido)
    {
        // En memoria, al tratarse de referencias de objetos, al modificar el objeto se actualiza
        // automáticamente. Sin embargo, nos aseguramos de que esté en el diccionario.
        _pedidos[pedido.Id] = pedido;
    }
}
