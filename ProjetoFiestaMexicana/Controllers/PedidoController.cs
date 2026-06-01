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

        [HttpGet]
        public IActionResult Cardapio(string? q)
        {
            var grupos = new Dictionary<string, List<Pratos>>();
            var titulos = new List<string>();

            using var conn = db.GetConnection();

            using (var cmd = new MySqlCommand("sp_prato_listar_cardapio_categorias", conn)
            { CommandType = CommandType.StoredProcedure })
            {
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    var nome = rd.GetString("nome");
                    if (!string.IsNullOrWhiteSpace(q) &&
                        !nome.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;

                    var cat = rd.GetString("categoria_nome");
                    if (!grupos.ContainsKey(cat)) grupos[cat] = new List<Pratos>();

                    grupos[cat].Add(new Pratos
                    {
                        Id = rd.GetInt32("id"),
                        Nome = nome,
                        Preco = rd.GetDecimal("preco"),
                        CapaArquivo = rd["capa_arquivo"] as string,
                        Descricao = rd["descricao"] as string,
                        TempoPreparo = rd["tempo_preparo"] == DBNull.Value ? null : rd.GetInt32("tempo_preparo"),
                        NivelPicancia = rd["nivel_picancia"] as string,
                        CategoriaNome = cat
                    });

                    if (!titulos.Contains(nome)) titulos.Add(nome);
                }
            }

            var ordemCats = new List<string> { "Entradas", "Principais", "Bebidas", "Sobremesas" };
            var gruposOrdenados = new Dictionary<string, List<Pratos>>();
            foreach (var c in ordemCats)
                if (grupos.ContainsKey(c)) gruposOrdenados[c] = grupos[c];
            foreach (var kv in grupos)
                if (!gruposOrdenados.ContainsKey(kv.Key)) gruposOrdenados[kv.Key] = kv.Value;

            // ── MESAS ──
            var mesas = new List<SelectListItem>();
            using (var cmd = new MySqlCommand("sp_mesa_listar", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                    mesas.Add(new SelectListItem
                    {
                        Value = rd["id"].ToString(),
                        Text = "Mesa " + rd["numero"].ToString()
                    });
            }

            // ── GARÇONS ──
            var garcons = new List<SelectListItem>();
            using (var cmd = new MySqlCommand("sp_garcom_listar", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                    garcons.Add(new SelectListItem
                    {
                        Value = rd["id"].ToString(),
                        Text = rd["nome"].ToString()
                    });
            }

            ViewBag.Grupos = gruposOrdenados;
            ViewBag.q = q ?? "";
            ViewBag.Titulos = titulos;
            ViewBag.Mesas = mesas;
            ViewBag.Garcons = garcons;

            return View();
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

            using var conn = db.GetConnection();

            if (cart.Count > 0)
            {
                var idsCsv = string.Join(",", cart.Keys);
                using (var cmd = new MySqlCommand("sp_prato_listar_por_ids", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("p_ids", idsCsv);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        var pratoId = rd.GetInt32("id");
                        var item = new Pedido
                        {
                            PratoId = pratoId,
                            Nome = rd.GetString("nome"),
                            Preco = rd.GetDecimal("preco"),
                            CapaArquivo = rd["capa_arquivo"] as string,
                            Quantidade = cart[pratoId]
                        };
                        item.Subtotal = item.Quantidade * item.Preco;
                        totalGeral += item.Subtotal;
                        linhas.Add(item);
                    }
                }
            }

            // LÓGICA DE CARREGAR AS MESAS
            model.NomeMesa = new List<SelectListItem>();
            using (var cmdM = new MySqlCommand("SELECT id, numero FROM Mesa", conn))
            using (var rdM = cmdM.ExecuteReader())
            {
                while (rdM.Read())
                    model.NomeMesa.Add(new SelectListItem { Value = rdM["id"].ToString(), Text = "Mesa " + rdM["numero"].ToString() });
            }

            // LÓGICA DE CARREGAR OS GARÇONS (Buscando da sua tabela Usuarios)
            model.NomeGarcom = new List<SelectListItem>();
            using (var cmdG = new MySqlCommand("SELECT id, nome FROM Usuarios WHERE role = 'Garcom'", conn))
            using (var rdG = cmdG.ExecuteReader())
            {
                while (rdG.Read())
                    model.NomeGarcom.Add(new SelectListItem { Value = rdG["id"].ToString(), Text = rdG["nome"].ToString() });
            }

            ViewBag.Itens = linhas;
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

        [HttpGet]
        public IActionResult Menu(string? categoria)
        {
            var grupos = new Dictionary<string, List<Pratos>>();
            var maisPedidos = new List<PratoMaisPedido>();
            var todosOsPratos = new List<Pratos>();

            using var conn = db.GetConnection();

            using (var cmd = new MySqlCommand("sp_prato_listar_cardapio_categorias", conn)
            { CommandType = CommandType.StoredProcedure })
            {
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    var cat = rd.GetString("categoria_nome");
                    if (!grupos.ContainsKey(cat))
                        grupos[cat] = new List<Pratos>();

                    grupos[cat].Add(new Pratos
                    {
                        Id = rd.GetInt32("id"),
                        Nome = rd.GetString("nome"),
                        Preco = rd.GetDecimal("preco"),
                        CapaArquivo = rd["capa_arquivo"] as string,
                        Descricao = rd["descricao"] as string,
                        TempoPreparo = rd["tempo_preparo"] == DBNull.Value
                                        ? null : rd.GetInt32("tempo_preparo"),
                        NivelPicancia = rd["nivel_picancia"] as string
                    });
                }
            }

            using (var cmd2 = new MySqlCommand("sp_pratos_mais_pedidos", conn)
            { CommandType = CommandType.StoredProcedure })
            {
                using var rd2 = cmd2.ExecuteReader();
                while (rd2.Read())
                {
                    maisPedidos.Add(new PratoMaisPedido
                    {
                        Id = rd2.GetInt32("id"),
                        Nome = rd2.GetString("nome"),
                        Preco = rd2.GetDecimal("preco"),
                        CapaArquivo = rd2["capa_arquivo"] as string,
                        CategoriaNome = rd2.GetString("categoria_nome"),
                        TotalPedidos = rd2.GetInt32("total_pedidos")
                    });
                }
            }

            var ordemCats = new List<string> { "Entradas", "Principais", "Bebidas", "Sobremesas" };
            var gruposOrdenados = new Dictionary<string, List<Pratos>>();
            foreach (var c in ordemCats)
                if (grupos.ContainsKey(c)) gruposOrdenados[c] = grupos[c];
            foreach (var kv in grupos)
                if (!gruposOrdenados.ContainsKey(kv.Key)) gruposOrdenados[kv.Key] = kv.Value;

            var primeiraCategoria = categoria
                ?? ordemCats.FirstOrDefault(c => gruposOrdenados.ContainsKey(c))
                ?? "";

            ViewBag.Grupos = gruposOrdenados;
            ViewBag.MaisPedidos = maisPedidos;
            ViewBag.CategoriaAtiva = primeiraCategoria;
            ViewBag.TotalItensCart = GetCart().Values.Sum();


            return View(todosOsPratos);
        }

        [HttpGet]
        public IActionResult Detalhes(int id)
        {
            Pratos? prato = null;

            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_prato_obter_por_id", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("p_id", id);
            using var rd = cmd.ExecuteReader();

            if (rd.Read())
            {
                prato = new Pratos
                {
                    Id = rd.GetInt32("id"),
                    Nome = rd.GetString("nome"),
                    Preco = rd.GetDecimal("preco"),
                    CapaArquivo = rd["capa_arquivo"] as string,
                    Descricao = rd["descricao"] as string,
                    NivelPicancia = rd["nivel_picancia"] as string,
                    TempoPreparo = rd["tempo_preparo"] == DBNull.Value
                                    ? null : rd.GetInt32("tempo_preparo"),
                    CategoriaNome = rd["categoria_nome"] as string
                };
            }

            if (prato == null)
                return NotFound();

            return View(prato);
        }

        // =====================================================================
        // COMANDAS EM ABERTO
        // =====================================================================

        [HttpGet]
        public IActionResult Comandas()
        {
            var lista = new List<dynamic>();
            using var conn = db.GetConnection();

            var pedidos = new List<(int Id, int Mesa, string Garcom, string Status, decimal Total, DateTime DataHora, string Obs)>();

            using (var cmd = new MySqlCommand("sp_comanda_listar_abertas", conn)
            { CommandType = CommandType.StoredProcedure })
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    pedidos.Add((
                        rd.GetInt32("id"),
                        rd.GetInt32("mesa_numero"),
                        rd.GetString("garcom_nome"),
                        rd.GetString("status"),
                        rd.GetDecimal("total"),
                        rd.GetDateTime("data_hora"),
                        rd["observacao"] as string ?? ""
                    ));
                }
            }

            // Busca itens de cada pedido
            var resultado = new List<ComandasViewModel>();
            foreach (var p in pedidos)
            {
                var vm = new ComandasViewModel
                {
                    Id = p.Id,
                    Mesa = p.Mesa,
                    Garcom = p.Garcom,
                    Status = p.Status,
                    Total = p.Total,
                    DataHora = p.DataHora,
                    Observacao = p.Obs
                };

                using (var cmd2 = new MySqlCommand("sp_comanda_itens", conn)
                { CommandType = CommandType.StoredProcedure })
                {
                    cmd2.Parameters.AddWithValue("p_id", p.Id);
                    using var rd2 = cmd2.ExecuteReader();
                    while (rd2.Read())
                    {
                        vm.Itens.Add(new ComandasItem
                        {
                            PratoId = rd2.GetInt32("prato_id"),
                            NomePrato = rd2.GetString("prato_nome"),
                            CapaArquivo = rd2["capa_arquivo"] as string,
                            Quantidade = rd2.GetInt32("quantidade"),
                            PrecoUnitario = rd2.GetDecimal("preco_unitario"),
                            Subtotal = rd2.GetDecimal("subtotal")
                        });
                    }
                }

                resultado.Add(vm);
            }

            ViewBag.Grupos = GetGruposCardapio();
            ViewBag.Mesas = GetSelectList("sp_mesa_listar");
            ViewBag.Garcons = GetSelectList("sp_garcom_listar");

            return View(resultado);
        }

        // Adiciona item a uma comanda já existente
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdicionarItemComanda(int pedidoId, int pratoId, int quantidade)
        {
            using var conn = db.GetConnection();

            decimal preco = 0;
            using (var cmdP = new MySqlCommand("SELECT preco FROM Prato WHERE id=@id", conn))
            {
                cmdP.Parameters.AddWithValue("@id", pratoId);
                preco = Convert.ToDecimal(cmdP.ExecuteScalar());
            }

            using var cmd = new MySqlCommand("sp_comanda_adicionar_item", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_pedido", pedidoId);
            cmd.Parameters.AddWithValue("p_prato", pratoId);
            cmd.Parameters.AddWithValue("p_quantidade", quantidade);
            cmd.Parameters.AddWithValue("p_preco", preco);
            cmd.ExecuteNonQuery();

            return Ok(new { preco = preco.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) });
        }

        // Finaliza uma comanda
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult FinalizarComanda(int id)
        {
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_comanda_finalizar", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id", id);
            cmd.ExecuteNonQuery();
            TempData["ok"] = $"Comanda #{id} finalizada!";
            return RedirectToAction(nameof(Comandas));
        }

        // Adiciona item sem redirecionar
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdicionarAoPedidoSilencioso(int id)
        {
            var cart = GetCart();
            if (cart.ContainsKey(id)) cart[id]++;
            else cart[id] = 1;
            SaveCart(cart);
            return Ok();
        }

        private Dictionary<string, List<Pratos>> GetGruposCardapio()
        {
            var grupos = new Dictionary<string, List<Pratos>>();
            using var conn = db.GetConnection();
            using var cmd = new MySqlCommand("sp_prato_listar_cardapio_categorias", conn)
            { CommandType = CommandType.StoredProcedure };
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var cat = rd.GetString("categoria_nome");
                if (!grupos.ContainsKey(cat)) grupos[cat] = new List<Pratos>();
                grupos[cat].Add(new Pratos
                {
                    Id = rd.GetInt32("id"),
                    Nome = rd.GetString("nome"),
                    Preco = rd.GetDecimal("preco"),
                    CapaArquivo = rd["capa_arquivo"] as string,
                    CategoriaNome = cat
                });
            }

            var ordem = new List<string> { "Entradas", "Principais", "Bebidas", "Sobremesas" };
            var ord = new Dictionary<string, List<Pratos>>();
            foreach (var c in ordem) if (grupos.ContainsKey(c)) ord[c] = grupos[c];
            foreach (var kv in grupos) if (!ord.ContainsKey(kv.Key)) ord[kv.Key] = kv.Value;
            return ord;
        }
        [HttpGet]
        public IActionResult GetCartJson()
        {
            var cart = GetCart();
            var result = new Dictionary<string, object>();
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
                        var id = rd.GetInt32("id");
                        result[id.ToString()] = new
                        {
                            nome = rd.GetString("nome"),
                            preco = rd.GetDecimal("preco"),
                            foto = rd["capa_arquivo"] as string,
                            qtd = cart[id],
                            obs = ""
                        };
                    }
                }
            }
            return Json(result);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AtualizarItemCarrinho(int id, int quantidade)
        {
            var cart = GetCart();
            if (quantidade <= 0) cart.Remove(id);
            else cart[id] = quantidade;
            SaveCart(cart);
            return Ok();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult LimparCarrinho()
        {
            HttpContext.Session.Remove(CART_KEY);
            return Ok();
        }

    }
}