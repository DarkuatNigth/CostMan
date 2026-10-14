using System;
using System.Collections.Generic;

namespace CostManagement.Aplicación.DTos
{
    public class AuditoriaMaterialEmpaqueDto
    {
        public int intLote { get; set; }
        public string strProducto { get; set; } = "";
        public string strTipoProceso { get; set; } = "";
        public int intCierreTunel { get; set; }

        public int intItem { get; set; }
        public string strItemDescripcion { get; set; } = "";
        public string strGrupoFicha { get; set; } = "";
        public string strLineaActualBD { get; set; } = "";
        public string strGrupoActualBD { get; set; } = "";

        public decimal dcCantidadFicha { get; set; }
        public decimal dcLibrasMaster { get; set; }

        public bool blIncluidoCosto { get; set; }
        public string strReglaEmpaque { get; set; } = "";

        public decimal? dcPrecioCierreTunel { get; set; }
        public DateOnly? dtPrecioCierreTunel { get; set; }

        public decimal? dcPrecioMov2 { get; set; }
        public DateOnly? dtPrecioMov2 { get; set; }

        public decimal? dcPrecioCompra { get; set; }
        public DateOnly? dtPrecioCompra { get; set; }

        public decimal? dcPrecioBodite { get; set; }
        public DateOnly? dtPrecioBodite { get; set; }

        public decimal dcPrecioSeleccionado { get; set; }
        public string strOrigenSeleccionado { get; set; } = "";
        public DateOnly? dtFechaPrecioSeleccionado { get; set; }

        public bool blExisteEnBD { get; set; }
        public bool blExisteEnFichaActual { get; set; } = true;

        public decimal dcPrecioActualBD { get; set; }
        public decimal dcCantidadActualBD { get; set; }
        public string strOrigenActualBD { get; set; } = "";

        public decimal dcCostoItemMaster { get; set; }
        public decimal dcCostoItemLibra { get; set; }

        public string strBandera { get; set; } = "";
        public string strObservacion { get; set; } = "";
    }

    public class AuditoriaMaterialEmpaqueResumenDto
    {
        public int intLote { get; set; }
        public string strProducto { get; set; } = "";
        public string strTipoProceso { get; set; } = "";

        public decimal dcLibras { get; set; }

        public decimal dcCostoUnitarioActualBD { get; set; }
        public decimal dcCostoUnitarioAuditado { get; set; }
        public decimal dcDiferenciaUnitario { get; set; }

        public decimal dcCostoTotalActualBD { get; set; }
        public decimal dcCostoTotalAuditado { get; set; }
        public decimal dcDiferenciaDolares { get; set; }

        public int intCantidadItems { get; set; }
        public int intCantidadBanderas { get; set; }
        public string strBanderas { get; set; } = "";
        public string strEstado { get; set; } = "";
    }

    public class ResultadoAuditoriaMaterialEmpaqueDto
    {
        public List<AuditoriaMaterialEmpaqueResumenDto> lstResumen { get; set; } = new();
        public List<AuditoriaMaterialEmpaqueDto> lstDetalle { get; set; } = new();
    }
}
