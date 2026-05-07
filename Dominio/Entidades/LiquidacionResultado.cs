using CostManagement.Infraestructura.EF_Core;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;

namespace CostManagement.Dominio.Entidades
{
    public class LiquidacionResultado
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


        [Column("Proceso Primario")] // Este nombre será el título del Merge en Excel
        public ProcesoPrimarioDto ProcesoPrimario { get; set; } = new ProcesoPrimarioDto();

        [Column("Proceso Secundario")] // Este nombre será el título del Merge en Excel
        public ProcesoSecundarioDto ProcesoSecundario { get; set; } = new ProcesoSecundarioDto();

        [Column("Proceso Presentacion")] // Este nombre será el título del Merge en Excel
        public ProcesoPresentacionDto ProcesoPresentacion { get; set; } = new ProcesoPresentacionDto();

        [Column("Proceso Congelacion")] // Este nombre será el título del Merge en Excel
        public ProcesoCongelacionDto ProcesoCongelacion { get; set; } = new ProcesoCongelacionDto();

        [Column("Costos Directos")]
        public ProcesoCostDirectoDto ProcesoCostFijo { get; set; } = new ProcesoCostDirectoDto();

        [Column("Costos Indirectos")]
        public ProcesoCostIndirectoDto ProcesoCostIndirecto { get; set; } = new ProcesoCostIndirectoDto();

