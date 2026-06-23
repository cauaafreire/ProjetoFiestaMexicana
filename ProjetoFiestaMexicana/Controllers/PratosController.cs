using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Models; // Namespace da sua Model Pratos
using ProjetoFiestaMexicana.Data;   // Certifique-se de que este namespace está correto para sua classe Database
using System.Data;
using System.IO;

namespace ProjetoFiestaMexicana.Controllers
{
    public class PratosController : Controller
    {
        private readonly Database db = new Database();

        // Helpers para carregar os selects via SP
        private List<SelectListItem> CarregarCategorias(MySqlConnection conn)
        {
            var list = new List<SelectListItem>();
            using var cmd = new MySqlCommand("sp_categoria_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new SelectListItem { Value = rd.GetInt32("id").ToString(), Text = rd.GetString("nome") });
            return list;
        }

        private List<SelectListItem> CarregarMetodosPreparo(MySqlConnection conn)
        {
            var list = new List<SelectListItem>();
            using var cmd = new MySqlCommand("sp_metodo_preparo_listar", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new SelectListItem { Value = rd.GetInt32("id").ToString(), Text = rd.GetString("nome") });
            return list;
        }

        private List<SelectListItem> CarregarNiveisPicancia()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Sem Pimenta", Text = "Sem Pimenta" },
                new SelectListItem { Value = "Suave", Text = "Suave" },
                new SelectListItem { Value = "Médio", Text = "Médio" },
                new SelectListItem { Value = "Forte", Text = "Forte" },
                new SelectListItem { Value = "Extra", Text = "Extra" }
            };
        }

