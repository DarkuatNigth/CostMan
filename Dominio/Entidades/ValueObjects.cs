using System.Collections.Concurrent;

namespace CostManagement.Dominio.Entidades
{
    public record LoteRpcKey(int LotNumero, int LotUnificado);
    public record LoteRpcKeyXSec(int intLoteSecuencial, int intLoteUnificado);
    public record LoteFrsKey(int intLote, int intProdCod, int intTallaCod);
    public record LoteRpcUniKey(int intLoteUnificado, int intProdCod, int intTallaCod);
    public record LoteRpcKeyXProdTal(int ProdCod, int Codtal);
    public record PromXProdTal(string ProdCod, int Codtal);
    public record PromLoteXProdTal(int intLote, string strProdCod, int intTallaCod);
    public record ContextoCostos(
            ConcurrentDictionary<PromLoteXProdTal, decimal> DictPorLoteFrs,
            ConcurrentDictionary<PromXProdTal, decimal> DictPromFrs,
            ConcurrentDictionary<PromLoteXProdTal, decimal> DictPorLoteRpc,
            ConcurrentDictionary<PromXProdTal, decimal> DictPromRpc,
            ConcurrentDictionary<PromLoteXProdTal, decimal> DictPorLoteSld
        );
    public record CtCtblXClaseTipo (string strClase, string strTipo);
    internal sealed class CostosUnitarios
    {
        // Proceso Primario
        public decimal dcRecepcion { get; init; }
        public decimal dcClasificacion { get; init; }
        public decimal dcCodificacion { get; init; }
        public decimal dcDescabezado { get; init; }
        // Proceso Presentación
        public decimal dcDecorado { get; init; }
        public decimal dcRetractilado { get; init; }
        // Proceso Congelación
        public decimal dcBrine { get; init; }
        public decimal dcIQF { get; init; }
        public decimal dcTunel { get; init; }
        // Proceso Secundario
        public decimal dcPelado { get; init; }
        public decimal dcHidratacion { get; init; }
        public decimal dcCocido { get; init; }
        // Costos estructurales
        public decimal dcCostDirectoVar { get; init; }
        public decimal dcCostDirectoFij { get; init; }
        public decimal dcCostIndirVar { get; init; }
        public decimal dcCostIndirFij { get; init; }
        public decimal dcCopacking { get; init; }
        public static CostosUnitarios ExtraerCostosUnitarios(
        ConcurrentDictionary<string, decimal> d) => new()
        {
            // Proceso Primario
            dcRecepcion = d.GetValueOrDefault("Recepcion"),
            dcClasificacion = d.GetValueOrDefault("Clasificacion"),
            dcCodificacion = d.GetValueOrDefault("Codificacion"),
            dcDescabezado = d.GetValueOrDefault("Descabezado"),
            // Proceso Presentación
            dcDecorado = d.GetValueOrDefault("Decorado"),
            dcRetractilado = d.GetValueOrDefault("Retractilado"),
            // Proceso Congelación
            dcBrine = d.GetValueOrDefault("Brine"),
            dcIQF = d.GetValueOrDefault("IQF"),
            dcTunel = d.GetValueOrDefault("Tunel"),
            // Proceso Secundario
            dcPelado = d.GetValueOrDefault("Pelado"),
            dcHidratacion = d.GetValueOrDefault("Hidratacion"),
            dcCocido = d.GetValueOrDefault("Cocido"),
            // Costos estructurales
            dcCostDirectoVar = d.GetValueOrDefault("C.D.Variables"),
            dcCostDirectoFij = d.GetValueOrDefault("C.D.Fijos"),
            dcCostIndirVar = d.GetValueOrDefault("C.I.Variables"),
            dcCostIndirFij = d.GetValueOrDefault("C.I.Fijos"),
            dcCopacking = d.GetValueOrDefault("C.Copacking"),
        };
    }
    public record LoteRpcKeyLoteXProd(int intLoteSecuencial, string strCodProd);

    public record TipoCopacking(string Codigo, string Descripcion);
    public record LoteRpcNivelCosteo(int intLotNumero, int intNivel);
    public class ValueObjects
    {
    }
}
