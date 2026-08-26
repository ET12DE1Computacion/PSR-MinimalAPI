namespace Shared.Models;

/// <summary>
/// Define los posibles estados de un pedido en su ciclo de vida.
/// </summary>
public enum EstadoPedido
{
    Pendiente,
    EnCocina,
    ListoParaReparto,
    EnReparto,
    Entregado
}
