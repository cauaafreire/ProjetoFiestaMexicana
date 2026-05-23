namespace ProjetoFiestaMexicana.Models
{
    public class DashboardResumo
    {
        public int TotalPedidos { get; set; }
        public decimal Faturamento { get; set; }
        public int Pendentes { get; set; }
        public int Preparando { get; set; }
        public int Finalizados { get; set; }
        public int Cancelados { get; set; }
    }

    public class DashboardMesas
    {
        public int Livres { get; set; }
        public int Ocupadas { get; set; }
        public int Total { get; set; }
    }

    public class DashboardTopPrato
    {
        public string Nome { get; set; } = "";
        public int TotalVendido { get; set; }
        public decimal Receita { get; set; }
    }

    public class DashboardGarcom
    {
        public string Nome { get; set; } = "";
        public int TotalPedidos { get; set; }
        public decimal TotalValor { get; set; }
    }

    public class DashboardPedido
    {
        public int Id { get; set; }
        public int Mesa { get; set; }
        public string Garcom { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
        public DateTime DataHora { get; set; }
    }

    public class DashboardViewModel
    {
        public DashboardResumo Resumo { get; set; } = new();
        public DashboardMesas Mesas { get; set; } = new();
        public List<DashboardTopPrato> TopPratos { get; set; } = new();
        public List<DashboardGarcom> Garcons { get; set; } = new();
        public List<DashboardPedido> UltimosPedidos { get; set; } = new();
    }
}