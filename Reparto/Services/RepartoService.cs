using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shared.Models;

namespace Reparto.Services;

/// <summary>
/// Proporciona el servicio y la lógica de negocio para la conexión TCP y procesamiento de entrega de pedidos.
/// </summary>
public class RepartoService
{
    private const string Host = "127.0.0.1";
    private const int Puerto = 8888;

    public async Task IniciarAsync()
    {
        Console.Title = "Servicio de Reparto 🛵";
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("========================================");
        Console.WriteLine("     INICIANDO SERVICIO DE REPARTO      ");
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
                await writer.WriteLineAsync("REGISTRO:REPARTO");
                Console.WriteLine("Registrado en el Backend como: REPARTO.");

                // Escuchar pedidos entrantes listos para entrega
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
                        await ProcesarEntregaAsync(pedido, writer);
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

    private async Task ProcesarEntregaAsync(Pedido pedido, StreamWriter writer)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"────────────────────────────────────────");
        Console.WriteLine($"🛵 PEDIDO LISTO PARA REPARTO: Pedido #{pedido.Id}");
        Console.WriteLine($"👤 Cliente  : {pedido.Cliente}");
        Console.WriteLine($"📍 Destino  : {pedido.Direccion}");
        Console.WriteLine($"────────────────────────────────────────");
        Console.ResetColor();

        Console.Write("🛵 Repartidor en viaje");
        for (int i = 0; i < 6; i++)
        {
            await Task.Delay(1000);
            Console.Write(".");
        }
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ ¡Pedido #{pedido.Id} entregado al cliente!");
        Console.ResetColor();

        await writer.WriteLineAsync($"ENTREGADO:{pedido.Id}");
        Console.WriteLine($"Notificación enviada al Backend: ENTREGADO:{pedido.Id}");
    }
}
