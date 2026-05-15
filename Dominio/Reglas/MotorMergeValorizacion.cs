using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;

namespace CostManagementService.Dominio.Reglas
{
    public class MotorMergeValorizacion
    {
        private readonly ILogger _objLogger;

        public MotorMergeValorizacion(ILogger objLogger)
        {
            _objLogger = objLogger;
        }

        /// <summary>
        /// Merge Fresco: asigna dcCostoTotXLibra desde la tabla de cache
        /// (TbMateriaPrimaFrescoValorizada) sobre los registros crudos de fresco.
        /// Llave: LoteFrsKey(intLote, intCodProd, intCodTal)
        /// </summary>
        public void MergeFrescoValorizado(
            List<LiquidacionResultado> lstFrescoCrudo,
            List<LiquidacionResultado> lstYaValorizados)
        {
            if (lstYaValorizados == null || !lstYaValorizados.Any()) return;

            // Lookup por LoteFrsKey — record con GetHashCode optimizado
            var lookupCache = lstYaValorizados
                .ToLookup(v => v.objLotkey);

            // Diccionario de enumeradores para consumir 1:1 en orden
            var dictEnumeradores = lookupCache
                .ToDictionary(g => g.Key, g => g.GetEnumerator());

            int intMergeados = 0;

            try
            {
                foreach (var itemCrudo in lstFrescoCrudo)
                {
                    if (dictEnumeradores.TryGetValue(itemCrudo.objLotkey, out var enumerador) && enumerador.MoveNext())
                    {
                        itemCrudo.MergeValorizacion(enumerador.Current);
                        intMergeados++;
                    }
                }
            }
            finally
            {
                foreach (var e in dictEnumeradores.Values) e.Dispose();
            }

            _objLogger.LogInformation(
                "[MotorMergeValorizacion.MergeFresco] {Mergeados}/{Total} registros fresco valorizados desde cache.",
                intMergeados, lstFrescoCrudo.Count);
        }

        /// <summary>
        /// Merge RPC: asigna campos de costo desde MatPrimaReproceso (intermediario)
        /// sobre los registros crudos de RPC (LiquidacionResultado de ObtenerMatPrimValRpcsXRangoFecha).
        /// Llave: LoteRpcValKey(intSecuencialLote, intLoteUnificado, intCodProd, intCodTal)
        /// </summary>
        public void MergeReproValorizado(
            List<LiquidacionResultado> lstRpcCrudo,
            List<MatPrimaReproceso> lstReproValorizado)
        {
            if (lstReproValorizado == null || !lstReproValorizado.Any()) return;

            // Lookup por LoteRpcValKey — 4 campos para evitar colisiones
            var lookupCache = lstReproValorizado
                .ToLookup(v => v.objLotRpc);

            var dictEnumeradores = lookupCache
                .ToDictionary(g => g.Key, g => g.GetEnumerator());

            int intMergeados = 0;

            try
            {
                foreach (var itemCrudo in lstRpcCrudo)
                {

                    if (dictEnumeradores.TryGetValue(itemCrudo.objLotRpc, out var enumerador) && enumerador.MoveNext())
                    {
                        itemCrudo.MergeValorizacion(enumerador.Current);
                        intMergeados++;
                    }
                }
            }
            finally
            {
                foreach (var e in dictEnumeradores.Values) e.Dispose();
            }

            //_objLogger.LogInformation(
            //    "[MotorMergeValorizacion.MergeRepro] {Mergeados}/{Total} registros RPC valorizados desde cache.",
            //    intMergeados, lstRpcCrudo.Count);
        }

        public void MergeInventarioValorizado(List<InventarioVal> lstInvVal ,List<DiarioCosto> lstCuadre)
        {
            if (lstCuadre.Any() && lstInvVal.Any())
            {
                var dictCostoDiario = DiarioCosto.ConstruirDictPromedioDiario(lstCuadre);

                foreach (var objInvFinal in lstInvVal)
                {
                    if (objInvFinal.dcCostoUnit == 0 || objInvFinal.dcCostoTot == 0)
                    {
                        var objKey = objInvFinal.objLotePromProdTalKey;

                        if (dictCostoDiario.TryGetValue(objKey, out var dcCostoUnit))
                        {
                            objInvFinal.dcCostoUnit = dcCostoUnit;
                            objInvFinal.dcCostoTot = Math.Round(
                                dcCostoUnit * objInvFinal.dcLibras, 2);
                        }
                    }
                }
            }
        }
    }
}

