using CostManagement.Dominio.Entidades;

namespace CostManagement.Dominio.Reglas
{
    public static class PremioDetalleBuilder
    {
        // detalle del premio por línea, solo PROCESADO en reproceso.
        public static List<Dictionary<string, object>> Construir(IEnumerable<LiquidacionResultado>? lstFrs, IEnumerable<MatPrimaReproceso>? lstRpc)
        {

            var lstDetalle = (lstFrs ?? Enumerable.Empty<LiquidacionResultado>())
                .Where(x => !string.IsNullOrWhiteSpace(x.strCertificado))
                .Select(x => new { Origen = "PFR", Certificado = x.strCertificado!.Trim(), Lote = x.intLote, Producto = x.intCodProd, Descripcion = x.strDescripcion, Talla = x.strTalla, Libras = (decimal)x.dcLibras, Premio = x.dcCertificado ?? 0m })
                .Concat((lstRpc ?? Enumerable.Empty<MatPrimaReproceso>())
                    .Where(x => x.strAgrupacion == "2. PROCESADO" && !string.IsNullOrWhiteSpace(x.strCertificado))
                    .Select(x => new { Origen = "RPC", Certificado = x.strCertificado!.Trim(), Lote = x.intLoteUnificado, Producto = (int?)x.intCodProd, Descripcion = x.strDescriProduc, Talla = x.strTalDescri, Libras = (decimal)x.dbLibras, Premio = x.dcCertificado ?? 0m }));

            return lstDetalle.OrderBy(x => x.Origen).ThenBy(x => x.Certificado).ThenBy(x => x.Lote).ThenBy(x => x.Producto)
                .Select(x => new Dictionary<string, object>
                {
                    ["Origen"] = x.Origen, ["Certificado"] = x.Certificado, ["Lote"] = x.Lote, ["Producto"] = x.Producto!,
                    ["Descripcion"] = x.Descripcion!, ["Talla"] = x.Talla!,
                    ["Libras"] = Math.Round(x.Libras, 2), ["Premio"] = Math.Round(x.Premio, 2)
                }).ToList();
        }
    }
}