        [HttpGet]
        public IActionResult Index()
        {
            var lista = new List<Pratos>();
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_prato_listar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                lista.Add(new Pratos
                {
                    Id = rd.GetInt32("id"),
                    Nome = rd.GetString("nome"),
                    Preco = rd.GetDecimal("preco"),
                    Descricao = rd["descricao"] == DBNull.Value ? null : rd.GetString("descricao"),
                    CategoriaId = rd["categoria"] == DBNull.Value ? null : (int?)rd.GetInt32("categoria"),
                    CategoriaNome = rd["categoria_nome"] == DBNull.Value ? null : rd.GetString("categoria_nome"),
                    MetodoPreparoId = rd["metodo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("metodo_preparo"),
                    MetodoPreparoNome = rd["metodo_preparo_nome"] == DBNull.Value ? null : rd.GetString("metodo_preparo_nome"),
                    NivelPicancia = rd["nivel_picancia"] == DBNull.Value ? null : rd.GetString("nivel_picancia"),
                    TempoPreparo = rd["tempo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("tempo_preparo"),
                    Disponivel = rd.GetBoolean("disponivel"),
                    CriadoEm = rd.GetDateTime("criado_em"),
                    CapaArquivo = rd["capa_arquivo"] == DBNull.Value ? null : rd.GetString("capa_arquivo")
                });
            }
            return View(lista);
        }

        [HttpGet]
        public IActionResult Criar()
        {
            using var conn = db.GetConnection();
            ViewBag.ListaCategorias = CarregarCategorias(conn);
            ViewBag.ListaMetodosPreparo = CarregarMetodosPreparo(conn);
            ViewBag.ListaNiveisPicancia = CarregarNiveisPicancia();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Criar(Pratos model, IFormFile? capa)
        {
            // Salvar capa (opcional)
            string? relPath = null;
            if (capa != null && capa.Length > 0)
            {
                var ext = Path.GetExtension(capa.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "capas");
                Directory.CreateDirectory(saveDir);
                var absPath = Path.Combine(saveDir, fileName);
                using var fs = new FileStream(absPath, FileMode.Create);
                capa.CopyTo(fs);
                relPath = Path.Combine("capas", fileName).Replace("\\", "/");
            }

            using var conn2 = db.GetConnection();
            using var cmd = new MySqlCommand("sp_prato_criar", conn2) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.Parameters.AddWithValue("p_preco", model.Preco);
            cmd.Parameters.AddWithValue("p_descricao", (object?)model.Descricao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_categoria", (object?)model.CategoriaId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_metodo_preparo", (object?)model.MetodoPreparoId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_nivel_picancia", (object?)model.NivelPicancia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_tempo_preparo", (object?)model.TempoPreparo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_disponivel", model.Disponivel);
            cmd.Parameters.AddWithValue("p_capa_arquivo", (object?)relPath ?? DBNull.Value);

            cmd.ExecuteNonQuery();

            TempData["ok"] = "Prato cadastrado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            using var conn = db.GetConnection();
            Pratos? prato = null;

            using (var cmd = new MySqlCommand("sp_prato_obter", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("p_id", id);

                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        prato = new Pratos
                        {
                            Id = rd.GetInt32("id"),
                            Nome = rd.GetString("nome"),
                            Preco = rd.GetDecimal("preco"),
                            Descricao = rd["descricao"] == DBNull.Value ? null : rd.GetString("descricao"),
                            CategoriaId = rd["categoria"] == DBNull.Value ? null : (int?)rd.GetInt32("categoria"),
                            CategoriaNome = rd["categoria_nome"] == DBNull.Value ? null : rd.GetString("categoria_nome"),
                            MetodoPreparoId = rd["metodo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("metodo_preparo"),
                            MetodoPreparoNome = rd["metodo_preparo_nome"] == DBNull.Value ? null : rd.GetString("metodo_preparo_nome"),
                            NivelPicancia = rd["nivel_picancia"] == DBNull.Value ? null : rd.GetString("nivel_picancia"),
                            TempoPreparo = rd["tempo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("tempo_preparo"),
                            Disponivel = rd.GetBoolean("disponivel"),
                            CapaArquivo = rd["capa_arquivo"] == DBNull.Value ? null : rd.GetString("capa_arquivo")
                        };
                    }
                }
            }

            if (prato == null) return NotFound();

            ViewBag.ListaCategorias = CarregarCategorias(conn);
            ViewBag.ListaMetodosPreparo = CarregarMetodosPreparo(conn);
            ViewBag.ListaNiveisPicancia = CarregarNiveisPicancia();

            return View(prato);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Editar(Pratos model, IFormFile? capa)
        {
            if (model.Id <= 0) return NotFound();
            if (string.IsNullOrWhiteSpace(model.Nome) || model.Preco <= 0)
            {
                ModelState.AddModelError("", "Informe nome e preço (>=0).");
            }

            string? relPath = model.CapaArquivo; // Mantém o arquivo existente se um novo não for enviado
            if (capa != null && capa.Length > 0)
            {
                var ext = Path.GetExtension(capa.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "capas");
                Directory.CreateDirectory(saveDir);
                var absPath = Path.Combine(saveDir, fileName);
                using var fs = new FileStream(absPath, FileMode.Create);
                capa.CopyTo(fs);
                relPath = Path.Combine("capas", fileName).Replace("\\", "/");
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_prato_atualizar", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id", model.Id);
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.Parameters.AddWithValue("p_preco", model.Preco);
            cmd.Parameters.AddWithValue("p_descricao", (object?)model.Descricao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_categoria", (object?)model.CategoriaId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_metodo_preparo", (object?)model.MetodoPreparoId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_nivel_picancia", (object?)model.NivelPicancia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_tempo_preparo", (object?)model.TempoPreparo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_disponivel", model.Disponivel);
            cmd.Parameters.AddWithValue("p_capa_arquivo", (object?)relPath ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            TempData["ok"] = "Prato atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Excluir(int id)
        {
            using var conn = db.GetConnection();
            try
            {
                using var check = new MySqlCommand("SELECT COUNT(*) FROM ItemPedido WHERE prato = @id", conn);
                check.Parameters.AddWithValue("@id", id);
                var total = Convert.ToInt32(check.ExecuteScalar());

                if (total > 0)
                {
                    TempData["erro"] = $"Não é possível excluir este prato pois ele está vinculado a {total} pedido(s).";
                    return RedirectToAction(nameof(Index));
                }

                using var cmd = new MySqlCommand("sp_prato_excluir", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();

                TempData["ok"] = "Prato excluído com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["erro"] = "Erro inesperado ao excluir: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
