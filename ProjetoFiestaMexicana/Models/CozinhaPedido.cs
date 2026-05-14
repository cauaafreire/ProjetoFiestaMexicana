namespace ProjetoFiestaMexicana.Models
{
    public class CozinhaItem
    {
        public int PratoId { get; set; }
        public string NomePrato { get; set; } = "";
        public int Quantidade { get; set; }
    }

    public class CozinhaPedido
    {
        public int Id { get; set; }
        public int NumeroMesa { get; set; }
        public string NomeGarcom { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Observacao { get; set; }
        public decimal Total { get; set; }
        public DateTime DataHora { get; set; }
        public List<CozinhaItem> Itens { get; set; } = new();
    }
}