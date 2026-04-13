using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class SaldoBodegaDto
    {
        [Column("Fecha")]
        public string strFecha { get; set; }

        [Column("Grupo")]
        public string strGrupo { get; set; }

        [Column("Proveedor")]
        public string strProveedor { get; set; }

        [Column("Cod.Bodega")]
        public string strCodBodega { get; set; }

        [Column("Desc.Bodega")]
        public string strDescBodega { get; set; }

        [Column("Tipo Bodega")]
        public string strTipoBodega { get; set; }

        [Column("Producto")]
        public string strProducto { get; set; }

        [Column("Desc.Producto")]
        public string strDescProducto { get; set; }

        [Column("Tipo")]
        public string strTipo { get; set; }

        [Column("Clase")]
        public string strClase { get; set; }

        [Column("Talla")]
        public string strTalla { get; set; }

        [Column("Lote")]
        public long intLote { get; set; }

        [Column("Fecha Lote")]
        public string strFechaLote { get; set; }

        [Column("Cantidad Cajas")]
        public decimal dcCantidadCajas { get; set; }

        [Column("Libras")]
        public double dcLibras { get; set; }

        [Column("out1")]
        public decimal dcOut1 { get; set; }

        public SaldoBodegaDto()
        {

        }

    }

    public class LoteCertificacionDto
    {
        [Column("Lote")]
        public decimal? intLote { get; set; }

        [Column("FechaLote")]
        public string? dtFechaLote { get; set; }

        [Column("Proveedor")]
        public string? strProveedor { get; set; }

        [Column("Piscina")]
        public string? strPiscina { get; set; }

        [Column("Libras")]
        public decimal? dcLibras { get; set; }

        [Column("Kilos")]
        public decimal? dcKilos { get; set; }

        [Column("Producto")]
        public string? strCodProd { get; set; }

        [Column("Descripción")]
        public string? strDescripcion { get; set; }

        [Column("Talla")]
        public string? strTalla { get; set; }

        [Column("Precio Base")]
        public decimal? dcPrecioBase { get; set; }

        [Column("Valor Premio")]
        public decimal? dcValorPremio { get; set; }

        [Column("País")]
        public string? strPais { get; set; }

        [Column("TipoProducto")]
        public string? strTipoProducto { get; set; }

        [Column("Concepto Premio")]
        public string? strConceptoPremio { get; set; }

        [Column("Planta")]
        public string? strPlanta { get; set; }

        [Column("GrupoProveedor")]
        public string? strGrupoProveedor { get; set; }
    }
}
