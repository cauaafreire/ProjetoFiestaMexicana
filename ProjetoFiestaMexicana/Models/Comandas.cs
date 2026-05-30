namespace ProjetoFiestaMexicana.Models
{
    public class ComandasItem
    {
        public int PratoId { get; set; }
        public string NomePrato { get; set; } = "";
        public string? CapaArquivo { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class ComandasViewModel
    {
        public int Id { get; set; }
        public int Mesa { get; set; }
        public string Garcom { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
        public DateTime DataHora { get; set; }
        public string Observacao { get; set; } = "";
        public List<ComandasItem> Itens { get; set; } = new();

        public decimal TaxaGarcom => Total * 0.10m;
        public decimal TotalFinal => Total + TaxaGarcom;
    }
}