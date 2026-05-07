using CostManagement.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace CostManagementService.Dominio.Entidades
{
    public class ParamRectrac
    {
        public  int intCodProd { get; set; }
        public  int intLote { get; set; }
        public  int intCodTal { get; set; }
        public  decimal dcCajasRetra { get; set; }
        public  decimal dcEmbPeso { get; set; }
        public  decimal dcMedFactor { get; set; }

        public decimal dcLibrasRetra { get; set; }

        public LoteFrsKey objFrskey { get; set; }


        public ParamRectrac(string strCodProd, string strLote, decimal? strCodTal, decimal? intCajasRetra, double dbEmbPeso, double dbMedFactor)
        {
            this.intCodProd = Convert.ToInt32(strCodProd);
            this.intLote = Convert.ToInt32(strLote);
            this.intCodTal = strCodTal.HasValue ? Convert.ToInt32(strCodTal.Value) : 0;
            this.dcCajasRetra = intCajasRetra.HasValue ? intCajasRetra.Value : 0;
            this.dcEmbPeso = (decimal)dbEmbPeso;
            this.dcMedFactor = (decimal)dbMedFactor;
            this.dcLibrasRetra = this.dcCajasRetra * this.dcEmbPeso * this.dcMedFactor;
            this.objFrskey = new LoteFrsKey(intLote, intCodProd, intCodTal);
        }

        public static ConcurrentDictionary<LoteFrsKey, ConcurrentQueue<ParamRectrac>> ConstruirDictParamRectracFrs(List<ParamRectrac> lst)
        {
            if (lst == null || !lst.Any())
                return new ConcurrentDictionary<LoteFrsKey, ConcurrentQueue<ParamRectrac>>();

            // Agrupamos por clave y convertimos cada grupo en una cola segura
            var kvpItems = lst
                .OrderByDescending(x => x.dcCajasRetra)
                .GroupBy(x => x.objFrskey)
                .Select(g => KeyValuePair.Create(g.Key, new ConcurrentQueue<ParamRectrac>(g)));

            return new ConcurrentDictionary<LoteFrsKey, ConcurrentQueue<ParamRectrac>>(kvpItems);
        }


    }
}
