using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySql.Data.MySqlClient;
using ProjetoFiestaMexicana.Data;
using ProjetoFiestaMexicana.Models;
using System.Data;
using System.Text.Json;

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
            var itens = new List<Pratos>();
            var titulos = new List<string>();

            using var conn = db.GetConnection();

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

        // ------------------- Carrinho: dicionário {pratoId -> quantidade} -------------------

        private Dictionary<int, int> GetCart()
        {
            var json = HttpContext.Session.GetString(CART_KEY);
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<int, int>();

            try { return JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new(); }
            catch { return new Dictionary<int, int>(); }
        }

        private void SaveCart(Dictionary<int, int> cart)
        {
            var limpo = cart.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);

            if (limpo.Count == 0)
                HttpContext.Session.Remove(CART_KEY);
            else
                HttpContext.Session.SetString(CART_KEY, JsonSerializer.Serialize(limpo));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdicionarAoPedido(int id)
        {
            var cart = GetCart();
            if (cart.ContainsKey(id))
                cart[id]++;
            else
                cart[id] = 1;

            SaveCart(cart);
            TempData["ok"] = "Prato adicionado ao pedido.";
            return RedirectToAction(nameof(Cardapio));
        }

        // Novo: altera quantidade direto na view Pedido
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AlterarQuantidade(int id, int quantidade)
        {
            var cart = GetCart();
            if (quantidade <= 0)
                cart.Remove(id);
            else
                cart[id] = quantidade;

            SaveCart(cart);
            return RedirectToAction(nameof(Pedido));
        }

        [HttpGet]
        public IActionResult Pedido()
        {
            var cart = GetCart();
            var model = new Pedido();
            var linhas = new List<Pedido>();
            decimal totalGeral = 0;

            if (cart.Count > 0)
            {
                var idsCsv = string.Join(",", cart.Keys);

                using var conn = db.GetConnection();
                using (var cmd = new MySqlCommand("sp_prato_listar_por_ids", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("p_ids", idsCsv);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        var pratoId = rd.GetInt32("id");
                        var quantidade = cart.ContainsKey(pratoId) ? cart[pratoId] : 1;

                        var item = new Pedido
                        {
                            PratoId = pratoId,
                            Nome = rd.GetString("nome"),
                            Preco = rd.GetDecimal("preco"),
                            CapaArquivo = rd["capa_arquivo"] as string,
                            Quantidade = quantidade
                        };
                        item.Subtotal = item.Quantidade * item.Preco;
                        totalGeral += item.Subtotal;
                        linhas.Add(item);
                    }
                }

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
                string textoExibicao = "";
                for (int i = 0; i < rd.FieldCount; i++)
                {
                    string nomeColuna = rd.GetName(i).ToLower();
                    if (nomeColuna == "nome" || nomeColuna == "numero")
                    {
                        textoExibicao = rd[i].ToString();
                        break;
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
            var cart = GetCart();
            cart.Remove(id);
            SaveCart(cart);
            return RedirectToAction(nameof(Pedido));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Finalizar(Pedido model)
        {
            if (model.Mesa <= 0 || model.Garcom <= 0)
            {
                TempData["ok"] = "Selecione a mesa e o garçom corretamente.";
                return RedirectToAction(nameof(Pedido));
            }

            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["ok"] = "Pedido vazio.";
                return RedirectToAction(nameof(Cardapio));
            }

            using var conn = db.GetConnection();
            using var tx = conn.BeginTransaction();

            try
            {
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

                foreach (var kv in cart)
                {
                    int pratoId = kv.Key;
                    int qtd = kv.Value;

                    decimal preco = 0;
                    using (var cmdP = new MySqlCommand("SELECT preco FROM Prato WHERE id=@id", conn, tx))
                    {
                        cmdP.Parameters.AddWithValue("@id", pratoId);
                        preco = Convert.ToDecimal(cmdP.ExecuteScalar());
                    }

                    using var cmdI = new MySqlCommand("sp_pedido_adicionar_item", conn, tx) { CommandType = CommandType.StoredProcedure };
                    cmdI.Parameters.AddWithValue("p_id_pedido", idPed);
                    cmdI.Parameters.AddWithValue("p_id_prato", pratoId);
                    cmdI.Parameters.AddWithValue("p_quantidade", qtd);
                    cmdI.Parameters.AddWithValue("p_preco_unitario", preco);
                    cmdI.Parameters.AddWithValue("p_subtotal", preco * qtd);
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