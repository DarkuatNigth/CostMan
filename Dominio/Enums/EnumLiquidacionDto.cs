namespace CostManagementService.Dominio.Enums
{
    public class EnumLiquidacionDto
    {
        // Auto intenta el cierre completo; el cierre siempre usa TiempoReal.
        public enum enmOrigenDatos
        {
            Auto,
            TiempoReal
        }

        public enum enmTipoMerge
        {
            Fresco = 1,
            Reproceso = 2
        }

        public enum enmTipoConsulta
        {
            ConFront = 1,
            ConBack = 2,
        }
    }
}
