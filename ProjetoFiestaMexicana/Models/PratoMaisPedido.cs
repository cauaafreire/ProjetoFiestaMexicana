namespace ProjetoFiestaMexicana.Models
{
    public class PratoMaisPedido
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public decimal Preco { get; set; }
        public string? CapaArquivo { get; set; }
        public string CategoriaNome { get; set; } = "";
        public int TotalPedidos { get; set; }
    }
}