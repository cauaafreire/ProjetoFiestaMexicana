using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Data;
using ProjetoFiestaMexicana.Models;
using System.Data;

namespace ProjetoFiestaMexicana.Controllers
{
    public class PedidoController : Controller
    {
        private readonly Database db = new Database();
        private const string CART_KEY = "Pedido";

        // ------------------- Vitrine (Cardápio) -------------------
        [HttpGet]
        public IActionResult Cardapio(string? q)
        {
            var itens = new List<Pratos>(); // Usando sua model de Pratos
            var titulos = new List<string>();

            using var conn = db.GetConnection();

            // 1) itens filtrados para exibir na grade
            using (var cmd = new MySqlCommand("sp_prato_listar_cardapio", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("p_q", q ?? "");
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    itens.Add(new Pratos
                    {
                        Id = rd.GetInt32("id"),
                        Nome = rd.GetString("nome"),
                        Preco = rd.GetDecimal("preco"),
                        CapaArquivo = rd["capa_arquivo"] as string
                    });
                }
            }

            // 2) nomes para o datalist 
            using (var cmdAll = new MySqlCommand("sp_prato_listar_cardapio", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmdAll.Parameters.AddWithValue("p_q", "");
                using var rd2 = cmdAll.ExecuteReader();
                while (rd2.Read())
                {
                    var nome = rd2.GetString("nome");
                    if (!string.IsNullOrWhiteSpace(nome) && !titulos.Contains(nome))
                        titulos.Add(nome);
                }
            }

            ViewBag.q = q ?? "";
            ViewBag.Titulos = titulos;
            return View(itens);
        }

        private List<int> GetCartIds()
        {
            var csv = HttpContext.Session.GetString(CART_KEY);
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(csv)) return list;

            foreach (var s in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(s, out var id) && !list.Contains(id))
                    list.Add(id);

            return list;
        }

        private void SaveCartIds(List<int> ids)
        {
            var csv = string.Join(",", ids);
            if (string.IsNullOrEmpty(csv))
                HttpContext.Session.Remove(CART_KEY);
            else
                HttpContext.Session.SetString(CART_KEY, csv);
        }


        // Adiciona 1 por prato (IDs únicos)
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdicionarAoPedido(int id)
        {
            var ids = GetCartIds();
            if (!ids.Contains(id)) ids.Add(id);
            SaveCartIds(ids);

            TempData["ok"] = "Prato adicionado ao pedido.";
            return RedirectToAction(nameof(Cardapio));
        }

        [HttpGet]
        public IActionResult Pedido()
        {
            var ids = GetCartIds();
            var model = new Pedido();
            var linhas = new List<Pedido>();
            decimal totalGeral = 0;

            if (ids.Count > 0)
            {
                var idsCsv = string.Join(",", ids);

                using var conn = db.GetConnection();
                using (var cmd = new MySqlCommand("sp_prato_listar_por_ids", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("p_ids", idsCsv);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        var item = new Pedido
                        {
                            PratoId = rd.GetInt32("id"),
                            Nome = rd.GetString("nome"),
                            Preco = rd.GetDecimal("preco"),
                            CapaArquivo = rd["capa_arquivo"] as string,
                            Quantidade = 1 // sempre 1 por prato na lógica de IDs únicos
                        };
                        // LÓGICA NO CONTROLLER: Cálculo do subtotal
                        item.Subtotal = item.Quantidade * item.Preco;
                        totalGeral += item.Subtotal;
                        linhas.Add(item);
                    }
                }

                // Carrega Mesas e Garçons para as listas na Model
                model.NomeMesa = GetSelectList("sp_mesa_listar");
                model.NomeGarcom = GetSelectList("sp_garcom_listar");
            }

            ViewBag.Itens = linhas.OrderBy(x => x.Nome).ToList();
            ViewBag.TotalGeral = totalGeral;

            return View(model);
        }

        private List<SelectListItem> GetSelectList(string sp)
        {
            var lista = new List<SelectListItem>();
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                // Lógica para encontrar o texto de exibição (nome ou numero) sem dar erro
                string textoExibicao = "";

                // Percorre as colunas do resultado para ver qual delas existe
                for (int i = 0; i < rd.FieldCount; i++)
                {
                    string nomeColuna = rd.GetName(i).ToLower();
                    if (nomeColuna == "nome" || nomeColuna == "numero")
                    {
                        textoExibicao = rd[i].ToString();
                        break; // Encontrou, pode parar de procurar
                    }
                }

                lista.Add(new SelectListItem
                {
                    Value = rd["id"].ToString(),
                    Text = textoExibicao
                });
            }
            return lista;
        }


        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RemoverDoPedido(int id)
        {
            var ids = GetCartIds();
            if (ids.Remove(id))
                SaveCartIds(ids);

            return RedirectToAction(nameof(Pedido));
        }

        // =================== Finalizar (transação + SPs) ===================
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Finalizar(Pedido model)
        {
            // validações simples antes de abrir transação
            if (model.Mesa <= 0 || model.Garcom <= 0)
            {
                TempData["ok"] = "Selecione a mesa e o garçom corretamente.";
                return RedirectToAction(nameof(Pedido));
            }

            var ids = GetCartIds();
            if (ids.Count == 0)
            {
                TempData["ok"] = "Pedido vazio.";
                return RedirectToAction(nameof(Cardapio));
            }

            using var conn = db.GetConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) Cabeçalho (OUT id gerado)
                int idPed;
                using (var cmd = new MySqlCommand("sp_pedido_criar", conn, tx) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("p_mesa", model.Mesa);
                    cmd.Parameters.AddWithValue("p_garcom", model.Garcom);
                    cmd.Parameters.AddWithValue("p_observacao", (object)model.Observacao ?? DBNull.Value);
                    var pOut = new MySqlParameter("p_id_gerado", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(pOut);
                    cmd.ExecuteNonQuery();
                    idPed = Convert.ToInt32(pOut.Value);
                }

                // 2) Itens (cada prato com cálculo de subtotal no Controller)
                foreach (var pratoId in ids)
                {
                    decimal preco = 0;
                    using (var cmdP = new MySqlCommand("SELECT preco FROM Prato WHERE id=@id", conn, tx))
                    {
                        cmdP.Parameters.AddWithValue("@id", pratoId);
                        preco = Convert.ToDecimal(cmdP.ExecuteScalar());
                    }

                    using var cmdI = new MySqlCommand("sp_pedido_adicionar_item", conn, tx) { CommandType = CommandType.StoredProcedure };
                    cmdI.Parameters.AddWithValue("p_id_pedido", idPed);
                    cmdI.Parameters.AddWithValue("p_id_prato", pratoId);
                    cmdI.Parameters.AddWithValue("p_quantidade", 1);
                    cmdI.Parameters.AddWithValue("p_preco_unitario", preco);
                    cmdI.Parameters.AddWithValue("p_subtotal", preco * 1); // Cálculo manual no Controller
                    cmdI.ExecuteNonQuery();
                }

                tx.Commit();
                HttpContext.Session.Remove(CART_KEY);
                TempData["ok"] = $"Pedido #{idPed} criado com sucesso!";
            }
            catch (MySqlException ex)
            {
                tx.Rollback();
                TempData["ok"] = $"Falha ao finalizar: {ex.Message}";
            }

            return RedirectToAction(nameof(Cardapio));
        }
    }
}
