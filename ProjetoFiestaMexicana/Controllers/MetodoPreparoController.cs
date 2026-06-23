using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Models; // Certifique-se de que este namespace está correto para sua classe MetodoPreparo
using ProjetoFiestaMexicana.Data;   // Certifique-se de que este namespace está correto para sua classe Database
using System.Data; // Necessário para System.Data.CommandType.StoredProcedure

namespace ProjetoFiestaMexicana.Controllers
{
    public class MetodoPreparoController : Controller
    {
        private readonly Database db = new Database();

        [HttpGet]
        public IActionResult Index()
        {
            var lista = new List<MetodoPreparo>();
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_metodo_preparo_listar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                lista.Add(new MetodoPreparo
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
        public IActionResult Criar(MetodoPreparo model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_metodo_preparo_criar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.ExecuteNonQuery();

            TempData["ok"] = "Método de Preparo cadastrado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            using var conn = db.GetConnection();
            MetodoPreparo? metodoPreparo = null;

            using (var cmd = new MySqlCommand("sp_metodo_preparo_obter", conn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("p_id", id);

                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        metodoPreparo = new MetodoPreparo
                        {
                            Id = rd.GetInt32("id"),
                            Nome = rd["nome"] as string,
                            CriadoEm = rd.GetDateTime("criado_em")
                        };
                    }
                }
            }

            if (metodoPreparo == null) return NotFound();

            return View(metodoPreparo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Editar(MetodoPreparo model)
        {
            if (model.Id <= 0) return NotFound();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_metodo_preparo_atualizar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id", model.Id);
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.ExecuteNonQuery();

            TempData["ok"] = "Método de Preparo atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            using var conn = db.GetConnection();
            try
            {
                using var check = new MySqlCommand("SELECT COUNT(*) FROM Prato WHERE metodo_preparo = @id", conn);
                check.Parameters.AddWithValue("@id", id);
                var total = Convert.ToInt32(check.ExecuteScalar());

                if (total > 0)
                {
                    TempData["erro"] = $"Não é possível excluir este método de preparo pois ele possui {total} prato(s) vinculado(s).";
                    return RedirectToAction(nameof(Index));
                }

                using var cmd = new MySqlCommand("sp_metodo_preparo_excluir", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();

                TempData["ok"] = "Método de preparo excluído com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["erro"] = "Erro inesperado ao excluir: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
