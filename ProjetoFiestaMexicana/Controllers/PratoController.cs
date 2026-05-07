using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Models; // Namespace da sua Model Pratos
using ProjetoFiestaMexicana.Data;   // Certifique-se de que este namespace está correto para sua classe Database
using System.Data;

namespace ProjetoFiestaMexicana.Controllers
{
    public class PratoController : Controller
    {
        private readonly Database db = new Database();


        // Helper para carregar as categorias para um SelectList
        private List<SelectListItem> CarregarCategorias(MySqlConnection conn)
        {
            var list = new List<SelectListItem>();
            using var cmd = new MySqlCommand("sp_categoria_listar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new SelectListItem { Value = rd.GetInt32("id").ToString(), Text = rd.GetString("nome") });
            return list;
        }

        // Helper para carregar os métodos de preparo para um SelectList
        private List<SelectListItem> CarregarMetodosPreparo(MySqlConnection conn)
        {
            var list = new List<SelectListItem>();
            using var cmd = new MySqlCommand("sp_metodo_preparo_listar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                list.Add(new SelectListItem { Value = rd.GetInt32("id").ToString(), Text = rd.GetString("nome") });
            return list;
        }

        // Helper para carregar os níveis de picância para um SelectList
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
                    Nome = rd["nome"] as string,
                    Preco = rd.GetDecimal("preco"),
                    Descricao = rd["descricao"] as string,
                    CategoriaId = rd["categoria"] == DBNull.Value ? null : (int?)rd.GetInt32("categoria"),
                    CategoriaNome = rd["categoria_nome"] as string,
                    MetodoPreparoId = rd["metodo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("metodo_preparo"),
                    MetodoPreparoNome = rd["metodo_preparo_nome"] as string,
                    NivelPicancia = rd["nivel_picancia"] as string,
                    TempoPreparo = rd["tempo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("tempo_preparo"),
                    Disponivel = rd.GetBoolean("disponivel"),
                    CapaArquivo = rd["capa_arquivo"] as string,
                    CriadoEm = rd.GetDateTime("criado_em")
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
        public async Task<IActionResult> Criar(Pratos model, IFormFile? capa)
        {
            if (!ModelState.IsValid)
            {
                using var conn = db.GetConnection();
                ViewBag.ListaCategorias = CarregarCategorias(conn);
                ViewBag.ListaMetodosPreparo = CarregarMetodosPreparo(conn);
                ViewBag.ListaNiveisPicancia = CarregarNiveisPicancia();
                return View(model);
            }

            string? capaArquivo = null;
            if (capa != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                capaArquivo = Guid.NewGuid().ToString() + "_" + capa.FileName;
                string filePath = Path.Combine(uploadsFolder, capaArquivo);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await capa.CopyToAsync(fileStream);
                }
                capaArquivo = "uploads/" + capaArquivo; // Caminho relativo para o navegador
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_prato_criar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.Parameters.AddWithValue("p_preco", model.Preco);
            cmd.Parameters.AddWithValue("p_descricao", (object?)model.Descricao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_categoria", (object?)model.CategoriaId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_metodo_preparo", (object?)model.MetodoPreparoId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_nivel_picancia", (object?)model.NivelPicancia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_tempo_preparo", (object?)model.TempoPreparo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_disponivel", model.Disponivel);
            cmd.Parameters.AddWithValue("p_capa_arquivo", (object?)capaArquivo ?? DBNull.Value);

            cmd.ExecuteNonQuery();

            TempData["ok"] = "Prato cadastrado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Editar(int id)
        {
            using var conn = db.GetConnection();

            Pratos? prato = null;
            using (var cmd = new MySqlCommand("sp_prato_obter", conn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("p_id", id);

                using var rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    prato = new Pratos
                    {
                        Id = rd.GetInt32("id"),
                        Nome = rd["nome"] as string,
                        Preco = rd.GetDecimal("preco"),
                        Descricao = rd["descricao"] as string,
                        CategoriaId = rd["categoria"] == DBNull.Value ? null : (int?)rd.GetInt32("categoria"),
                        CategoriaNome = rd["categoria_nome"] as string,
                        MetodoPreparoId = rd["metodo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("metodo_preparo"),
                        MetodoPreparoNome = rd["metodo_preparo_nome"] as string,
                        NivelPicancia = rd["nivel_picancia"] as string,
                        TempoPreparo = rd["tempo_preparo"] == DBNull.Value ? null : (int?)rd.GetInt32("tempo_preparo"),
                        Disponivel = rd.GetBoolean("disponivel"),
                        CapaArquivo = rd["capa_arquivo"] as string,
                        CriadoEm = rd.GetDateTime("criado_em")
                    };
                }

                if (prato == null) return NotFound();

                ViewBag.ListaCategorias = CarregarCategorias(conn);
                ViewBag.ListaMetodosPreparo = CarregarMetodosPreparo(conn);
                ViewBag.ListaNiveisPicancia = CarregarNiveisPicancia();
                return View(prato);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Pratos model, IFormFile? capa)
        {
            if (model.Id <= 0) return NotFound();
            if (!ModelState.IsValid)
            {
                using var conn = db.GetConnection();
                ViewBag.ListaCategorias = CarregarCategorias(conn);
                ViewBag.ListaMetodosPreparo = CarregarMetodosPreparo(conn);
                ViewBag.ListaNiveisPicancia = CarregarNiveisPicancia();
                return View(model);
            }

            string? capaArquivo = model.CapaArquivo; // Mantém a capa existente por padrão
            if (capa != null)
            {
                // Se já existe uma capa e uma nova está sendo enviada, apaga a antiga
                if (!string.IsNullOrEmpty(model.CapaArquivo))
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, model.CapaArquivo);
                    if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                }

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                capaArquivo = Guid.NewGuid().ToString() + "_" + capa.FileName;
                string filePath = Path.Combine(uploadsFolder, capaArquivo);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await capa.CopyToAsync(fileStream);
                }
                capaArquivo = "uploads/" + capaArquivo; // Caminho relativo para o navegador
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_prato_atualizar", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id", model.Id);
            cmd.Parameters.AddWithValue("p_nome", model.Nome);
            cmd.Parameters.AddWithValue("p_preco", model.Preco);
            cmd.Parameters.AddWithValue("p_descricao", (object?)model.Descricao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_categoria", (object?)model.CategoriaId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_metodo_preparo", (object?)model.MetodoPreparoId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_nivel_picancia", (object?)model.NivelPicancia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_tempo_preparo", (object?)model.TempoPreparo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_disponivel", model.Disponivel);
            cmd.Parameters.AddWithValue("p_capa_arquivo", (object?)capaArquivo ?? DBNull.Value);
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
                // Opcional: Apagar o arquivo de capa antes de excluir o registro do banco
                var pratoParaExcluir = new Pratos();
                using (var cmdObter = new MySqlCommand("sp_prato_obter", conn) { CommandType = System.Data.CommandType.StoredProcedure })
                {
                    cmdObter.Parameters.AddWithValue("p_id", id);
                    using var rd = cmdObter.ExecuteReader();
                    if (rd.Read()) pratoParaExcluir.CapaArquivo = rd["capa_arquivo"] as string;
                }

                if (!string.IsNullOrEmpty(pratoParaExcluir.CapaArquivo))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, pratoParaExcluir.CapaArquivo);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

                using var cmd = new MySqlCommand("sp_prato_excluir", conn) { CommandType = System.Data.CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("p_id", id);
                cmd.ExecuteNonQuery();

                TempData["ok"] = "Prato excluído!";
            }
            catch (MySqlException ex)
            {
                TempData["ok"] = "Não foi possível excluir o prato: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
