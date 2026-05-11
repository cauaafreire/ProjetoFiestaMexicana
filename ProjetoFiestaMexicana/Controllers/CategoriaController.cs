using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Models; // Certifique-se de que este namespace está correto para sua classe Categoria
using ProjetoFiestaMexicana.Data;   // Certifique-se de que este namespace está correto para sua classe Database
using System.Data; // Necessário para System.Data.CommandType.StoredProcedure

namespace ProjetoFiestaMexicana.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly Database db = new Database();

        [HttpGet]
        public IActionResult Index()
        {
            var lista = new List<Categoria>();
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_categoria_listar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                lista.Add(new Categoria
                {
                    Id = rd.GetInt32("id"),
                    Nome = rd["nome"] as string,
                    CriadoEm = rd.GetDateTime("criado_em")
                });
            }
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Criar(Categoria model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_categoria_criar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.ExecuteNonQuery();

            TempData["ok"] = "Categoria cadastrada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            using var conn = db.GetConnection();
            Categoria? categoria = null;

            using (var cmd = new MySqlCommand("sp_categoria_obter", conn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("p_id", id);

                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        categoria = new Categoria
                        {
                            Id = rd.GetInt32("id"),
                            Nome = rd["nome"] as string,
                            CriadoEm = rd.GetDateTime("criado_em")
                        };
                    }
                }
            }

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Editar(Categoria model)
        {
            if (model.Id <= 0) return NotFound();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_categoria_atualizar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id", model.Id);
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.ExecuteNonQuery();

            TempData["ok"] = "Categoria atualizada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            using var conn = db.GetConnection();
            try
            {
                using var cmd = new MySqlCommand("sp_categoria_excluir", conn) { CommandType = System.Data.CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();

                TempData["ok"] = "Categoria excluída!";
            }
            catch (MySqlException ex)
            {
                TempData["ok"] = "Não foi possível excluir a categoria: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
