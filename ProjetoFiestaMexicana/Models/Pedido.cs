using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjetoFiestaMexicana.Models
{
    public class Pedido
    {
        public int PratoId { get; set; }
        public string Nome { get; set; } = "";
        public string? CapaArquivo { get; set; }
        public int Quantidade { get; set; }
        public decimal Preco { get; set; }
        public decimal Subtotal { get; set; }

        public int Mesa { get; set; }
        public int Garcom { get; set; }
        public string? Observacao { get; set; }

        public List<SelectListItem> NomeMesa { get; set; } = new();
        public List<SelectListItem> NomeGarcom { get; set; } = new();
    }
}
