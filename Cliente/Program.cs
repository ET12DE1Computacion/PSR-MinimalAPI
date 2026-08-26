using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Shared.Models;
using Cliente.DTOs;
using Cliente.Services;

namespace Cliente;

class Program
{
    private static readonly PedidoClientService ClientService = new(new HttpClient());

    static async Task Main(string[] args)
    {
        Console.Title = "Cliente - Sistema de Pedidos 🍔";

        bool salir = false;
        while (!salir)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("    SISTEMA DE PEDIDOS - PANEL CLIENTE  ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine("1. Crear nuevo pedido (Interactivo)");
            Console.WriteLine("2. Crear pedido rápido y seguimiento en vivo");
            Console.WriteLine("3. Consultar estado de un pedido por ID");
            Console.WriteLine("4. Ver lista de todos los pedidos");
            Console.WriteLine("5. Salir");
            Console.Write("\nSeleccione una opción: ");

            string? opcion = Console.ReadLine();
            switch (opcion)
            {
                case "1":
                    await CrearPedidoInteractivoAsync();
                    break;
                case "2":
                    await CrearPedidoRapidoConSeguimientoAsync();
                    break;
                case "3":
                    await ConsultarPedidoPorIdAsync();
                    break;
                case "4":
                    await ListarTodosLosPedidosAsync();
                    break;
                case "5":
                    salir = true;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Opción no válida. Intente de nuevo.");
                    Console.ResetColor();
                    EsperarTecla();
                    break;
            }
        }
    }

    private static async Task CrearPedidoInteractivoAsync()
    {
        Console.Clear();
        Console.WriteLine("--- NUEVO PEDIDO INTERACTIVO ---");
        
        Console.Write("Ingrese su nombre: ");
        string? cliente = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(cliente))
        {
            Console.WriteLine("El nombre no puede estar vacío.");
            EsperarTecla();
            return;
        }

