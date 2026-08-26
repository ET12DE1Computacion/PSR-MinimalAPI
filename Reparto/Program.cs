using System.Threading.Tasks;
using Reparto.Services;

namespace Reparto;

class Program
{
    static async Task Main(string[] args)
    {
        var repartoService = new RepartoService();
        await repartoService.IniciarAsync();
    }
}
