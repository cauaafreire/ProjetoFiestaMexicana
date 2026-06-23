using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Data;
using ProjetoFiestaMexicana.Models;

namespace ProjetoFiestaMexicana.Controllers
{
    public class GarcomController : Controller
    {
        private readonly Database db = new Database();

        // Helper para carregar as opções de turno para um SelectList
        private List<SelectListItem> CarregarTurnos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Manhã", Text = "Manhã" },
                new SelectListItem { Value = "Tarde", Text = "Tarde" },
                new SelectListItem { Value = "Noite", Text = "Noite" }
            };
        }

        [HttpGet]
        public IActionResult Index()
        {
            var lista = new List<Garcom>();
            using var conn = db.GetConnection();

            // Garçons cadastrados na tabela Garcom
            using (var cmd = new MySqlCommand("sp_garcom_listar", conn)
            { CommandType = System.Data.CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    lista.Add(new Garcom
                    {
                        Id = rd.GetInt32("id"),
                        Nome = rd["nome"] as string,
                        Turno = rd["turno"] as string,
                        CriadoEm = rd.GetDateTime("criado_em")
                    });
                }
            }

            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar()
        {
            ViewBag.Turnos = CarregarTurnos();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Criar(Garcom model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Turnos = CarregarTurnos();
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_garcom_criar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.Parameters.AddWithValue("p_turno", (object?)model.Turno ?? DBNull.Value);

            try
            {
                cmd.ExecuteNonQuery();
                TempData["ok"] = "Garçom cadastrado!";
                return RedirectToAction(nameof(Index));
            }
            catch (MySqlException ex)
            {
                ModelState.AddModelError("", "Erro ao cadastrar: " + ex.Message);
                ViewBag.Turnos = CarregarTurnos();
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            using var conn = db.GetConnection();

            Garcom? garcom = null;
            using (var cmd = new MySqlCommand("sp_garcom_obter", conn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("p_id", id);

                using var rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    garcom = new Garcom
                    {
                        Id = rd.GetInt32("id"),
                        Nome = rd.GetString("nome"),
                        Turno = rd["turno"] as string,
                        CriadoEm = rd.GetDateTime("criado_em")
                    };
                }

                if (garcom == null) return NotFound();

                ViewBag.Turnos = CarregarTurnos();
                return View(garcom);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Editar(Garcom model)
        {
            if (model.Id <= 0) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.Turnos = CarregarTurnos();
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_garcom_atualizar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id", model.Id);
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.Parameters.AddWithValue("p_turno", (object?)model.Turno ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            TempData["ok"] = "Garçom atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            using var conn = db.GetConnection();
            try
            {
                using var check = new MySqlCommand("SELECT COUNT(*) FROM Pedido WHERE garcom = @id", conn);
                check.Parameters.AddWithValue("@id", id);
                var total = Convert.ToInt32(check.ExecuteScalar());

                if (total > 0)
                {
                    TempData["erro"] = $"Não é possível excluir este garçom pois ele possui {total} pedido(s) vinculado(s).";
                    return RedirectToAction(nameof(Index));
                }

                using var cmd = new MySqlCommand("sp_garcom_excluir", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();

                TempData["ok"] = "Garçom excluído com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["erro"] = "Erro inesperado ao excluir: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
