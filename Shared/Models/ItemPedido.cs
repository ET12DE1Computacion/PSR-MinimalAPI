using System;

namespace Shared.Models;

/// <summary>
/// Representa un ítem o plato dentro de un pedido, con su cantidad correspondiente.
/// Aplica inmutabilidad y encapsulamiento.
/// </summary>
public class ItemPedido
{
    public string NombrePlato { get; }
    public int Cantidad { get; }

    public ItemPedido(string nombrePlato, int cantidad)
    {
        if (string.IsNullOrWhiteSpace(nombrePlato))
        {
            throw new ArgumentException("El nombre del plato no puede estar vacío.", nameof(nombrePlato));
        }

        if (cantidad <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");
        }

        NombrePlato = nombrePlato;
        Cantidad = cantidad;
    }
}
