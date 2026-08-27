using System.Threading.Tasks;
using Cocina.Services;

namespace Cocina;

class Program
{
    static async Task Main(string[] args)
    {
        var cocinaService = new CocinaService();
        await cocinaService.IniciarAsync();
    }
}
