using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Data;
using ProjetoFiestaMexicana.Models;
using System.Data;

namespace ProjetoFiestaMexicana.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly Database db = new Database();

        [HttpGet]
        public IActionResult Index()
        {
            var lista = new List<Usuarios>();
            try
            {
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("SELECT id, nome, email, role FROM Usuarios ORDER BY nome", conn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new Usuarios
                    {
                        id = rd.GetInt32("id"),
                        nome = rd.GetString("nome"),
                        email = rd.GetString("email"),
                        role = rd.IsDBNull(rd.GetOrdinal("role")) ? "" : rd.GetString("role")
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao carregar lista: " + ex.Message;
            }
            return View(lista);
        }
        [HttpGet]
        public IActionResult CriarUsuario()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CriarUsuario(Usuarios vm, string role)
        {
            try
            {
                string senhaCriptografada = BCrypt.Net.BCrypt.HashPassword(vm.senha_hash);

                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_usuario_criar", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_nome", vm.nome);
                cmd.Parameters.AddWithValue("p_email", vm.email);
                cmd.Parameters.AddWithValue("p_senha_hash", senhaCriptografada); // Envia a senha segura
                cmd.Parameters.AddWithValue("p_role", role);
                cmd.ExecuteNonQuery();

                TempData["Sucesso"] = "Funcionário cadastrado com sucesso! 🌮";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Erro = "Erro ao salvar: " + ex.Message;
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            try
            {
                using var conn = db.GetConnection();
                // Você pode criar a sp_usuario_excluir no banco ou usar o comando abaixo:
                using var cmd = new MySqlCommand("DELETE FROM Usuarios WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                TempData["Sucesso"] = "Funcionário removido com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Não foi possível excluir o usuário: " + ex.Message;
            }

            return RedirectToAction("Index");
        }


    }
}

