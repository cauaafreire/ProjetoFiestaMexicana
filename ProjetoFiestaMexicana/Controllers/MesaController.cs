using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Data;
using ProjetoFiestaMexicana.Models;
using System.Data;

namespace ProjetoFiestaMexicana.Controllers
{
    public class MesaController : Controller
    {
        private readonly Database db = new Database();

        // Helper para carregar as opções de status para um SelectList
        private List<SelectListItem> CarregarStatusMesa()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Livre", Text = "Livre" },
                new SelectListItem { Value = "Ocupado", Text = "Ocupado" }
            };
        }

        [HttpGet]
        public IActionResult Index()
        {
            var lista = new List<Mesas>();
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_mesa_listar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                lista.Add(new Mesas
                {
                    Id = rd.GetInt32("id"),
                    Numero = rd.GetInt32("numero"),
                    Capacidade = rd.GetInt32("capacidade"),
                    Status = rd["status"] as string, // ENUM é lido como string
                    CriadoEm = rd.GetDateTime("criado_em")
                });
            }
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar()
        {
            ViewBag.StatusMesa = CarregarStatusMesa();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Criar(Mesas model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.StatusMesa = CarregarStatusMesa();
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_mesa_criar", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_numero", model.Numero);
            cmd.Parameters.AddWithValue("p_capacidade", model.Capacidade);
            cmd.Parameters.AddWithValue("p_status", model.Status);

            cmd.ExecuteNonQuery();

            TempData["ok"] = "Mesa cadastrada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            using var conn = db.GetConnection();

            Mesas? mesa = null;
            using (var cmd = new MySqlCommand("sp_mesa_obter", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("p_id", id);

                using var rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    mesa = new Mesas
                    {
                        Id = rd.GetInt32("id"),
                        Numero = rd.GetInt32("numero"),
                        Capacidade = rd.GetInt32("capacidade"),
                        Status = rd["status"] as string,
                        CriadoEm = rd.GetDateTime("criado_em")
                    };
                }

                if (mesa == null) return NotFound();

                ViewBag.StatusMesa = CarregarStatusMesa();
                return View(mesa);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Editar(Mesas model)
        {
            if (model.Id <= 0) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.StatusMesa = CarregarStatusMesa();
                return View(model);
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_mesa_atualizar", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id", model.Id);
            cmd.Parameters.AddWithValue("p_numero", model.Numero);
            cmd.Parameters.AddWithValue("p_capacidade", model.Capacidade);
            cmd.Parameters.AddWithValue("p_status", model.Status);
            cmd.ExecuteNonQuery();

            TempData["ok"] = "Mesa atualizada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            using var conn = db.GetConnection();
            try
            {
                using var cmd = new MySqlCommand("sp_mesa_excluir", conn) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();

                TempData["ok"] = "Mesa excluída!";
            }
            catch (MySqlException ex)
            {
                TempData["ok"] = "Não foi possível excluir a mesa: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