        [Column("Total Mat Prim")]
        public double? dcTotalDol { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Total Mat Empaque")]
        public decimal? dcCostoTotalMatEmp { get; set; }

        [Column("Total Proceso")]
        public decimal? dcCostTotalProc { get; set; }

        [Column("Total Costos")]
        public decimal dcTotalDolSum { get; set; }
        //[NotMapped]
        //[JsonIgnore]
        [Column("Costo X Libra")]
        public decimal? dcCostoTotXLibra { get; set; }

        [Column("Validador")]
        public decimal dcValidador { get; set; }

        [JsonIgnore]
        public decimal? dcValorCert { get; set; }

        [JsonIgnore]
        public int? intLidCodTal { get; set; }

        [JsonIgnore]
        public decimal? dcLiqPrecio { get; set; }

        [JsonIgnore]
        public string? strProClas06 { get; set; }

        [JsonIgnore]
        public int intProCongela { get; set; }

        [JsonIgnore]
        public bool blBodEsBrine { get; set; }

        [JsonIgnore]
        public decimal? dcCostRecepcion { get; set; }

        [JsonIgnore]
        public decimal? dcCostClasificacion { get; set; }

        [JsonIgnore]
        public decimal? dcCostCajas { get; set; }

        [JsonIgnore]
        public decimal? dcCostDescabezado { get; set; }

        [JsonIgnore]
        public decimal? dcCostBrine { get; set; }

        [JsonIgnore]
        public decimal? dcCostIQF { get; set; }

        [JsonIgnore]
        public decimal? dcCostTunel { get; set; }

        [JsonIgnore]
        public decimal? dcCostRectra { get; set; }

        [JsonIgnore]
        public decimal? dcCostDecorado { get; set; }

        [JsonIgnore]
        public decimal? dcCostPelado { get; set; }

        [JsonIgnore]
        public decimal? dcCostHidratacion { get; set; }

        [JsonIgnore]
        public decimal? dcCostCocido { get; set; }

        [JsonIgnore]
        public decimal? dcCostDirVaria { get; set; }

        [JsonIgnore]
        public decimal? dcCostDirFij { get; set; }

        [JsonIgnore]
        public decimal? dcCostIndVaria { get; set; }

        [JsonIgnore]
        public decimal? dcCostIndFij { get; set; }

        [JsonIgnore]

        public string? strBodCod { get; set; }

        [JsonIgnore]

        public decimal? dcMedCodigo { get; set; }

        [JsonIgnore]

        public string? strEmbCodigo { get; set; }

        [JsonIgnore]

        public decimal? dcRtaCodigo { get; set; }

        [JsonIgnore]

        public decimal dcLotSecuencial { get; set; }

        [NotMapped]
        [JsonIgnore]
        public int intCodCopacking { get; set; }

        //Por ahora solo usado con fresco
        [NotMapped]
        [JsonIgnore]
        public LoteFrsKey objLotkey { get; set; }

        public LiquidacionResultado() { }


        public LiquidacionResultado(
            string? tipoLiq, decimal? lote, int? mes, DateTime? fechaLote,
            string? planta, string? proveedor, string? piscina,
            //decimal totalCanenv,
            //decimal totalCantid,
            //decimal totalPeso,
            //decimal totalFactor,
            double masters, double libras, string? tipPro,
            DateTime fechaLiq, int horaLiq, string? codProd, string? descripcion,
            string? talla, string? certificado, string? bodDescri,
            string? clpGrupo, string? paiDescri, string? proClas02,
            string? proClasePago, string? clasificadora, DateTime? inicioLiq,
            DateTime? finLiq, int CodTal, //decimal ?LiqPrecio,
            string? clasifi, string? grupo, string? subGrupo,
            string? proClas01, string? proClas05, string? planta2, string? proClas03,
            string proClas06, decimal? proCongela, bool? bodEsBrine, decimal? certPrecio, decimal LotSecuencial, string CodCopacking

            )
        {
            var tmpTipoCopacking = new[]
            {
                    new TipoCopacking("0", "NO COPACKING"),
                    new TipoCopacking("1", "COPACKING EN SONGA"),
                    new TipoCopacking("2", "COPACKING EN OTRAS CIAS.")
                }.AsQueryable();
            var dictTipoCopacking = tmpTipoCopacking
                .ToDictionary(x => x.Codigo, x => x.Descripcion);
            strTipoLiq = tipoLiq;
            intLote = (int)Convert.ToInt64(lote); // Maneja '20A' sin romper
            dcLotSecuencial = LotSecuencial;
            intMes = mes;
            dtFechaLote = DateOnly.FromDateTime((DateTime)fechaLote!);
            strPlanta = planta;
            strProveedor = proveedor;
            strRloPiscin = piscina;
            //dcMasters = Math.Round(masters, 2);
            dcMasters = masters;
            //dcLibras = Math.Truncate(libras * 100) / 100;
            //dcLibras = Math.Round(libras, 2);
            dcLibras = libras;
            strTipPro = tipPro;
            dtFechaLiq = DateOnly.FromDateTime(fechaLiq);
            intHoraLiq = horaLiq;
            intCodProd = (int?)Convert.ToInt64(codProd); // Maneja '20A' sin romper
            strDescripcion = descripcion;
            strTalla = talla;//?.Replace('/', '-');
            strCertificado = certificado;
            strBodDescri = bodDescri;
            strClpGrupo = clpGrupo?.Trim();
            strPaiDescri = paiDescri;
            strProClas02 = proClas02;
            strProClasePago = proClasePago;
            strRloPiscin = piscina;
            dtInicioLiquidacion = inicioLiq ?? DateTime.MinValue;
            dtFinLiquidacion = finLiq;
            intLidCodTal = CodTal;
            strLidClasificadora = clasificadora;
            strClasificadora = clasifi;
            strGrupo = grupo;
            strSubGrupo = subGrupo;
            strProClas01 = proClas01;
            strProClas05 = proClas05;
            strPlanta2 = planta2;
            strProClas03 = proClas03;
            strProClas06 = proClas06;
            intProCongela = (int)proCongela;
            blBodEsBrine = bodEsBrine != null ? bodEsBrine.Value : false;

            // 1. FechaTurno: Si la hora es < 08:00, es el día anterior
            if (intHoraLiq >= 0 && intHoraLiq < 8)
            {
                dtFechaTurno = DateOnly.FromDateTime(fechaLiq.AddDays(-1));
            }
            else
            {
                dtFechaTurno = DateOnly.FromDateTime(fechaLiq);
            }

            // 2. Turno: De 08:00 a 20:59 es 'D' (Día), resto 'N' (Noche)
            strTurno = intHoraLiq >= 8 && intHoraLiq <= 20 ? "D" : "N";

            // 3. Horas Liquidación: Diferencia entre inicio y fin
            if (inicioLiq.HasValue && finLiq.HasValue)
            {
                var diff = finLiq.Value - inicioLiq.Value;
                dcHorasLiquidacion = Math.Round((decimal)diff.TotalHours, 2);
            }
            //this.InitializePrecioCompraAndTotalDol();
            // Inicializamos valores de precio para el proceso posterior en memoria
            dcPrecioCompra = 0.0000;
            dcTotalDol = 0.0000;
            InitializePrecioCert(certPrecio);
            this.strTipoCopacking = dictTipoCopacking.GetValueOrDefault(CodCopacking, "");

            this.intCodCopacking = Convert.ToInt32(CodCopacking);
        }

        public LiquidacionResultado(
    string? tipoLiq, decimal? lote, int? mes, DateTime? fechaLote,
    string? planta, string? proveedor, string? piscina,
    double masters, double libras, string? tipPro,
    DateTime fechaLiq, int horaLiq, string? codProd, string? descripcion,
    string? talla, string? certificado, string? bodDescri,
    string? clpGrupo, string? paiDescri, string? proClas02,
    string? proClasePago, string? clasificadora, DateTime? inicioLiq,
    DateTime? finLiq, int CodTal, //decimal ?LiqPrecio,
    string? clasifi, string? grupo, string? subGrupo,
    string? proClas01, string? proClas05, string? planta2, string? proClas03,
    string proClas06, decimal? proCongela, bool? bodEsBrine, decimal? certPrecio, //decimal? PrecioCompra,
    string tipCola , decimal? RtaCodigo, decimal? TidCodigo, //decimal? LbsCajasRetra, 
    string BodCodigo, decimal? MedCodigo, string EmbCodigo, string CodCopacking
    )
        {
            try
            {
                var tmpTipoCopacking = new[]
                {
                    new TipoCopacking("0", "NO COPACKING"),
                    new TipoCopacking("1", "COPACKING EN SONGA"),
                    new TipoCopacking("2", "COPACKING EN OTRAS CIAS.")
                }.AsQueryable();
                var dictTipoCopacking = tmpTipoCopacking
                    .ToDictionary(x => x.Codigo, x => x.Descripcion);
                strTipoLiq = tipoLiq;
                intLote = (int)Convert.ToInt64(lote); // Maneja '20A' sin romper
                intMes = mes;
                dtFechaLote = DateOnly.FromDateTime((DateTime)fechaLote!);
                strPlanta = planta;
                strProveedor = proveedor;
                strRloPiscin = piscina;
                //dcMasters = Math.Round(masters, 2);
                dcMasters = masters;
                dcLibras = libras;
                strTipPro = tipPro;
                dtFechaLiq = DateOnly.FromDateTime(fechaLiq);
                intHoraLiq = horaLiq;
                intCodProd = (int?)Convert.ToInt64(codProd); // Maneja '20A' sin romper
                strDescripcion = descripcion;
                strTalla = talla;
                strCertificado = certificado;
                strBodDescri = bodDescri;
                strClpGrupo = clpGrupo?.Trim();
                strPaiDescri = paiDescri;
                strProClas02 = proClas02;
                strProClasePago = proClasePago;
                strRloPiscin = piscina;
                dtInicioLiquidacion = inicioLiq ?? DateTime.MinValue;
                dtFinLiquidacion = finLiq;
                intLidCodTal = CodTal;
                strLidClasificadora = clasificadora;
                //dcLiqPrecio = LiqPrecio;
                strClasificadora = clasifi;
                strGrupo = grupo;
                strSubGrupo = subGrupo;
                strProClas01 = proClas01;
                strProClas05 = proClas05;
                strPlanta2 = planta2;
                strProClas03 = proClas03;
                strProClas06 = proClas06;
                intProCongela = (int)proCongela;
                blBodEsBrine = bodEsBrine != null ? bodEsBrine.Value : false;
                decimal dcValCertPrecio = certPrecio ?? 0.0m;
                dcCertificado = Math.Round(dcValCertPrecio * (decimal)libras, 2);
                dcValorCert = dcValCertPrecio;
                strTipCola = tipCola;
                dcRtaCodigo = RtaCodigo;
                strBodCod = BodCodigo;
                dcMedCodigo = MedCodigo;
                strEmbCodigo = EmbCodigo;
                dcLibrasDecorado = TidCodigo != 1 && dcLibras != null ? (decimal)dcLibras: 0m;
                if (intHoraLiq >= 0 && intHoraLiq < 8)
                {
                    dtFechaTurno = DateOnly.FromDateTime(fechaLiq.AddDays(-1));
                }
                else
                {
                    dtFechaTurno = DateOnly.FromDateTime(fechaLiq);
                }

                // 2. Turno: De 08:00 a 20:59 es 'D' (Día), resto 'N' (Noche)
                strTurno = intHoraLiq >= 8 && intHoraLiq <= 20 ? "D" : "N";

                // 3. Horas Liquidación: Diferencia entre inicio y fin
                if (inicioLiq.HasValue && finLiq.HasValue)
                {
                    var diff = finLiq.Value - inicioLiq.Value;
                    dcHorasLiquidacion = Math.Round((decimal)diff.TotalHours, 2);
                }
                //dcLiqPrecio = PrecioCompra;

                //InitializePrecioCompraAndTotalDol();
                InitializePrecioCert(certPrecio);
                this.strTipoCopacking = dictTipoCopacking.GetValueOrDefault(CodCopacking, "");
                this.intCodCopacking = Convert.ToInt32(CodCopacking);
                this.objLotkey = new LoteFrsKey( intLote, (int)this.intCodProd, (int)this.intLidCodTal);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones: Loguear el error y asignar valores por defecto
                Console.WriteLine($"Error al crear LiquidacionResultadoDto: {ex.Message}");
            }
        }
        public void InitializePrecioCompraAndTotalDol()
        {
            if (dcLiqPrecio != null)
            {
                // Aplicamos la lógica del CASE del SQL original
                // Si es 'cola' mantenemos el precio, si no, lo dividimos para el factor de conversión
                double precioBase = (double)dcLiqPrecio;
                double dcValorPrecioCompra = 0.0000;
                if (strTipPro?.Trim().ToLower() == "cola")
                {
                    this.dcPrecioCompra = Math.Round(precioBase, 4, MidpointRounding.ToZero);
                    dcValorPrecioCompra = Math.Truncate(dcLibras * (this.dcPrecioCompra ?? 0) * 100) / 100;
                }
                else
                {
                    // Usamos 2.20462 para mayor precisión en conversión de Libras
                    dcValorPrecioCompra = Math.Truncate(dcLibras * (precioBase / 2.2046) * 100) / 100;
                    this.dcPrecioCompra =
                            Math.Truncate(precioBase / 2.2046 * 10000) / 10000;
                }
                this.dcTotalDol = dcValorPrecioCompra;
            }
            else
            {
                this.dcPrecioCompra = 0;
                this.dcTotalDol = Math.Round(dcLibras * (this.dcPrecioCompra ?? 0), 2, MidpointRounding.ToZero);
            }

            this.dcMasters = Math.Round((double)dcMasters, 2);
            this.dcLibras = Math.Round(dcLibras, 2);
        }

        private void InitializePrecioCert(decimal? certPrecio)
        {
            decimal dcValCertPrecio = certPrecio ?? 0.0m;
            dcCertificado = Math.Round(dcValCertPrecio * (decimal)dcLibras, 2);
            dcValorCert = dcValCertPrecio;
        }
        public void MergeValorizacion(LiquidacionResultado valorizado)
        {
            if (valorizado == null) return;

            dcCostoTotXLibra = valorizado.dcCostoTotXLibra;
        }
        public void MergeValorizacion(MatPrimaReproceso objValor)
        {

            if (objValor == null) return;

            // Sobreescribimos solo los campos de cálculo/costo que vienen de la tabla valorizada
            dcPrecioCompra = (double?)objValor.dbCostoXSecuencial;
            dcTotalDol = (double?)objValor.dbCostoTotal;
            dcCostoTotXLibra = objValor.dcCostoTotXLibra;
            dcCostoMatEmpaque = objValor.dcCostoMatEmpaque;
            dcCostoTotalMatEmp = objValor.dcCostoTotalMatEmp;
            dcCostoMatEmpaque = objValor.dcCostoMatEmpaque;
            ProcesoCongelacion = objValor.ProcesoCongelacion;
            ProcesoPresentacion = objValor.ProcesoPresentacion;
            ProcesoSecundario = objValor.ProcesoSecundario;
            ProcesoPrimario = objValor.ProcesoPrimario;
            ProcesoCostIndirecto = objValor.ProcesoCostIndirecto;
            ProcesoCostFijo = objValor.ProcesoCostFijo;
            dcCostoCopacking = objValor.dcCostoCopacking;
            dcTarifaProc = objValor.dcTarifaProc;

        }

        /// <summary>
        /// Genera un diccionario con el costo promedio por Lote, Producto y Talla a partir de una lista de materia prima fresco.
        /// </summary>
        /// <param name="lstMatPrimaFrs">Lista de objetos con los costos.</param>
        /// <returns>Diccionario donde la llave es (Lote, Producto, Talla) y el valor es el Precio Promedio calculado.</returns>
        public static Dictionary<(int Lote, int? Producto, int? Talla), decimal?> GenerarDiccionarioCostoXTalla(IEnumerable<LiquidacionResultado> lstMatPrimaFrs)
        {
            if (lstMatPrimaFrs == null)
                return new Dictionary<(int, int?, int?), decimal?>();

            return (
                            from lct in lstMatPrimaFrs
                            let dcCostMatEmp = lct.dcCostoMatEmpaque == null ? 0m : lct.dcCostoMatEmpaque
                            let dcCertificado = lct.dcValorCert == null ? 0m : lct.dcValorCert
                            group new { lct, dcCostMatEmp, dcCertificado } by new
                            {
                                Lote = lct.intLote,
                                Producto = lct.intCodProd,
                                Talla = lct.intLidCodTal
                            } into g
                            select new
                            {
                                Key = (g.Key.Lote, g.Key.Producto, g.Key.Talla),
                                PrecioPromedio = g.Average(x => x.lct.dcCostoTotXLibra)
                            }
                        ).ToDictionary(x => x.Key, x => x.PrecioPromedio);
        }

        public static Dictionary<LoteFrsKey, List<LiquidacionResultado>> GenerarDiccionarioLbsRetra(IEnumerable<LiquidacionResultado> lstMatPrimaFrs)
        {
            if (lstMatPrimaFrs == null)
                return new Dictionary<LoteFrsKey, List<LiquidacionResultado>>();

            return lstMatPrimaFrs
                    .GroupBy(lct => new LoteFrsKey(
                        lct.intLote,
                        (int)lct.intCodProd,
                        (int)lct.intLidCodTal
                    ))
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList() // Almacena todos los objetos que coinciden con la clave
                    );
        }