        Console.Write("Ingrese su dirección de entrega: ");
        string? direccion = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(direccion))
        {
            Console.WriteLine("La dirección no puede estar vacía.");
            EsperarTecla();
            return;
        }

        var items = new List<ItemDto>();
        bool agregarMas = true;
        while (agregarMas)
        {
            Console.Write("Nombre del plato/bebida: ");
            string? plato = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(plato)) break;

            Console.Write("Cantidad: ");
            if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0)
            {
                Console.WriteLine("Cantidad no válida. Debe ser mayor a 0.");
                continue;
            }

            items.Add(new ItemDto(plato, cantidad));

            Console.Write("¿Desea agregar otro ítem? (s/n): ");
            string? respuesta = Console.ReadLine()?.ToLower();
            agregarMas = respuesta == "s" || respuesta == "si";
        }

        if (items.Count == 0)
        {
            Console.WriteLine("No se agregaron ítems. Cancelando pedido.");
            EsperarTecla();
            return;
        }

        var requestDto = new CrearPedidoRequestDto(cliente, direccion, items);
        
        var (exito, mensaje, _) = await ClientService.EnviarPedidoAsync(requestDto);
        if (exito)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n{mensaje}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nFallo al enviar pedido: {mensaje}");
        }
        Console.ResetColor();
        EsperarTecla();
    }

    private static async Task CrearPedidoRapidoConSeguimientoAsync()
    {
        Console.Clear();
        Console.WriteLine("--- CREANDO PEDIDO RÁPIDO ---");

        var items = new List<ItemDto>
        {
            new("Hamburguesa Completa", 1),
            new("Papas Fritas Medianas", 1),
            new("Gaseosa Cola 500ml", 1)
        };

        var requestDto = new CrearPedidoRequestDto("Alumno Técnico", "Pasaje de la Escuela 123", items);
        
        var (exito, mensaje, pedidoCreado) = await ClientService.EnviarPedidoAsync(requestDto);
        if (!exito || pedidoCreado == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nNo se pudo crear el pedido: {mensaje}");
            Console.ResetColor();
            EsperarTecla();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n{mensaje}");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Iniciando seguimiento automático del pedido...");
        Console.ResetColor();

        int pedidoId = pedidoCreado.Id;
        EstadoPedido? ultimoEstado = null;

        while (ultimoEstado != EstadoPedido.Entregado)
        {
            await Task.Delay(2000);
            
            var pedidoActualizado = await ClientService.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedidoActualizado == null)
            {
                Console.WriteLine("No se pudo obtener información del pedido. Reintentando...");
                continue;
            }

            if (ultimoEstado != pedidoActualizado.Estado)
            {
                ultimoEstado = pedidoActualizado.Estado;
                MostrarCambioEstado(pedidoId, ultimoEstado.Value);
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n🎉 ¡El pedido ha finalizado con éxito! Disfrute de su comida.");
        Console.ResetColor();
        EsperarTecla();
    }

    private static async Task ConsultarPedidoPorIdAsync()
    {
        Console.Clear();
        Console.WriteLine("--- CONSULTAR PEDIDO ---");
        Console.Write("Ingrese el ID del pedido a buscar: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID no válido.");
            EsperarTecla();
            return;
        }

        var pedido = await ClientService.ObtenerPedidoPorIdAsync(id);
        if (pedido != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nPedido #{pedido.Id} encontrado:");
            Console.ResetColor();
            Console.WriteLine($"👤 Cliente  : {pedido.Cliente}");
            Console.WriteLine($"📍 Dirección: {pedido.Direccion}");
            Console.WriteLine($"🔄 Estado   : {pedido.Estado}");
            Console.WriteLine("📋 Ítems:");
            foreach (var item in pedido.Items)
            {
                Console.WriteLine($"   - {item.Cantidad}x {item.NombrePlato}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nNo se encontró el pedido #{id}.");
            Console.ResetColor();
        }
        EsperarTecla();
    }

    private static async Task ListarTodosLosPedidosAsync()
    {
        Console.Clear();
        Console.WriteLine("--- LISTA DE TODOS LOS PEDIDOS ---");

        var pedidos = await ClientService.ObtenerTodosLosPedidosAsync();
        if (pedidos == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error de comunicación con el Backend al recuperar la lista.");
            Console.ResetColor();
        }
        else if (pedidos.Count == 0)
        {
            Console.WriteLine("No hay pedidos registrados en el sistema.");
        }
        else
        {
            foreach (var p in pedidos)
            {
                Console.WriteLine($"ID: {p.Id.ToString().PadRight(4)} | Cliente: {p.Cliente.PadRight(15)} | Estado: {p.Estado}");
            }
        }
        EsperarTecla();
    }

    private static void MostrarCambioEstado(int id, EstadoPedido estado)
    {
        string emoji = estado switch
        {
            EstadoPedido.Pendiente => "⏳",
            EstadoPedido.EnCocina => "🍳",
            EstadoPedido.ListoParaReparto => "📦",
            EstadoPedido.EnReparto => "🛵",
            EstadoPedido.Entregado => "✅",
            _ => "❓"
        };

        Console.Write($"[{DateTime.Now:HH:mm:ss}] Pedido #{id}: ");
        switch (estado)
        {
            case EstadoPedido.Pendiente:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case EstadoPedido.EnCocina:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case EstadoPedido.ListoParaReparto:
                Console.ForegroundColor = ConsoleColor.Blue;
                break;
            case EstadoPedido.EnReparto:
                Console.ForegroundColor = ConsoleColor.Magenta;
                break;
            case EstadoPedido.Entregado:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
        }
        Console.WriteLine($"{emoji} {estado.ToString().ToUpper()}");
        Console.ResetColor();
    }

    private static void EsperarTecla()
    {
        Console.WriteLine("\nPresione cualquier tecla para continuar...");
        Console.ReadKey();
    }
}
