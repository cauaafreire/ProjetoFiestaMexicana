using Microsoft.AspNetCore.Mvc;

namespace ProjetoFiestaMexicana.Controllers
{
    public class SobreNosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}