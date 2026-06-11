using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Models;
using ProjetoFiestaMexicana.Data;
using System.Data;

namespace ProjetoFiestaMexicana.Controllers
{
    public class SobreNosController : Controller
    {
        private readonly Database db = new Database();

        public IActionResult Index()
        {
            var lista = new List<Pratos>();
            try
            {
                using var conn = db.GetConnection();
                using var cmd = new MySqlCommand("sp_prato_listar", conn) { CommandType = CommandType.StoredProcedure };
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new Pratos
                    {
                        Id = rd.GetInt32("id"),
                        Nome = rd.GetString("nome"),
                        Preco = rd.GetDecimal("preco"),
                        Descricao = rd["descricao"] == DBNull.Value ? null : rd.GetString("descricao"),
                        CategoriaNome = rd["categoria_nome"] == DBNull.Value ? null : rd.GetString("categoria_nome"),
                        CapaArquivo = rd["capa_arquivo"] == DBNull.Value ? null : rd.GetString("capa_arquivo")
                    });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Erro = ex.Message;
            }

            return View(lista);
        }
    }
}
