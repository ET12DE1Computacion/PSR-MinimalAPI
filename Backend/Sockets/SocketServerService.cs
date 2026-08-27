using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Backend.Repositories;

namespace Backend.Sockets;

/// <summary>
/// Servicio de fondo que actúa como servidor TCP Sockets.
/// Gestiona la conexión de los servicios de Cocina y Reparto, y procesa sus mensajes en tiempo real.
/// </summary>
public class SocketServerService : BackgroundService
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly ILogger<SocketServerService> _logger;
    private readonly TcpListener _listener;
    
    private TcpClient? _cocinaClient;
    private TcpClient? _repartoClient;
    private readonly object _lock = new();

    public SocketServerService(IPedidoRepository pedidoRepository, ILogger<SocketServerService> logger)
    {
        _pedidoRepository = pedidoRepository;
        _logger = logger;
        _listener = new TcpListener(IPAddress.Loopback, 8888);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando servidor de Sockets TCP en el puerto 8888...");
        _listener.Start();

        stoppingToken.Register(() => _listener.Stop());

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _logger.LogInformation("Nuevo cliente TCP conectado desde: {Remote}", client.Client.RemoteEndPoint);
                
                _ = ProcesarClienteAsync(client, stoppingToken);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            _logger.LogInformation("Servidor TCP detenido de forma controlada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico en el servidor TCP.");
        }
    }

    private async Task ProcesarClienteAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? identificacion = null;

        try
        {
            identificacion = await reader.ReadLineAsync(stoppingToken);
            if (string.IsNullOrEmpty(identificacion))
            {
                client.Close();
                return;
            }

            if (identificacion == "REGISTRO:COCINA")
            {
                lock (_lock)
                {
                    _cocinaClient?.Close();
                    _cocinaClient = client;
                }
                _logger.LogInformation("Servicio de COCINA registrado correctamente.");
            }
            else if (identificacion == "REGISTRO:REPARTO")
            {
                lock (_lock)
                {
                    _repartoClient?.Close();
                    _repartoClient = client;
                }
                _logger.LogInformation("Servicio de REPARTO registrado correctamente.");
            }
            else
            {
                _logger.LogWarning("Cliente rechazado por identificación inválida: {Msg}", identificacion);
                client.Close();
                return;
            }

            while (!stoppingToken.IsCancellationRequested && client.Connected)
            {
                string? mensaje = await reader.ReadLineAsync(stoppingToken);
                if (mensaje == null) break;

                await ProcesarMensajeClienteAsync(identificacion, mensaje);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la conexión con un cliente de Sockets.");
        }
        finally
        {
            lock (_lock)
            {
                if (identificacion == "REGISTRO:COCINA" && _cocinaClient == client)
                {
                    _cocinaClient = null;
                    _logger.LogWarning("Servicio de COCINA desconectado.");
                }
                else if (identificacion == "REGISTRO:REPARTO" && _repartoClient == client)
                {
                    _repartoClient = null;
                    _logger.LogWarning("Servicio de REPARTO desconectado.");
                }
            }
            client.Close();
        }
    }

    private async Task ProcesarMensajeClienteAsync(string tipoCliente, string mensaje)
    {
        _logger.LogInformation("Mensaje recibido de {Cliente}: {Msg}", tipoCliente, mensaje);

        if (tipoCliente == "REGISTRO:COCINA" && mensaje.StartsWith("LISTO:"))
        {
            if (int.TryParse(mensaje.Split(':')[1], out int id))
            {
                var pedido = _pedidoRepository.ObtenerPorId(id);
                if (pedido != null)
                {
                    pedido.MarcarComoListo();
                    _pedidoRepository.Actualizar(pedido);
                    _logger.LogInformation("Pedido #{Id} marcado como Listo para Reparto.", id);

                    await EnviarARepartoAsync(pedido);
                }
            }
        }
        else if (tipoCliente == "REGISTRO:REPARTO" && mensaje.StartsWith("ENTREGADO:"))
        {
            if (int.TryParse(mensaje.Split(':')[1], out int id))
            {
                var pedido = _pedidoRepository.ObtenerPorId(id);
                if (pedido != null)
                {
                    pedido.Entregar();
                    _pedidoRepository.Actualizar(pedido);
                    _logger.LogInformation("Pedido #{Id} marcado como ENTREGADO.", id);
                }
            }
        }
    }

    public async Task<bool> EnviarACocinaAsync(Pedido pedido)
    {
        TcpClient? client;
        lock (_lock)
        {
            client = _cocinaClient;
        }

        if (client == null || !client.Connected)
        {
            _logger.LogWarning("No se pudo enviar el pedido #{Id} a Cocina porque no está conectada.", pedido.Id);
            return false;
        }

        try
        {
            pedido.IniciarPreparacion();
            _pedidoRepository.Actualizar(pedido);

            string json = JsonSerializer.Serialize(pedido);
            byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
            
            await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
            _logger.LogInformation("Pedido #{Id} enviado a Cocina correctamente.", pedido.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar pedido #{Id} a Cocina.", pedido.Id);
            return false;
        }
    }

    public async Task<bool> EnviarARepartoAsync(Pedido pedido)
    {
        TcpClient? client;
        lock (_lock)
        {
            client = _repartoClient;
        }

        if (client == null || !client.Connected)
        {
            _logger.LogWarning("No se pudo enviar el pedido #{Id} a Reparto porque no está conectado.", pedido.Id);
            return false;
        }

        try
        {
            pedido.EnviarAReparto();
            _pedidoRepository.Actualizar(pedido);

            string json = JsonSerializer.Serialize(pedido);
            byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");

            await client.GetStream().WriteAsync(bytes, 0, bytes.Length);
            _logger.LogInformation("Pedido #{Id} enviado a Reparto correctamente.", pedido.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar pedido #{Id} a Reparto.", pedido.Id);
            return false;
        }
    }
}
