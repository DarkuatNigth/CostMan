using CostManagement.Dominio.Entidades;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using static CostManagementService.Dominio.Enums.EnumLiquidacionDto;

namespace CostManagementService.Dominio.Entidades
{
    public class LiquidacionResDto
    {
        [Column("TIPOLIQ")]
        public string? strTipoLiq { get; set; }

        [Column("Lote")]
        public int intLote { get; set; }

        [Column("MES")]
        public int? intMes { get; set; }

        [Column("FechaLote")]
        public DateOnly? dtFechaLote { get; set; }

        [Column("Planta")]
        public string? strPlanta { get; set; }

        [Column("Proveedor")]
        public string? strProveedor { get; set; }

        [Column("Piscina")]
        public string? strRloPiscin { get; set; }

        [JsonIgnore]
        [Column("Masters")]
        public double? dcMasters { get; set; }

        [Column("Fecha Liq")]
        public DateOnly? dtFechaLiq { get; set; }

        [JsonIgnore]

        [Column("hora Liq")]
        public int? intHoraLiq { get; set; }

        [Column("bod_descri")]
        public string? strBodDescri { get; set; }

        [Column("clp_grupo")]
        public string? strClpGrupo { get; set; }

        [Column("pai_descri")]
        public string? strPaiDescri { get; set; }

        [Column("Clase")]
        [JsonIgnore]
        public string? strProClas02 { get; set; }

        [Column("pro_ClasePago")]
        public string? strProClasePago { get; set; }

        [Column("lid_clasificadora")]
        public string? strLidClasificadora { get; set; }

        [Column("Clasificadora")]
        public string? strClasificadora { get; set; }

        [Column("FechaTurno")]
        public DateOnly? dtFechaTurno { get; set; }

        [Column("Turno")]
        public string? strTurno { get; set; }

        [Column("Inicio Liq")]
        public DateTime dtInicioLiquidacion { get; set; }

        [Column("Fin Liq")]
        public DateTime? dtFinLiquidacion { get; set; }

        [Column("Horas Liq")]
        public decimal? dcHorasLiquidacion { get; set; }

        [Column("Grupo Proveedor")]
        public string? strGrupo { get; set; }

        [Column("TipoCopacking")]
        public string? strTipoCopacking { get; set; }

        [Column("Sub Grupo")]
        public string? strSubGrupo { get; set; }

        [Column("Tipo Proceso")]
        public string? strTipPro { get; set; }

        [Column("CodProd")]
        public int? intCodProd { get; set; }

        [Column("Tipo Producto")]
        public string? strProClas01 { get; set; }

        [Column("Tipo")]
        public string? strProClas05 { get; set; }

        [Column("Tipo Cola")]
        public string? strTipCola { get; set; }

        [JsonIgnore]

        [Column("Planta 2")]
        public string? strPlanta2 { get; set; }

        [Column("Tipo Proc")]
        public string? strProClas03 { get; set; }

        [Column("Descripcion")]
        public string? strDescripcion { get; set; }

        [Column("Talla")]
        public string? strTalla { get; set; }

        [Column("libras")]
        public double dcLibras { get; set; }

        [Column("Precio Compra")]
        public double? dcPrecioCompra { get; set; }

        [Column("Costo Mat Empaque")]
        public decimal dcCostoMatEmpaque { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Excedente")]
        public decimal? dcExcedente { get; set; }

        [Column("Certi")]
        public string? strCertificado { get; set; }

        [Column("Premio")]
        public decimal? dcCertificado { get; set; }

        [JsonIgnore]
        [Column("Libras Decorado")]
        public decimal? dcLibrasDecorado { get; set; }

        //[JsonIgnore]
        [Column("lbs Retractilado")]
        public decimal? dcLibrasRetractilado { get; set; }

        [Column("Copacking")]
        public decimal? dcCostoCopacking { get; set; }

        [Column("Proceso")]
        public decimal? dcTarifaProc { get; set; }


        [JsonIgnore]
        public decimal? dcValorCert { get; set; }

        [JsonIgnore]
        [Column("CodTal")]
        public int? intLidCodTal { get; set; }

        [JsonIgnore]
        public decimal? dcLiqPrecio { get; set; }

        [JsonIgnore]
        [Column("ProClas06")]
        public string? strProClas06 { get; set; }

        [JsonIgnore]
        [Column("ProCongela")]
        public int intProCongela { get; set; }

        [JsonIgnore]
        [Column("BodEsBrine")]
        public bool blBodEsBrine { get; set; }

        [JsonIgnore]
        [Column("BodCodigo")]

        public string? strBodCod { get; set; }

        [JsonIgnore]
        [Column("MedCodigo")]

        public decimal? dcMedCodigo { get; set; }

        [JsonIgnore]
        [Column("EmbCodigo")]

        public string? strEmbCodigo { get; set; }

        [JsonIgnore]
        [Column("RtaCodigo")]

        public decimal? dcRtaCodigo { get; set; }

        [JsonIgnore]
        [Column("lotSecuencial")]

        public decimal dcLotSecuencial { get; set; }

        [NotMapped]
        [JsonIgnore]
        [Column("LiqCopack")]
        public int intCodCopacking { get; set; }

        #region Constructor
        public LiquidacionResDto() { }



        #endregion

    }
}
