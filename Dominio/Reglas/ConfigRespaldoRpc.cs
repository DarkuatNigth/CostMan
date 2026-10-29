namespace CostManagement.Dominio.Reglas
{
    public enum ModoRespaldoRpc { Actual, N1, N1Excluyente }

    public static class ConfigRespaldoRpc
    {
        public static bool HistoricoPonderado { get; set; } = false;
        public static bool RetrocosteoEspeciales { get; set; } = false;
        public static ModoRespaldoRpc Modo { get; set; } = ModoRespaldoRpc.Actual;
    }
}
