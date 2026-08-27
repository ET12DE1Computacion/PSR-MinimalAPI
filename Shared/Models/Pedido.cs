using System;
using System.Collections.Generic;

namespace Shared.Models;

/// <summary>
/// Representa un pedido de comida, controlando las transiciones de estado de forma segura.
/// </summary>
public class Pedido
{
    public int Id { get; set; }
    public string Cliente { get; }
    public string Direccion { get; }
    public List<ItemPedido> Items { get; }
    public EstadoPedido Estado { get; private set; }

    public Pedido(int id, string cliente, string direccion, List<ItemPedido> items)
    {
        if (string.IsNullOrWhiteSpace(cliente))
        {
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(cliente));
        }

        if (string.IsNullOrWhiteSpace(direccion))
        {
            throw new ArgumentException("La dirección de entrega es obligatoria.", nameof(direccion));
        }

        if (items == null || items.Count == 0)
        {
            throw new ArgumentException("El pedido debe contener al menos un ítem.", nameof(items));
        }

        Id = id;
        Cliente = cliente;
        Direccion = direccion;
        Items = items;
        Estado = EstadoPedido.Pendiente;
    }

    public void IniciarPreparacion()
    {
        ValidarTransicion(EstadoPedido.Pendiente, EstadoPedido.EnCocina);
        Estado = EstadoPedido.EnCocina;
    }

    public void MarcarComoListo()
    {
        ValidarTransicion(EstadoPedido.EnCocina, EstadoPedido.ListoParaReparto);
        Estado = EstadoPedido.ListoParaReparto;
    }

    public void EnviarAReparto()
    {
        ValidarTransicion(EstadoPedido.ListoParaReparto, EstadoPedido.EnReparto);
        Estado = EstadoPedido.EnReparto;
    }

    public void Entregar()
    {
        ValidarTransicion(EstadoPedido.EnReparto, EstadoPedido.Entregado);
        Estado = EstadoPedido.Entregado;
    }

    private void ValidarTransicion(EstadoPedido estadoActualEsperado, EstadoPedido nuevoEstado)
    {
        if (Estado != estadoActualEsperado)
        {
            throw new InvalidOperationException(
                $"Transición inválida: No se puede cambiar el pedido #{Id} al estado '{nuevoEstado}' " +
                $"porque su estado actual es '{Estado}' y se esperaba '{estadoActualEsperado}'.");
        }
    }
}
