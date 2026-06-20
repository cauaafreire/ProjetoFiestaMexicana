namespace ProjetoFiestaMexicana.Models
{
    public class Garcom
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Turno { get; set; }
        public DateTime CriadoEm { get; set; }
        public string Origem { get; set; } = "Garcom"; // "Garcom" ou "Usuario"
    }
}