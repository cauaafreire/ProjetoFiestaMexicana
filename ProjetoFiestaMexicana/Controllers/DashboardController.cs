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

            // Resumo do dia
            using (var cmd = new MySqlCommand("sp_dashboard_resumo", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                if (rd.Read())
                {
                    vm.Resumo = new DashboardResumo
                    {
                        TotalPedidos = rd.IsDBNull(rd.GetOrdinal("total_pedidos")) ? 0 : rd.GetInt32("total_pedidos"),
                        Faturamento = rd.IsDBNull(rd.GetOrdinal("faturamento")) ? 0 : rd.GetDecimal("faturamento"),
                        Pendentes = rd.IsDBNull(rd.GetOrdinal("pendentes")) ? 0 : rd.GetInt32("pendentes"),
                        Preparando = rd.IsDBNull(rd.GetOrdinal("preparando")) ? 0 : rd.GetInt32("preparando"),
                        Finalizados = rd.IsDBNull(rd.GetOrdinal("finalizados")) ? 0 : rd.GetInt32("finalizados"),
                        Cancelados = rd.IsDBNull(rd.GetOrdinal("cancelados")) ? 0 : rd.GetInt32("cancelados")
                    };
                }
            }

            // Mesas
            using (var cmd = new MySqlCommand("sp_dashboard_mesas", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                if (rd.Read())
                {
                    vm.Mesas = new DashboardMesas
                    {
                        Livres = rd.IsDBNull(rd.GetOrdinal("livres")) ? 0 : rd.GetInt32("livres"),
                        Ocupadas = rd.IsDBNull(rd.GetOrdinal("ocupadas")) ? 0 : rd.GetInt32("ocupadas"),
                        Total = rd.IsDBNull(rd.GetOrdinal("total")) ? 0 : rd.GetInt32("total")
                    };
                }
            }

            // Top pratos do dia
            using (var cmd = new MySqlCommand("sp_dashboard_top_pratos", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.TopPratos.Add(new DashboardTopPrato
                    {
                        Nome = rd.IsDBNull(rd.GetOrdinal("nome")) ? "" : rd.GetString("nome"),
                        TotalVendido = rd.IsDBNull(rd.GetOrdinal("total_vendido")) ? 0 : rd.GetInt32("total_vendido"),
                        Receita = rd.IsDBNull(rd.GetOrdinal("receita")) ? 0 : rd.GetDecimal("receita")
                    });
                }
            }

            // Por garçom
            using (var cmd = new MySqlCommand("sp_dashboard_por_garcom", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.Garcons.Add(new DashboardGarcom
                    {
                        Nome = rd.IsDBNull(rd.GetOrdinal("nome")) ? "" : rd.GetString("nome"),
                        TotalPedidos = rd.IsDBNull(rd.GetOrdinal("total_pedidos")) ? 0 : rd.GetInt32("total_pedidos"),
                        TotalValor = rd.IsDBNull(rd.GetOrdinal("total_valor")) ? 0 : rd.GetDecimal("total_valor")
                    });
                }
            }

            // Últimos pedidos
            using (var cmd = new MySqlCommand("sp_dashboard_ultimos_pedidos", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    vm.UltimosPedidos.Add(new DashboardPedido
                    {
                        Id = rd.IsDBNull(rd.GetOrdinal("id")) ? 0 : rd.GetInt32("id"),
                        Mesa = rd.IsDBNull(rd.GetOrdinal("mesa")) ? 0 : rd.GetInt32("mesa"),
                        Garcom = rd.IsDBNull(rd.GetOrdinal("garcom")) ? "" : rd.GetString("garcom"),
                        Status = rd.IsDBNull(rd.GetOrdinal("status")) ? "" : rd.GetString("status"),
                        Total = rd.IsDBNull(rd.GetOrdinal("total")) ? 0 : rd.GetDecimal("total"),
                        DataHora = rd.IsDBNull(rd.GetOrdinal("data_hora")) ? DateTime.Now : rd.GetDateTime("data_hora")
                    });
                }
            }

            return View(vm);
        }
    }
}