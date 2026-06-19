using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Autenticacao;
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
                // ADICIONADO: Selecionando o campo 'ativo'
                using var cmd = new MySqlCommand("SELECT id, nome, email, role, ativo FROM Usuarios ORDER BY nome", conn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new Usuarios
                    {
                        id = rd.GetInt32("id"),
                        nome = rd.GetString("nome"),
                        email = rd.GetString("email"),
                        role = rd.IsDBNull(rd.GetOrdinal("role")) ? "" : rd.GetString("role"),
                        ativo = rd.GetInt32("ativo") // MAPEADO AQUI
                    });
                }
            }
            catch (Exception ex) { TempData["Erro"] = ex.Message; }
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

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Desativar(int id)
        {
            using var conn = db.GetConnection();
            using var checkCmd = new MySqlCommand("SELECT email FROM Usuarios WHERE id = @id", conn);
            checkCmd.Parameters.AddWithValue("@id", id);
            if (checkCmd.ExecuteScalar()?.ToString() == "juanpablo@fiesta.com")
            {
                TempData["Erro"] = "O Administrador Master não pode ser desativado!";
                return RedirectToAction("Index");
            }
            using var cmd = new MySqlCommand("UPDATE Usuarios SET ativo = 0 WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            TempData["Sucesso"] = "Acesso desativado!";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Reativar(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("UPDATE Usuarios SET ativo = 1 WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            TempData["Sucesso"] = "Acesso reativado!";
            return RedirectToAction("Index");
        }


    }


}