        public static Dictionary<LoteRpcKeyLoteXProd, Queue<List<LiquidacionResultado>>> GenerarDiccionarioProcMatEmp(IEnumerable<LiquidacionResultado> lstMatPrimaFrs)
        {
            if (lstMatPrimaFrs == null)
                return new Dictionary<LoteRpcKeyLoteXProd, Queue<List<LiquidacionResultado>>>();

            return lstMatPrimaFrs
                                .Where(x => x.strProClas03 == "PT")
                                .GroupBy(x => new LoteRpcKeyLoteXProd(x.intLote, x.intCodProd.ToString().Trim()))
                                .ToDictionary(
                                    g => g.Key,
                                    g => new Queue<List<LiquidacionResultado>>(
                                        g.GroupBy(x => x.intLidCodTal)
                                         .Select(tg => tg.ToList())
                                    )
                                );
        }


    }
}
    public class ProcesoPrimarioDto
    {
        [Column("Recepcion")]
        public decimal? dcRecepcion { get; set; }

        [Column("Clasificacion")]
        public decimal? dcClasificacion { get; set; }

        [Column("Cajas")]
        public decimal? dcCajas { get; set; }
    }

    public class ProcesoPresentacionDto
    {
        [Column("Decorado")]
        public decimal? dcDecorado { get; set; }

        [Column("Retractilado")]
        public decimal? dcRetractilado { get; set; }
    }

    public class ProcesoCongelacionDto
    {
        [Column("Brine")]
        public decimal? dcBrine { get; set; }

        [Column("IQF")]
        public decimal? dcIQF { get; set; }

        [Column("Tunel")]
        public decimal? dcTunel { get; set; }
    }

    public class ProcesoSecundarioDto
    {

        [Column("Descabezado")]
        public decimal? dcDescabezado { get; set; }

        [Column("Pelado")]
        public decimal? dcPelado { get; set; }

        [Column("Hidratacion")]
        public decimal? dcHidratacion { get; set; }

        [Column("Cocido")]
        public decimal? dcCocido { get; set; }
    }


    public class ProcesoCostDirectoDto
    {
        [Column("C.D.Fijos")]
        public decimal? dcCostoFijo { get; set; }

        [Column("C.D.Variables")]
        public decimal? dcCostoVariable { get; set; }
    }

    public class ProcesoCostIndirectoDto
    {
        [Column("C.I.Fijos")]
        public decimal? dcCostoFijo { get; set; }

        [Column("C.I.Variables")]
        public decimal? dcCostoVariable { get; set; }
    }

