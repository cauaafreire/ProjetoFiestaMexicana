using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Autenticacao;
using ProjetoFiestaMexicana.Data;
using ProjetoFiestaMexicana.Models;
using System.Data;

namespace ProjetoFiestaMexicana.Controllers
{
    public class DashboardController : Controller
    {
        private readonly Database db = new Database();

        [HttpGet]
        public IActionResult Index()
        {
            if (!HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
                return RedirectToAction("Login", "Auth");

            var vm = new DashboardViewModel();
            using var conn = db.GetConnection();

            using (var cmd = new MySqlCommand("sp_dashboard_resumo", conn) { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                if (rd.Read())
                {
                    vm.Resumo.TotalPedidos = rd.IsDBNull("total_pedidos") ? 0 : rd.GetInt32("total_pedidos");
                    vm.Resumo.Faturamento = rd.IsDBNull("faturamento") ? 0 : rd.GetDecimal("faturamento");
                }
            }

            using (var cmd = new MySqlCommand("sp_dashboard_faturamento_hora", conn) { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.FaturamentoHora.Add(new DashboardFaturamentoHora { Hora = rd.GetInt32("hora"), Valor = rd.GetDecimal("valor") });
                }
            }

            using (var cmd = new MySqlCommand("sp_dashboard_faturamento_mes", conn) { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.FaturamentoMes.Add(new DashboardFaturamentoMes { Data = rd.GetDateTime("data"), Valor = rd.GetDecimal("valor") });
                }
            }
            using (var cmd = new MySqlCommand("sp_dashboard_top_pratos", conn) { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.TopPratos.Add(new DashboardTopPrato { Nome = rd.GetString("nome"), TotalVendido = rd.GetInt32("total_vendido"), Receita = rd.GetDecimal("receita") });
                }
            }

            using (var cmd = new MySqlCommand("sp_dashboard_por_garcom", conn) { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.Garcons.Add(new DashboardGarcom { Nome = rd.GetString("nome"), TotalPedidos = rd.GetInt32("total_pedidos"), TotalValor = rd.GetDecimal("total_valor") });
                }
            }

            using (var cmd = new MySqlCommand("sp_dashboard_ultimos_pedidos", conn) { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.UltimosPedidos.Add(new DashboardPedido { Id = rd.GetInt32("id"), Mesa = rd.GetInt32("mesa"), Garcom = rd.GetString("garcom"), Status = rd.GetString("status"), Total = rd.GetDecimal("total"), DataHora = rd.GetDateTime("data_hora") });
                }
            }

            return View(vm);
        }
    }
}
