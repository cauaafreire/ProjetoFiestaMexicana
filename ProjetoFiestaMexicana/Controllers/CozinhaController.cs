using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Data;
using ProjetoFiestaMexicana.Models;
using System.Data;
using System.Text.Json;

namespace ProjetoFiestaMexicana.Controllers
{
    public class CozinhaController : Controller
    {
        private readonly Database db = new Database();
        private const string OCULTOS_KEY = "CozinhaOcultos";

        // =====================================================================
        // PAINEL
        // =====================================================================

        [HttpGet]
        public IActionResult Index()
        {
            var pedidos = CarregarPedidos();
            ViewBag.Ocultados = GetOcultos();
            return View(pedidos);
        }

        // =====================================================================
        // ALTERAR STATUS — bloqueado se já Finalizado ou Cancelado
        // =====================================================================

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AlterarStatus(int id, string novoStatus)
        {
            var statusValidos = new[] { "Pendente", "Preparando", "Finalizado", "Cancelado" };
            if (!statusValidos.Contains(novoStatus))
                return BadRequest();

            // Verifica o status atual no banco antes de alterar
            var statusAtual = ObterStatusAtual(id);
            if (statusAtual == "Finalizado" || statusAtual == "Cancelado")
            {
                TempData["erro"] = "Pedido já encerrado, não é possível alterar o status.";
                return RedirectToAction(nameof(Index));
            }

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_cozinha_atualizar_status", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("p_id", id);
            cmd.Parameters.AddWithValue("p_status", novoStatus);
            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // OCULTAR DA TELA — só salva o ID na sessão, não toca no banco
        // =====================================================================

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult OcultarPedido(int id)
        {
            var ocultos = GetOcultos();
            if (!ocultos.Contains(id))
                ocultos.Add(id);
            SaveOcultos(ocultos);

            return RedirectToAction(nameof(Index));
        }

        private List<int> GetOcultos()
        {
            var json = HttpContext.Session.GetString(OCULTOS_KEY);
            if (string.IsNullOrWhiteSpace(json)) return new List<int>();
            try { return JsonSerializer.Deserialize<List<int>>(json) ?? new(); }
            catch { return new List<int>(); }
        }

        private void SaveOcultos(List<int> ids)
        {
            HttpContext.Session.SetString(OCULTOS_KEY, JsonSerializer.Serialize(ids));
        }

        private string ObterStatusAtual(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("SELECT status FROM Pedido WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteScalar()?.ToString() ?? "";
        }

        private List<CozinhaPedido> CarregarPedidos()
        {
            var pedidos = new List<CozinhaPedido>();

            using var conn = db.GetConnection();

            using (var cmd = new MySqlCommand("sp_cozinha_listar_pedidos", conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    pedidos.Add(new CozinhaPedido
                    {
                        Id = rd.GetInt32("id"),
                        NumeroMesa = rd.GetInt32("mesa_numero"),
                        NomeGarcom = rd.GetString("garcom_nome"),
                        Status = rd.GetString("status"),
                        Observacao = rd["observacao"] as string,
                        Total = rd.GetDecimal("total"),
                        DataHora = rd.GetDateTime("data_hora")
                    });
                }
            }

            foreach (var pedido in pedidos)
            {
                using var cmdItens = new MySqlCommand("sp_cozinha_listar_itens", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmdItens.Parameters.AddWithValue("p_id_pedido", pedido.Id);

                using var rdItens = cmdItens.ExecuteReader();
                while (rdItens.Read())
                {
                    pedido.Itens.Add(new CozinhaItem
                    {
                        PratoId = rdItens.GetInt32("prato_id"),
                        NomePrato = rdItens.GetString("prato_nome"),
                        Quantidade = rdItens.GetInt32("quantidade")
                    });
                }
            }

            return pedidos;
        }
    }
}