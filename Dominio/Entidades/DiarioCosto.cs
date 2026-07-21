using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.EF_Core;
using DocumentFormat.OpenXml.Office2016.Excel;
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

        //[JsonIgnore]
        //public int intTipoMovInv { get; set; }

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

        [JsonIgnore]
        public string? strProClas02 { get; set; }

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

        [NotMapped]
        [JsonIgnore]
        public PromXProdTal objLotePromProdTalKey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public PromLoteXProdTal objLotePromLoteProdTalKey { get; set; }


        [NotMapped]
        [JsonIgnore]
        public LoteRpcKeyXProdTal objProdTalKey { get; set; }

        [JsonIgnore]
        public LoteFrsKey objLoteProdTalKey { get; set; }

        [JsonIgnore]
        public string strCongelInv { get; set; }

        [JsonIgnore]
        public string? strProClas03 { get; set; }



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
                 { rpc.intLoteUnificado, rpc.intCodProd, rpc.intCodTal } into g
                 select g.First())
                .ToDictionary(
                    frs => new PromLoteXProdTal(frs.intLoteUnificado, frs.intCodProd.ToString().Trim(), (int)frs.intCodTal),
                    frs => (decimal)frs.dcCostoTotXLibra)
            );
        }

        /// <summary>Costo promedio ponderado por prod+talla — fuente RPC (para exportaciones).</summary>
        public static ConcurrentDictionary<PromXProdTal, decimal> ConstruirDictPromedioRpc(
            List<MatPrimaReproceso> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<PromXProdTal, decimal>();
            return new ConcurrentDictionary<PromXProdTal, decimal>(lst
                .GroupBy(x => new PromXProdTal(x.intCodProd.ToString().Trim(), (int)x.intCodTal))
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


        /// <summary>Costo promedio ponderado por prod+talla — fuente Diario Costo (EX, VA) (para exportaciones).</summary>
        public static ConcurrentDictionary<PromXProdTal, decimal> ConstruirDictPromedioDiario(
            List<DiarioCosto> lstDiarioCosto)
        {
            if (!lstDiarioCosto.Any()) return new ConcurrentDictionary<PromXProdTal, decimal>();
            HashSet<string> _lstTipCod =
            new(StringComparer.OrdinalIgnoreCase) { "EX", "EV", "SALDO" };
            return new ConcurrentDictionary<PromXProdTal, decimal>(
                lstDiarioCosto
                .Where(x =>/* x.strTipo == "E" &&*/ _lstTipCod.Contains(x.strCodTip) && x.dcLibras >0)
                .GroupBy(x => new PromXProdTal(x.strProCodcor.Trim(), (int)x.stTalCodigo))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.dcCostoTot) / g.Sum(x => x.dcLibras))
                );
        }

        /// <summary>Costo promedio ponderado por prod+talla — fuente FRS, RPC y Saldo (para exportaciones).</summary>
        public static ConcurrentDictionary<PromXProdTal, decimal> ConstruirDictPromedioFrsRpcSld(
            List<LiquidacionResultado> lstLiq, List<MatPrimaReproceso> lstRpc, List<DiarioCosto> lstSld)
        {
            if (lstLiq == null || !lstLiq.Any()) return new ConcurrentDictionary<PromXProdTal, decimal>();

            // 1) Pre-agregar FRS limpiando espacios
            var frsAgg = lstLiq
                .GroupBy(f => new PromXProdTal(f.intCodProd.ToString().Trim(), (int)f.intLidCodTal))
                .ToDictionary(g => g.Key, g => new {
                    dcLibras = g.Sum(f => (decimal)(f.dcLibras)),
                    dcTotalDol = g.Sum(f => (decimal)(f.dcTotalDol ?? 0))
                });

            // 2) Pre-agregar RPC filtrando registros que ya tengan costo final calculado si no se requiere sumarlos
            var rpcAgg = (lstRpc ?? Enumerable.Empty<MatPrimaReproceso>())
                .GroupBy(r => new PromXProdTal(r.intCodProd.ToString().Trim(), r.intCodTal))
                .ToDictionary(g => g.Key, g => new {
                    dcLibras = g.Sum(r => (decimal)r.dbLibras),
                    dcTotalDol = g.Sum(r => (decimal)r.dbCostoTotal)
                });

            // 3) Pre-agregar SLD (Saldo Inicial)
            var sldAgg = (lstSld ?? Enumerable.Empty<DiarioCosto>())
                .Where(s => s.strCodTip.Equals("SALDO", StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => new PromXProdTal(s.strProCodcor.Trim(), (int)s.stTalCodigo))
                .ToDictionary(g => g.Key, g => new {
                    dcLibras = g.Sum(s => s.dcLibras),
                    dcTotalDol = g.Sum(s => s.dcCostoTot)
                });

            var todasLasClaves = new HashSet<PromXProdTal>(frsAgg.Keys);
            todasLasClaves.UnionWith(rpcAgg.Keys);
            todasLasClaves.UnionWith(sldAgg.Keys);

            var result = new ConcurrentDictionary<PromXProdTal, decimal>();

            foreach (var key in todasLasClaves)
            {
                decimal dcLibrasAcum = 0m;
                decimal dcDolaresAcum = 0m;

                if (frsAgg.TryGetValue(key, out var frs))
                {
                    dcLibrasAcum += frs.dcLibras;
                    dcDolaresAcum += frs.dcTotalDol;
                }
                if (rpcAgg.TryGetValue(key, out var rpc))
                {
                    // CRÍTICO: Si detectas que RPC está duplicando a FRS, implementa una lógica de precedencia aquí
                    dcLibrasAcum += rpc.dcLibras;
                    dcDolaresAcum += rpc.dcTotalDol;
                }
                if (sldAgg.TryGetValue(key, out var sld))
                {
                    dcLibrasAcum += sld.dcLibras;
                    dcDolaresAcum += sld.dcTotalDol;
                }

                result[key] = dcLibrasAcum > 0 ? dcDolaresAcum / dcLibrasAcum : 0m;
            }

            return result;
        }


        /// <summary>Costo por libra específico del lote — fuente Saldos.</summary>
        public static ConcurrentDictionary<PromLoteXProdTal, decimal> ConstruirDictPorLoteSld(
            List<DiarioCosto> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<PromLoteXProdTal, decimal>();
            // Filtrar y agrupar asegurando que no sume de más si hay fragmentación de registros
            var dicResultados = lst
                .Where(x => x.strCodTip.Equals("SALDO", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => new { x.intLote, strProd = x.strProCodcor.Trim(), intTalla = (int)x.stTalCodigo })
                .ToDictionary(
                    g => new PromLoteXProdTal(g.Key.intLote, g.Key.strProd, g.Key.intTalla),
                    g => {
                        // En lugar de g.First(), calculamos el costo unitario real promedio del saldo del lote
                        decimal dcLibrasTot = g.Sum(x => x.dcLibras);
                        decimal dcDolaresTot = g.Sum(x => x.dcCostoTot);
                        return dcLibrasTot > 0 ? dcDolaresTot / dcLibrasTot : g.First().dcCostoUnit;
                    }
                );

            return new ConcurrentDictionary<PromLoteXProdTal, decimal>(dicResultados);
        }


        public void InitializeKeys()
        {
            this.objLotePromProdTalKey = new PromXProdTal(this.strProCodcor, (int)this.stTalCodigo);
            this.objLotePromLoteProdTalKey = new PromLoteXProdTal(this.intLote, this.strProCodcor, (int)this.stTalCodigo);
            this.objProdTalKey = new LoteRpcKeyXProdTal(Convert.ToInt32(this.strProCodcor), this.stTalCodigo);
            this.objLoteProdTalKey = new LoteFrsKey(this.intLote, Convert.ToInt32(this.strProCodcor), (int)this.stTalCodigo);
        }

        public void InicializarCamposCost(double dbLidPrecio)
        {
            dcCostoUnit = (decimal)dbLidPrecio;
            dcCostoTot = (decimal)dbLidPrecio * this.dcLibras;
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


        [Column("Tip Inventario")]
        public string strCodTip { get; set; }

        //[Column("Descripcion Saldo")]
        //public string strDescripcion { get; set; }

        [Column("Proceso")]
        public string? strProdDescri { get; set; }

        [Column("Lote")]
        public int intLote { get; set; }

        [Column("Clase")]
        public string? strProClas01 { get; set; }

        [JsonIgnore]
        public string? strProClas02 { get; set; }

        [JsonIgnore]
        public string? strProClas03 { get; set; }

        [JsonIgnore]
        public string? strCongel { get; set; }

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

        [JsonIgnore]
        public string strTipo { get; set; }

        [NotMapped]
        [JsonIgnore]
        public PromXProdTal objLotePromProdTalKey;

        public static List<InventarioVal> GenerarSaldoFinal(List<DiarioCosto> lstDiario)
        {
            if (!lstDiario.Any()) throw new ArgumentException($"La lista de {nameof(GenerarSaldoFinal)} no puede estar vacía.", nameof(lstDiario));

            HashSet<string> lstCodTipo =
            new(StringComparer.OrdinalIgnoreCase) { "I", "E" };
            HashSet<string> lstCodMov =
            new(StringComparer.OrdinalIgnoreCase) { "EX", "VA" };

            // Agrupamos por los campos que definen la unicidad de un ítem en el inventario
            var lstInventario = lstDiario
                .Where(x => lstCodTipo.Contains(x.strTipo) && lstCodMov.Contains(x.strCodTip))
        .GroupBy(d => new
        {
            d.strProCodcor,
            d.intLote,
            d.stTalCodigo,
            d.strProClas01,
            d.strProClas05,
            d.strProdDescri,
            d.strProDesesp,
            d.strTalDescri
        })
        .Select(g =>
        {
            // Creamos la instancia de InventarioVal con el saldo consolidado
            var objInv = new InventarioVal
            {
                strTipo = "F",
                strProCodcor = g.Key.strProCodcor,
                intLote = g.Key.intLote,
                stTalCodigo = g.Key.stTalCodigo,
                strProClas01 = g.Key.strProClas01,
                strProClas05 = g.Key.strProClas05,
                strProdDescri = g.Key.strProdDescri,
                strProDesesp = g.Key.strProDesesp,
                strTalDescri = g.Key.strTalDescri,
                lgCuentaContable = InventarioVal._mapaDeCuentas.GetValueOrDefault(new CtCtblXClaseTipo(g.First().strProClas05, g.First().strProClas02), 000000),
                strCodTip = "FINAL", 
                dcLibras = g.Sum(x => x.dcLibras),
                dcMasters = g.Sum(x => x.dcMasters),
                dcCostoTot = g.Sum(x => x.dcCostoTot),
                dcCostoUnit = g.Average(x => x.dcCostoUnit),
                // Tomamos la fecha más reciente como referencia de saldo
                dtFechaMov = g.Max(x => x.dtFechaMov)
            };

            return objInv;
        })
        // Filtramos aquellos que quedaron en cero absoluto (opcional)
        .Where(x => Math.Abs(x.dcLibras) > 0.0001m)
        .ToList();

            return lstInventario;
        }

        public void InicializarCampos(InfoProd objProd, string TalDescri)
        {
            CtCtblXClaseTipo objKey = new CtCtblXClaseTipo(objProd.strProClas05, objProd.strProClas02);
            strProDesesp = objProd.strProDesesp;
            strProClas01 = objProd.strProClas01;
            strProClas05 = objProd.strProClas05;
            strProClas02 = objProd.strProClas02;
            strProClas03 = objProd.strProClas03;
            strCongel = objProd.strDprDescri;
            strProdDescri = objProd.strProDescri;
            strProCod = objProd.strProCodigo;
            lgCuentaContable = InventarioVal._mapaDeCuentas.GetValueOrDefault(objKey, 000000);
            objLotePromProdTalKey = new PromXProdTal(strProCodcor.Trim(), (int)stTalCodigo);
            strTalDescri = TalDescri;
        }
    }


    public class CostVentUni
    {

        [Column("Tipo Liq")]
        public string strTipoLiq { get; set; }

        [Column("Tipo Saldo")]
        public string strTipo { get; set; }

        [Column("LoteUnificado")]
        public int? intLoteUni { get; set; }

        [Column("Lote")]
        public int intLote { get; set; }

        [Column("CodProd")]
        public int intCodProd { get; set; }

        [Column("Descripcion")]
        public string strDescripcion { get; set; }

        [Column("Talla")]
        public string strTalla { get; set; }

        [JsonIgnore]
        public int intCodTalla { get; set; }

        [Column("Proc Prod")]
        public string strProcProd { get; set; }

        [Column("Agrupacion")]
        public string? strAgrupacion { get; set; }

        [Column("TipoProd")]
        public string strTipoProd { get; set; }

        [Column("Congelamiento")]
        public string strCongelamiento { get; set; }

        [Column("Clase")]
        public string strClase { get; set; }

        [Column("Libras")]
        public decimal dcLibras { get; set; }

        [Column("Costo X Libra")]
        public decimal dcCostoUnit { get; set; }

        [Column("Costo Total")]
        public decimal dcCostoTot { get; set; }

        [JsonIgnore]
        public LoteFrsKey objLoteProdTalKey { get; set; }

        public CostVentUni()
        {

        }

        public CostVentUni(LiquidacionResultado objLiqFrs)
        {
            try
            {
                strTipo = "I";
                strTipoLiq = objLiqFrs.strTipoLiq;
                intCodProd = (int)objLiqFrs.intCodProd;
                strDescripcion = objLiqFrs.strDescripcion;
                strTalla = objLiqFrs.strTalla;
                intLote = objLiqFrs.intLote;
                intCodTalla = (int)objLiqFrs.intLidCodTal;
                strProcProd = objLiqFrs.strProClas03;
                strTipoProd =
                    objLiqFrs.strProClas01 == "CC" && objLiqFrs.strProClas05 == "EN" ? "ENTERO" :
                    objLiqFrs.strProClas01 == "SC" && objLiqFrs.strProClas05 == "SH" ? "COLA" :
                    objLiqFrs.strProClas01 == "CC" && objLiqFrs.strProClas05 == "VA" ? "ENTERO VALOR AGREGADO" :
                    objLiqFrs.strProClas01 == "SC" && objLiqFrs.strProClas05 == "VA" ? "COLA VALOR AGREGADO" :
                    "OTRO";
                strClase = objLiqFrs.strProClas02;
                strCongelamiento = Convert.ToString(objLiqFrs.intProCongela);
                dcLibras = (decimal)objLiqFrs.dcLibras;
                dcCostoUnit = (decimal)objLiqFrs.dcCostoTotXLibra;
                dcCostoTot = (decimal)objLiqFrs.dcTotalDolSum;
                objLoteProdTalKey = new LoteFrsKey(intLote, intCodProd, intCodTalla);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar CostVentUni con MatPrimaReproceso: {ex.Message}", ex);
            }
        }

        public CostVentUni(MatPrimaReproceso objLiqRpc)
        {
            try
            {
                strTipo = "I";
                strTipo = objLiqRpc.strAgrupacion.Contains("1") ? "E" : "I";
                strTipoLiq = objLiqRpc.strTipDescri;
                intLoteUni = objLiqRpc.intLotNumero;
                intLote = objLiqRpc.intLoteUnificado;
                intCodProd = (int)objLiqRpc.intCodProd;
                strDescripcion = objLiqRpc.strDescriProduc;
                strTalla = objLiqRpc.strTalDescri;
                intCodTalla = (int)objLiqRpc.intCodTal;
                strProcProd = objLiqRpc.strProClas03;
                strCongelamiento = objLiqRpc.strCongeProduc;
                strTipoProd = objLiqRpc.strTipoProducto;
                strClase = objLiqRpc.strClaseProd;
                dcLibras = objLiqRpc.strAgrupacion == "2. PROCESADO" ? (decimal)objLiqRpc.dbLibras :
                    -(decimal)objLiqRpc.dbLibras;
                dcCostoUnit = objLiqRpc.strAgrupacion == "2. PROCESADO" ? 
                    (decimal)objLiqRpc.dcCostoTotXLibra : 
                    (decimal)objLiqRpc.dbCostoXSecuencial;
                dcCostoTot = objLiqRpc.strAgrupacion == "2. PROCESADO" ?  
                    (decimal)objLiqRpc.dcTotalDolSum :
                    -(decimal)objLiqRpc.dbCostoTotal;
                strAgrupacion = objLiqRpc.strAgrupacion;
                objLoteProdTalKey = new LoteFrsKey(intLote, intCodProd, intCodTalla);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar CostVentUni con MatPrimaReproceso: {ex.Message}", ex);
            }
        }

        public CostVentUni(InventarioVal objInven)
        {
            try
            {
                strTipo = "I";
                strTipoLiq = "INVENTARIO";
                intLote = objInven.intLote;
                intCodProd = Convert.ToInt32(objInven.strProCodcor);
                strDescripcion = objInven.strProDesesp;
                strTalla = objInven.strTalDescri;
                intCodTalla = (int)objInven.stTalCodigo;
                strProcProd = objInven.strProClas03;
                strTipoProd =
                    objInven.strProClas01 == "CC" && objInven.strProClas05 == "EN" ? "ENTERO" :
                    objInven.strProClas01 == "SC" && objInven.strProClas05 == "SH" ? "COLA" :
                    objInven.strProClas01 == "CC" && objInven.strProClas05 == "VA" ? "ENTERO VALOR AGREGADO" :
                    objInven.strProClas01 == "SC" && objInven.strProClas05 == "VA" ? "COLA VALOR AGREGADO" :
                    "OTRO";
                strCongelamiento = objInven.strCongel;
                strClase = objInven.strProClas02;
                dcLibras = (decimal)objInven.dcLibras;
                dcCostoUnit = (decimal)objInven.dcCostoUnit;
                dcCostoTot = (decimal)objInven.dcCostoTot;
                objLoteProdTalKey = new LoteFrsKey(intLote, intCodProd, intCodTalla);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar CostVentUni con MatPrimaReproceso: {ex.Message}", ex);
            }
        }


        public CostVentUni(DiarioCosto objMovInven)
        {
            try
            {
                strTipo = objMovInven.strTipo;
                strTipoLiq = objMovInven.strProdDescri;
                intLote = objMovInven.intLote;
                intCodProd = Convert.ToInt32(objMovInven.strProCodcor);
                strDescripcion = objMovInven.strProDesesp;
                strTalla = objMovInven.strTalDescri;
                intCodTalla = (int)objMovInven.stTalCodigo;
                strProcProd = objMovInven.strProClas03;
                strTipoProd =
                    objMovInven.strProClas01 == "CC" && objMovInven.strProClas05 == "EN" ? "ENTERO" :
                    objMovInven.strProClas01 == "SC" && objMovInven.strProClas05 == "SH" ? "COLA" :
                    objMovInven.strProClas01 == "CC" && objMovInven.strProClas05 == "VA" ? "ENTERO VALOR AGREGADO" :
                    objMovInven.strProClas01 == "SC" && objMovInven.strProClas05 == "VA" ? "COLA VALOR AGREGADO" :
                    "OTRO";
                strCongelamiento = objMovInven.strCongelInv;
                strClase = objMovInven.strProClas02;
                dcLibras = (decimal)objMovInven.dcLibras;
                dcCostoUnit = (decimal)objMovInven.dcCostoUnit;
                dcCostoTot = (decimal)objMovInven.dcCostoTot;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar CostVentUni con MatPrimaReproceso: {ex.Message}", ex);
            }
        }


        public static ConcurrentDictionary<PromXProdTal, decimal> ConstruirDictPromCostUni(
            List<CostVentUni> lstCostVentUni)
        {
            if (!lstCostVentUni.Any()) return new ConcurrentDictionary<PromXProdTal, decimal>();
            return new ConcurrentDictionary<PromXProdTal, decimal>(
                lstCostVentUni
                //.Where(x =>/* x.strTipo == "E" &&*/ _lstTipCod.Contains(x.strCodTip) && x.dcLibras > 0)
                .GroupBy(x => new PromXProdTal(x.intCodProd.ToString(), x.intCodTalla))
                .ToDictionary(
                    g => g.Key,
                    g => {
                        // En lugar de g.First(), calculamos el costo unitario real promedio del saldo del lote
                        decimal dcLibrasTot = g.Sum(x => x.dcLibras);
                        decimal dcDolaresTot = g.Sum(x => x.dcCostoTot);
                        return dcLibrasTot > 0 ? dcDolaresTot / dcLibrasTot : g.First().dcCostoUnit;
                    }
                )
                );
        }
    }

    public class InfoProd
    {

        public string strProCodcor { get; set; }
        public string strProDesesp { get; set; }
        public string strProClas01 { get; set; }
        public string strProClas02 { get; set; }
        public string strProClas03 { get; set; }
        public string strProClas05 { get; set; }
        public string strProCodigo { get; set; }
        public string strProDescri { get; set; }
        public string strDprDescri { get; set; }

    }

    public class InfoTalProd
    {
        [Column("pro_codcor")]
        public string strProCodcor { get; set; }

        [Column("codTalla")]
        public int intCodTalla { get; set; }

        [Column("descTalla")]
        public string strDescTalla { get; set; }

    }
}


