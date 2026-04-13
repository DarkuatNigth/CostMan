using CostManagement.Dominio.Entidades;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CostManagement.Aplicación.DTos
{
    public class DiarioCosto
    {
        [Column("Tipo Saldo")]
        public string strTipo { get; set; }

        [JsonIgnore]
        public int intTipoMovInv { get; set; }

        [Column("Fecha Saldo")]
        public DateTime dtFechaMov { get; set; }

        [JsonIgnore]
        public string strCodTip { get; set; }

        [Column("Descripcion Saldo")]
        public string strDescripcion { get; set; }

        [Column("Proceso")]
        public string? strProdDescri { get; set; }

        [Column("Lote")]
        public int intLote { get; set; }

        [Column("Clase")]
        public string? strProClas01 { get; set; }

        [Column("Tipo")]
        public string? strProClas05 { get; set; }

        [Column("Talla")]
        public string? strTalDescri { get; set; }

        [JsonIgnore]
        public short stTalCodigo { get; set; }

        [Column("CodProd")]
        public string strProCodcor { get; set; }

        [Column("Descripcion")]
        public string? strProDesesp { get; set; }

        [JsonIgnore]
        public string strEmbCodigo { get; set; }

        [JsonIgnore]
        public int intMedCodigo { get; set; }

        [Column("Libras")]
        public decimal dcLibras { get; set; }

        [Column("Masters")]
        public decimal dcMasters { get; set; }

        [JsonIgnore]
        public string strCodBod { get; set; }

        [Column("Costo Unitario")]
        public decimal dcCostoUnit { get; set; }

        [Column("Costo Total")]
        public decimal dcCostoTot { get; set; }

        [JsonIgnore]
        public string? strProCod { get; set; }



        /// <summary>Costo por libra específico del lote — fuente FRS.</summary>
        public static ConcurrentDictionary<PromLoteXProdTal, decimal> ConstruirDictPorLoteFrs(
            List<LiquidacionResultado> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<PromLoteXProdTal, decimal>();
            return new ConcurrentDictionary<PromLoteXProdTal, decimal>(
                (from frs in lst
                    group frs by new
                    { frs.intLote, frs.intCodProd, frs.intLidCodTal } into g
                    select g.First()).ToDictionary(
                                   frs => new PromLoteXProdTal(frs.intLote, frs.intCodProd.ToString().Trim(), (int)frs.intLidCodTal),
                                   frs => (decimal)frs.dcCostoTotXLibra)
                    );
        }


        /// <summary>Costo promedio ponderado por prod+talla — fuente FRS (para exportaciones).</summary>
        public static ConcurrentDictionary<PromXProdTal, decimal> ConstruirDictPromedioFrs(
            List<LiquidacionResultado> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<PromXProdTal, decimal>();
            return new ConcurrentDictionary<PromXProdTal, decimal>(
                lst
                .GroupBy(x => new PromXProdTal(x.intCodProd.ToString().Trim(), (int)x.intLidCodTal))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal libras = (decimal)g.Sum(x => x.dcLibras);
                        decimal dolares = (decimal)g.Sum(x => x.dcTotalDol);
                        return libras > 0 ? dolares / libras : 0m;
                    })
                );
        }


        /// <summary>Costo por libra específico del lote — fuente RPC.</summary>
        public static ConcurrentDictionary<PromLoteXProdTal, decimal> ConstruirDictPorLoteRpc(
            List<MatPrimaReproceso> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<PromLoteXProdTal, decimal>();
            return new ConcurrentDictionary<PromLoteXProdTal, decimal>(
                (from rpc in lst
                 group rpc by new
                 { rpc.intLoteUnificado, rpc.intProdCod, rpc.intCodTal } into g
                 select g.First())
                .ToDictionary(
                    frs => new PromLoteXProdTal(frs.intLoteUnificado, frs.intProdCod.ToString().Trim(), (int)frs.intCodTal),
                    frs => (decimal)frs.dcCostoTotXLibra)
            );
        }

        /// <summary>Costo promedio ponderado por prod+talla — fuente RPC (para exportaciones).</summary>
        public static ConcurrentDictionary<PromXProdTal, decimal> ConstruirDictPromedioRpc(
            List<MatPrimaReproceso> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<PromXProdTal, decimal>();
            return new ConcurrentDictionary<PromXProdTal, decimal>(lst
                .GroupBy(x => new PromXProdTal(x.intProdCod.ToString().Trim(), (int)x.intCodTal))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal libras = (decimal)g.Sum(x => x.dbLibras);
                        decimal dolares = (decimal)g.Sum(x => x.dbCostoTotal);
                        return libras > 0 ? dolares / libras : 0m;
                    })
                );
        }

        /// <summary>Costo por libra específico del lote — fuente Saldos.</summary>
        public static ConcurrentDictionary<PromLoteXProdTal, decimal> ConstruirDictPorLoteSld(
            List<DiarioCosto> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<PromLoteXProdTal, decimal>();
            return new ConcurrentDictionary<PromLoteXProdTal, decimal>(
                (from frs in lst
                 where frs.strCodTip.Equals("SALDO", StringComparison.OrdinalIgnoreCase)
                 group frs by new
                 { frs.intLote, frs.strProCodcor, frs.stTalCodigo } into g
                 select g.First()).ToDictionary(
                                   frs => new PromLoteXProdTal(frs.intLote, frs.strProCodcor.ToString().Trim(), (int)frs.stTalCodigo),
                                   frs => (decimal)frs.dcCostoUnit)
                    );
        }

    }

    public class InventarioVal
    {

        public static readonly IReadOnlyDictionary<CtCtblXClaseTipo, long> _mapaDeCuentas =
            new Dictionary<CtCtblXClaseTipo, long>
            {
                //Entero
                { new CtCtblXClaseTipo("EN","A"),  10115010001} ,{ new CtCtblXClaseTipo("EN","A+"),  10115010001} ,{ new CtCtblXClaseTipo("EN","B"),  10115010005} ,
                // Cola
                { new CtCtblXClaseTipo("SH","A"),  10115010002} ,{ new CtCtblXClaseTipo("SH","A+"),  10115010002} ,{ new CtCtblXClaseTipo("SH","B"),  10115010003},
                { new CtCtblXClaseTipo("SH","N"),  10115010003} ,{ new CtCtblXClaseTipo("SH","C"),  10115010009},
                // VA
                { new CtCtblXClaseTipo("VA","A"),  10115010004} ,{ new CtCtblXClaseTipo("VA","A+"),  10115010004}, { new CtCtblXClaseTipo("VA","B"),  10115010010},
                { new CtCtblXClaseTipo("VA","N"),  10115010010} ,{ new CtCtblXClaseTipo("VA","C"),  10115010011}
            };
        [Column("Cuenta Contable")]
        public long lgCuentaContable { get; set; }

        [JsonIgnore]
        public int intTipoMovInv { get; set; }

        [Column("Fecha Saldo")]
        public DateTime dtFechaMov { get; set; }

        [JsonIgnore]
        public string strCodTip { get; set; }

        //[Column("Descripcion Saldo")]
        //public string strDescripcion { get; set; }

        [Column("Proceso")]
        public string? strProdDescri { get; set; }

        [Column("Lote")]
        public int intLote { get; set; }

        [Column("Clase")]
        public string? strProClas01 { get; set; }

        [Column("Tipo")]
        public string? strProClas05 { get; set; }

        [Column("Talla")]
        public string? strTalDescri { get; set; }

        [JsonIgnore]
        public short stTalCodigo { get; set; }

        [Column("CodProd")]
        public string strProCodcor { get; set; }

        [Column("Descripcion")]
        public string? strProDesesp { get; set; }

        [JsonIgnore]
        public string strEmbCodigo { get; set; }

        [JsonIgnore]
        public int intMedCodigo { get; set; }

        [Column("Libras")]
        public decimal dcLibras { get; set; }

        [Column("Masters")]
        public decimal dcMasters { get; set; }

        [JsonIgnore]
        public string strCodBod { get; set; }

        [Column("Costo Unitario")]
        public decimal dcCostoUnit { get; set; }

        [Column("Costo Total")]
        public decimal dcCostoTot { get; set; }

        [JsonIgnore]
        public string? strProCod { get; set; }

        }



}


