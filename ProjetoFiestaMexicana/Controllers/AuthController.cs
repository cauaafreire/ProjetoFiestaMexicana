using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Autenticacao;
using ProjetoFiestaMexicana.Data;
using System.Data;

namespace ProjetoFiestaMexicana.Controllers
{
    public class AuthController : Controller
    {
        private readonly Database db = new Database();

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(string email, string senha, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Error = "Informe e-mail e senha.";
                return View();
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_usuario_obter_por_email", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_email", email);
            using var rd = cmd.ExecuteReader();

            if (!rd.Read())
            {
                ViewBag.Error = "Usuário ou senha inválidos.";
                return View();
            }

            var id = rd.GetInt32("id");
            var nome = rd.GetString("nome");
            var role = rd.GetString("role");
            var ativo = rd.GetBoolean("ativo");
            var senhaHash = rd["senha_hash"] as string ?? "";

            if (!ativo)
            {
                ViewBag.Error = "Usuário inativo.";
                return View();
            }

            // ===== Verificação da senha =====
            bool ok;
            try
            {
                ok = BCrypt.Net.BCrypt.Verify(senha, senhaHash);
            }
            catch { ok = false; }

            if (!ok)
            {
                ViewBag.Error = "Usuário ou senha inválidos.";
                return View();
            }

            HttpContext.Session.SetInt32(SessionKeys.UserId, id);
            HttpContext.Session.SetString(SessionKeys.UserName, nome);
            HttpContext.Session.SetString(SessionKeys.UserEmail, email);
            HttpContext.Session.SetString(SessionKeys.UserRole, role);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return role switch
            {
                "Garcom" => RedirectToAction("Cardapio", "Pedido"),
                "Chefe" => RedirectToAction("Index", "Cozinha"),
                _ => RedirectToAction("Index", "Dashboard")
            };

        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // opcional
        [HttpGet]
        public IActionResult AcessoNegado() => View();
    }
}
