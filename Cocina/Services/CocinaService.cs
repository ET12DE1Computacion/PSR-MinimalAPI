using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shared.Models;

namespace Cocina.Services;

/// <summary>
/// Proporciona el servicio y la lógica de negocio para la conexión TCP y procesamiento en la Cocina.
/// </summary>
public class CocinaService
{
    private const string Host = "127.0.0.1";
    private const int Puerto = 8888;

    public async Task IniciarAsync()
    {
        Console.Title = "Servicio de Cocina 🍳";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("========================================");
        Console.WriteLine("     INICIANDO SERVICIO DE COCINA       ");
        Console.WriteLine("========================================");
        Console.ResetColor();

        while (true)
        {
            try
            {
                Console.WriteLine($"Conectándose al servidor TCP en {Host}:{Puerto}...");
                using var client = new TcpClient();
                await client.ConnectAsync(Host, Puerto);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("¡Conexión establecida con el Backend!");
                Console.ResetColor();

                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                using var reader = new StreamReader(stream, Encoding.UTF8);

                // Registrar el servicio
                await writer.WriteLineAsync("REGISTRO:COCINA");
                Console.WriteLine("Registrado en el Backend como: COCINA.");

                // Escuchar pedidos entrantes
                while (client.Connected)
                {
                    string? jsonPedido = await reader.ReadLineAsync();
                    if (jsonPedido == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Se perdió la conexión con el Backend.");
                        Console.ResetColor();
                        break;
                    }

                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var pedido = JsonSerializer.Deserialize<Pedido>(jsonPedido, opciones);

                    if (pedido != null)
                    {
                        await ProcesarPedidoAsync(pedido, writer);
                    }
                }
            }
            catch (SocketException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No se pudo conectar al Backend. Reintentando en 3 segundos...");
                Console.ResetColor();
                await Task.Delay(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");
                await Task.Delay(3000);
            }
        }
    }

    private async Task ProcesarPedidoAsync(Pedido pedido, StreamWriter writer)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"────────────────────────────────────────");
        Console.WriteLine($"🍳 NUEVO PEDIDO RECIBIDO: Pedido #{pedido.Id}");
        Console.WriteLine($"👤 Cliente  : {pedido.Cliente}");
        Console.WriteLine($"📍 Dirección: {pedido.Direccion}");
        Console.WriteLine($"📋 Ítems del Pedido:");
        foreach (var item in pedido.Items)
        {
            Console.WriteLine($"   • {item.Cantidad}x {item.NombrePlato}");
        }
        Console.WriteLine($"────────────────────────────────────────");
        Console.ResetColor();

        Console.Write("👨‍🍳 Cocinando");
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(1000);
            Console.Write(".");
        }
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ ¡Pedido #{pedido.Id} terminado!");
        Console.ResetColor();

        await writer.WriteLineAsync($"LISTO:{pedido.Id}");
        Console.WriteLine($"Notificación enviada al Backend: LISTO:{pedido.Id}");
    }
}
