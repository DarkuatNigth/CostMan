using CostManagement.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Dominio.Entidades
{
    public class ParamRectrac
    {
        [Column("CodProd")]
        public  int intCodProd { get; set; }

        [Column("Lote")]
        public  int intLote { get; set; }

        [Column("CodTal")]
        public  int intCodTal { get; set; }

        //[Column("CodProd")]
        //public  decimal dcCajasRetra { get; set; }

        //[Column("CodProd")]
        //public  decimal dcEmbPeso { get; set; }

        //[Column("CodProd")]
        //public  decimal dcMedFactor { get; set; }

        [Column("LbsCajasRetra")]
        public decimal dcLibrasRetra { get; set; }

        [NotMapped]
        public LoteRpcKeyXProdTal objProdTal { get; set; }

        [NotMapped]
        public LoteFrsKey objFrskey { get; set; }

        public ParamRectrac()
        {

        }
        //public ParamRectrac(string strCodProd, string strLote, decimal? strCodTal, decimal? intCajasRetra, double dbEmbPeso, double dbMedFactor)
        //{
        //    this.intCodProd = Convert.ToInt32(strCodProd);
        //    this.intLote = Convert.ToInt32(strLote);
        //    this.intCodTal = strCodTal.HasValue ? Convert.ToInt32(strCodTal.Value) : 0;
        //    this.dcCajasRetra = intCajasRetra.HasValue ? intCajasRetra.Value : 0;
        //    this.dcEmbPeso = (decimal)dbEmbPeso;
        //    this.dcMedFactor = (decimal)dbMedFactor;
        //    this.dcLibrasRetra = this.dcCajasRetra * this.dcEmbPeso * this.dcMedFactor;
        //    this.objProdTal = new LoteRpcKeyXProdTal(this.intCodProd, this.intCodTal);
        //}
        public void InitKey()
        {
            this.objProdTal = new LoteRpcKeyXProdTal(this.intCodProd, this.intCodTal);
            this.objFrskey = new LoteFrsKey(this.intLote, this.intCodProd, this.intCodTal);
        }
        public static ConcurrentDictionary<LoteFrsKey, ConcurrentQueue<ParamRectrac>> ConstruirDictParamRectracFrs(List<ParamRectrac> lst)
        {
            if (lst == null || !lst.Any())
                return new ConcurrentDictionary<LoteFrsKey, ConcurrentQueue<ParamRectrac>>();

            // Agrupamos por clave y convertimos cada grupo en una cola segura
            var kvpItems = lst
                .OrderByDescending(x => x.dcLibrasRetra)
                .GroupBy(x => x.objFrskey)
                .Select(g => KeyValuePair.Create(g.Key, new ConcurrentQueue<ParamRectrac>(g)));

            return new ConcurrentDictionary<LoteFrsKey, ConcurrentQueue<ParamRectrac>>(kvpItems);
        }


    }
}
