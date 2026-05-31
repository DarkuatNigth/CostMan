using Newtonsoft.Json;

namespace CostManagement.Dominio.Entidades
{
    public class ParametrosConfig
    {
        [ConfigurationKeyName("TunelCabId")]
        public int intTunelCabeceraId { get; set; }

        [ConfigurationKeyName("IqfCabId")]
        public int intIqfCabId { get; set; }

        [ConfigurationKeyName("BrineCabId")]
        public int intBrineCabId { get; set; }

        [ConfigurationKeyName("ProdCocidoCabId")]
        public int intProdCocidoCabId { get; set; }

        [ConfigurationKeyName("ValNotInFresco")]
        public List<string> lstNotInFresco { get; set; }

        [ConfigurationKeyName("ValTotalFresco")]
        public List<string> lstDescTotFresco { get; set; }

        [ConfigurationKeyName("ValProdTerm")]
        public List<string> lstProdTerm { get; set; }

        [ConfigurationKeyName("RutaReporteCostos")]
        public string strRutaReporteCostos { get; set; } 

        [ConfigurationKeyName("ValEftGrupo")]
        public List<string> lstNotInGrupoItem{ get; set; }
    }
}
