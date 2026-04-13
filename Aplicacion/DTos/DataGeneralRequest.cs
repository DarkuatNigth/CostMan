namespace CostManagement.Aplicación.DTos
{
    public class DataGeneralRequest
    {
        public string title { get; set; }
        public string title2 { get; set; }
        public string[]? columnas { get; set; }
        public string sp { get; set; }
        public List<modelParamSQL>? modelParam { get; set; }
    }
}
