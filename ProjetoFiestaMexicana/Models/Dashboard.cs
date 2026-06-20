using System;
using System.Collections.Generic;

namespace ProjetoFiestaMexicana.Models
{
    public class DashboardViewModel
    {
        public DashboardResumo Resumo { get; set; } = new DashboardResumo();
        public List<DashboardTopPrato> TopPratos { get; set; } = new List<DashboardTopPrato>();
        public List<DashboardGarcom> Garcons { get; set; } = new List<DashboardGarcom>();
        public List<DashboardPedido> UltimosPedidos { get; set; } = new List<DashboardPedido>();
        public List<DashboardFaturamentoHora> FaturamentoHora { get; set; } = new List<DashboardFaturamentoHora>();
        public List<DashboardFaturamentoMes> FaturamentoMes { get; set; } = new List<DashboardFaturamentoMes>();
    }

    public class DashboardResumo
    {
        public int TotalPedidos { get; set; }
        public decimal Faturamento { get; set; }
        public decimal TicketMedio => TotalPedidos > 0 ? Faturamento / TotalPedidos : 0;
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

    public class DashboardFaturamentoHora
    {
        public int Hora { get; set; }
        public decimal Valor { get; set; }
    }

    public class DashboardFaturamentoMes
    {
        public DateTime Data { get; set; }
        public decimal Valor { get; set; }
    }
}
