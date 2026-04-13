using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class ResumenEstiloLbsDto
    {
        [Column("Estilo")]
        public string strEstilo { get; set; }

        [Column("LibrasDecoradas")]
        public double dcLibrasDecoradas { get; set; }

        [Column("Promedio")]
        public double dcPromedio { get; set; }

        [Column("Presupuesto")]
        public decimal dcPresupuesto { get; set; }

        [Column("MasterRetractilado")]
        public decimal dcMastersRetractilado { get; set; }

        [Column("PromedioRetractilado")]
        public decimal dcPromedioRetractilado { get; set; }

        [Column("PresupuestoRetractilado")]
        public decimal dcPresupuestoRetractilado { get; set; }

        [Column("fein")]
        public string strFein { get; set; }

        [Column("fefi")]
        public string strFefi { get; set; }
    }
}
