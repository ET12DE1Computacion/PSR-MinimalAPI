using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shared.Models;
using Cliente.DTOs;

namespace Cliente.Services;

/// <summary>
/// Servicio de cliente HTTP REST que encapsula todas las llamadas a la Minimal API del Backend.
/// </summary>
public class PedidoClientService
{
    private const string ApiUrl = "http://localhost:5000/api/pedidos";
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public PedidoClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// Envía un pedido al backend para ser creado.
    /// Retorna una tupla indicando éxito, mensaje adicional y el pedido creado si aplica.
    /// </summary>
    public async Task<(bool Exito, string? Mensaje, Pedido? Pedido)> EnviarPedidoAsync(CrearPedidoRequestDto requestDto)
    {
        try
        {
            string json = JsonSerializer.Serialize(requestDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var pedido = JsonSerializer.Deserialize<Pedido>(responseBody, _jsonOptions);
                return (true, "¡Pedido enviado y aceptado!", pedido);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                var resultado = JsonSerializer.Deserialize<RespuestaAceptadaDto>(responseBody, _jsonOptions);
                return (true, resultado?.Mensaje, resultado?.Pedido);
            }
            else
            {
                return (false, $"Error del servidor: {response.StatusCode}. Detalle: {responseBody}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error de comunicación de red: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Consulta el estado actual de un pedido por ID.
    /// </summary>
    public async Task<Pedido?> ObtenerPedidoPorIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiUrl}/{id}");
            if (!response.IsSuccessStatusCode) return null;

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Pedido>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtiene la lista completa de pedidos del sistema.
    /// </summary>
    public async Task<List<Pedido>?> ObtenerTodosLosPedidosAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiUrl);
            if (!response.IsSuccessStatusCode) return null;

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Pedido>>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
