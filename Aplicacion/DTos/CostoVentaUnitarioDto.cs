using CostManagement.Aplicación.DTos;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Aplicacion.DTos
{
    // columnas exclusivas del libro de cinco hojas.
    public class CostoVentaMovimientoHojaDto
    {
        [Column("Origen")]
        public string strOrigen { get; set; } = string.Empty;

        [Column("Tipo Liq")]
        public string strTipoLiq { get; set; } = string.Empty;

        [Column("Tipo Saldo")]
        public string strTipo { get; set; } = string.Empty;

        [Column("LoteUnificado")]
        public int? intLoteUni { get; set; }

        [Column("Lote")]
        public int intLote { get; set; }

        [Column("CodProd")]
        public int intCodProd { get; set; }

        [Column("Descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [Column("Talla")]
        public string strTalla { get; set; } = string.Empty;

        [Column("Proc Prod")]
        public string strProcProd { get; set; } = string.Empty;

        [Column("Agrupacion")]
        public string strAgrupacion { get; set; } = string.Empty;

        [Column("TipoProd")]
        public string strTipoProd { get; set; } = string.Empty;

        [Column("Congelamiento")]
        public string strCongelamiento { get; set; } = string.Empty;

        [Column("Clase")]
        public string strClase { get; set; } = string.Empty;

        [Column("Libras")]
        public decimal dcLibras { get; set; }

        [Column("Costo X Libra")]
        public decimal dcCostoUnit { get; set; }

        [Column("Costo Total")]
        public decimal dcCostoTot { get; set; }

        [Column("Revisar")]
        public string strRevisar { get; set; } = string.Empty;

        // fuente de la valoración del movimiento.
        [Column("Fuente del Costo")]
        public string strFuenteCosto { get; set; } = string.Empty;

        [Column("Cod Movimiento")]
        public string strCodMov { get; set; } = string.Empty;

        [Column("Descripcion Movimiento")]
        public string strDescriMov { get; set; } = string.Empty;

        [Column("Movimiento")]
        public string strMovimiento { get; set; } = string.Empty;

        [Column("Fecha")] public DateTime? dtFecha { get; set; }
        [Column("Masters")] public decimal? dcMasters { get; set; }
        [Column("Fecha Corte")] public DateOnly? dtFechaCorte { get; set; }

        [JsonIgnore]
        public int intCodTalla { get; set; }

        // copia independiente para no cambiar el origen de las otras hojas.
        public CostoVentaMovimientoHojaDto CopiarParaInventarioFinal()
        {
            var objCopia = (CostoVentaMovimientoHojaDto)MemberwiseClone();
            objCopia.strOrigen = string.IsNullOrWhiteSpace(strCodMov) ? "COSTO DETALLADO" : strOrigen;
            objCopia.strMovimiento = strTipo == "I" ? "INGRESO" : "EGRESO";
            return objCopia;
        }
    }

    // columnas exclusivas del libro de cinco hojas.
    public class CostoVentaGlobalProdTallaDto
    {
        [Column("CodProd")]
        public int intCodProd { get; set; }

        [Column("Talla")]
        public string strTalla { get; set; } = string.Empty;

        [Column("Descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [Column("TipoProd")]
        public string strTipoProd { get; set; } = string.Empty;

        [Column("Congelamiento")]
        public string strCongelamiento { get; set; } = string.Empty;

        [Column("Clase")]
        public string strClase { get; set; } = string.Empty;

        [Column("Libras Totales")]
        public decimal dcLibrasTotales { get; set; }

        [Column("Costo Total")]
        public decimal dcCostoTot { get; set; }

        [Column("Costo Global")]
        public decimal? dcCostoGlobal { get; set; }

        // fuente del costo global aplicado.
        [Column("Fuente del Costo")]
        public string strFuenteCosto { get; set; } = string.Empty;

        [Column("Libras Ingreso")]
        public decimal dcLibrasIngreso { get; set; }

        [Column("Costo Ingreso")]
        public decimal dcCostoIngreso { get; set; }

        [Column("Libras Egreso")]
        public decimal dcLibrasEgreso { get; set; }

        [Column("Costo Egreso")]
        public decimal dcCostoEgreso { get; set; }

        [Column("Cantidad Movimientos")]
        public int intCantidadMovimientos { get; set; }

        [Column("Revisar")]
        public string strRevisar { get; set; } = string.Empty;

        [JsonIgnore]
        public int intCodTalla { get; set; }
    }
    // tercera hoja calculada con los signos originales del detalle.
    public class CostoVentaGlobalHojaDto
    {
        [Column("CodProd")]
        public int intCodProd { get; set; }

        [JsonIgnore]
        public int intCodTalla { get; set; }

        [Column("Talla")]
        public string strTalla { get; set; } = string.Empty;

        [Column("Descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [Column("TipoProd")]
        public string strTipoProd { get; set; } = string.Empty;

        [Column("Congelamiento")]
        public string strCongelamiento { get; set; } = string.Empty;

        [Column("Clase")]
        public string strClase { get; set; } = string.Empty;

        [Column("Libras Ingreso")]
        public decimal dcLibrasIngreso { get; set; }

        [Column("Costo Ingreso")]
        public decimal dcCostoIngreso { get; set; }

        [Column("Libras Egreso")]
        public decimal dcLibrasEgreso { get; set; }

        [Column("Costo Egreso")]
        public decimal dcCostoEgreso { get; set; }

        [Column("Libras Netas")]
        public decimal dcLibrasNetas { get; set; }

        [Column("Dolares Netos")]
        public decimal dcDolaresNetos { get; set; }

        [Column("Costo Ponderado Global")]
        public decimal? dcCostoPonderadoGlobal { get; set; }

        [Column("Costo Solo Ingresos")]
        public decimal? dcCostoSoloIngresos { get; set; }

        [Column("Cantidad Movimientos")]
        public int intCantidadMovimientos { get; set; }

        [Column("Revisar")]
        public string strRevisar { get; set; } = string.Empty;
    }

    /// <summary>
    /// Consolidado del disponible ANTES DE VENTAS por Código de Producto + Talla.
    /// Este es el "Costo de Venta Unitario" que posteriormente se usará para valorizar ventas.
    /// </summary>
    public class CostoVentaUnitarioDto
    {
        [Column("CodProd")]
        public int intCodProd { get; set; }

        [JsonIgnore]
        public int intCodTalla { get; set; }

        [Column("Talla")]
        public string strTalla { get; set; } = string.Empty;

        [Column("Descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [Column("TipoProd")]
        public string strTipoProd { get; set; } = string.Empty;

        [Column("Congelamiento")]
        public string strCongelamiento { get; set; } = string.Empty;

        [Column("Clase")]
        public string strClase { get; set; } = string.Empty;

        [Column("Libras Disponibles")]
        public decimal dcLibrasDisponible { get; set; }

        [Column("Costo Total Disponible")]
        public decimal dcCostoTotalDisponible { get; set; }

        [Column("Costo Venta Unitario")]
        public decimal dcCostoVentaUnitario { get; set; }

        [Column("Cantidad Lotes")]
        public int intCantidadLotes { get; set; }
    }

    /// <summary>
    /// Explica de qué lotes está compuesto el disponible previo a ventas.
    /// La clave física es Lote efectivo + CodProd + CodTalla.
    /// Para RECIBIDO de reproceso el lote efectivo es intLoteOrigen.
    /// </summary>
    public class CostoVentaDisponibleLoteDto
    {
        [Column("Tipo Proceso")]
        public string? strTipoProceso { get; set; }
        [Column("Lote")]
        public int? intLote { get; set; }

        [Column("CodProd")]
        public int intCodProd { get; set; }

        [JsonIgnore]
        public int intCodTalla { get; set; }

        [Column("Talla")]
        public string strTalla { get; set; } = string.Empty;

        [Column("Descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [Column("TipoProd")]
        public string strTipoProd { get; set; } = string.Empty;

        [Column("Congelamiento")]
        public string strCongelamiento { get; set; } = string.Empty;

        [Column("Clase")]
        public string strClase { get; set; } = string.Empty;

        [Column("Libras Ingreso")]
        public decimal dcLibrasIngreso { get; set; }

        [Column("Libras Egreso")]
        public decimal dcLibrasEgreso { get; set; }

        [Column("Libras Disponibles")]
        public decimal dcLibrasDisponible { get; set; }

        [Column("Costo Ingreso")]
        public decimal dcCostoIngreso { get; set; }

        [Column("Costo Egreso")]
        public decimal dcCostoEgreso { get; set; }

        [Column("Costo Total Disponible")]
        public decimal dcCostoTotalDisponible { get; set; }

        [Column("Costo X Libra Lote")]
        public decimal dcCostoXLibra { get; set; }

        [Column("Cantidad Movimientos")]
        public int intCantidadMovimientos { get; set; }

        [Column("Estado Saldo")]
        public string strEstadoSaldo { get; set; } = string.Empty;
    }

    // DTO exclusivo del Excel; conserva las columnas del disponible y agrega Revisar al final.
    public class CostoVentaDisponibleMpDto
    {
        [Column("Tipo Proceso")]
        public string? strTipoProceso { get; set; }
        [Column("Lote")]
        public int? intLote { get; set; }

        [Column("CodProd")]
        public int intCodProd { get; set; }

        [JsonIgnore]
        public int intCodTalla { get; set; }

        [Column("Talla")]
        public string strTalla { get; set; } = string.Empty;

        [Column("Descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [Column("TipoProd")]
        public string strTipoProd { get; set; } = string.Empty;

        [Column("Congelamiento")]
        public string strCongelamiento { get; set; } = string.Empty;

        [Column("Clase")]
        public string strClase { get; set; } = string.Empty;

        [Column("Libras Ingreso")]
        public decimal dcLibrasIngreso { get; set; }

        [Column("Libras Egreso")]
        public decimal dcLibrasEgreso { get; set; }

        [Column("Libras Disponibles")]
        public decimal dcLibrasDisponible { get; set; }

        [Column("Costo Ingreso")]
        public decimal dcCostoIngreso { get; set; }

        [Column("Costo Egreso")]
        public decimal dcCostoEgreso { get; set; }

        [Column("Costo Total Disponible")]
        public decimal dcCostoTotalDisponible { get; set; }

        [Column("Costo X Libra Lote")]
        public decimal dcCostoXLibra { get; set; }

        [Column("Cantidad Movimientos")]
        public int intCantidadMovimientos { get; set; }

        [Column("Estado Saldo")]
        public string strEstadoSaldo { get; set; } = string.Empty;
        [Column("Revisar")]
        public string strRevisar { get; set; } = string.Empty;
    }

    /// <summary>
    /// No se devuelve al front. Sirve para auditoría/log del cálculo.
    /// </summary>
    public class CostoVentaDisponibleExcepcionDto
    {
        public string strTipoExcepcion { get; set; } = string.Empty;
        public int? intLoteOriginal { get; set; }
        public int? intLoteOrigen { get; set; }
        public int? intLoteDisponible { get; set; }
        public int intCodProd { get; set; }
        public int intCodTalla { get; set; }
        public string strTalla { get; set; } = string.Empty;
        public decimal dcLibras { get; set; }
        public decimal dcCostoTotal { get; set; }
        public string strDetalle { get; set; } = string.Empty;
    }

    /// <summary>
    /// Diferencia entre el universo original de CostVentUni y el consolidado
    /// reconstruido desde los lotes disponibles.
    /// </summary>
    public class CostoVentaDisponibleCuadreDto
    {
        public int intCodProd { get; set; }
        public int intCodTalla { get; set; }
        public string strTalla { get; set; } = string.Empty;
        public decimal dcLibrasBase { get; set; }
        public decimal dcLibrasDisponible { get; set; }
        public decimal dcDiferenciaLibras { get; set; }
        public decimal dcCostoBase { get; set; }
        public decimal dcCostoDisponible { get; set; }
        public decimal dcDiferenciaCosto { get; set; }
    }

    /// <summary>
    /// Auditoría interna. No forma parte de Table/Table1.
    /// </summary>
    public class CostoVentaDisponibleAuditoriaDto
    {
        public int intTotalFilasBase { get; set; }
        public int intTotalProdTalla { get; set; }
        public int intTotalClavesLote { get; set; }
        public int intTotalClavesNegativas { get; set; }
        public decimal dcLibrasClavesNegativas { get; set; }
        public int intTotalRecibidosSinLoteOrigen { get; set; }
        public decimal dcLibrasRecibidosSinLoteOrigen { get; set; }
        public int intTotalEgresosSinIngreso { get; set; }
        public int intTotalSignosInconsistentes { get; set; }
        public int intTotalTipoSaldoInvalido { get; set; }
        public int intTotalDiferenciasCuadre { get; set; }
        public bool blCuadra { get; set; }

        public List<CostoVentaDisponibleExcepcionDto> lstExcepciones { get; set; } = new();
        public List<CostoVentaDisponibleCuadreDto> lstDiferenciasCuadre { get; set; } = new();
    }

    /// <summary>
    /// Resultado usado por el Feature. El Controller solo expone:
    /// Table  = lstCostoVentaUnitario
    /// Table1 = lstDisponibleLote
    /// </summary>
    public class CostoVentaUnitarioResultadoDto
    {
        public List<CostoVentaUnitarioDto> lstCostoVentaUnitario { get; set; } = new();
        public List<CostoVentaDisponibleLoteDto> lstDisponibleLote { get; set; } = new();
        public CostoVentaDisponibleAuditoriaDto objAuditoria { get; set; } = new();
    }
}
