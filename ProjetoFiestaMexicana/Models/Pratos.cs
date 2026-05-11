using Microsoft.AspNetCore.Mvc.Rendering; // Mantido para compatibilidade, mas não usado diretamente na Model

namespace ProjetoFiestaMexicana.Models
{
    public class Pratos
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal Preco { get; set; }
        public string? Descricao { get; set; }
        public int? CategoriaId { get; set; }
        public int? MetodoPreparoId { get; set; }
        public string? NivelPicancia { get; set; }
        public int? TempoPreparo { get; set; }
        public bool Disponivel { get; set; }
        public string? CapaArquivo { get; set; }
        public DateTime CriadoEm { get; set; }

        public string? CategoriaNome { get; set; }
        public string? MetodoPreparoNome { get; set; }
    }
}
