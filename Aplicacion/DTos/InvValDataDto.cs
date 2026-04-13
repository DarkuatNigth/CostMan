
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class InvValDataDto
    {
        [Display(Name = "CAM")]
        [Column("CAM")]
        [JsonProperty("CAM")]
        public string strCam { get; set; }

        [Display(Name = "BOD_DESCRI")]
        [Column("BOD_DESCRI")]
        [JsonProperty("BOD_DESCRI")]
        public string strBodDescri { get; set; }

        [Display(Name = "PROD")]
        [Column("PROD")]
        [JsonProperty("PROD")]
        public string strProd { get; set; }

        [Display(Name = "PRO_DESESP")]
        [Column("PRO_DESESP")]  
        [JsonProperty("PRO_DESESP")]
        public string strProDesesp { get; set; }

        [Display(Name = "NOMTAL")]
        [Column("NOMTAL")]
        [JsonProperty("NOMTAL")]
        public string strNomTal { get; set; }

        [Display(Name = "lote")]
        [Column("lote")]
        [JsonProperty("lote")]
        public int intLote { get; set; }

        [Display(Name = "CLP_NOMCOM")]
        [Column("CLP_NOMCOM")]
        [JsonProperty("CLP_NOMCOM")]
        public string strClpNomCom { get; set; }

        [Display(Name = "fecha")]
        [Column("fecha")]
        [JsonProperty("fecha")]
        public DateTime dtFecha { get; set; } // En C# 8, DateTime es el estándar; DateOnly es de .NET 6+

        [Display(Name = "clp_grupo")]
        [Column("clp_grupo")]
        [JsonProperty("clp_grupo")]
        public string? strClpGrupo { get; set; }

        [Display(Name = "LIBRAS")]
        [Column("LIBRAS")]
        [JsonProperty("LIBRAS")]
        public double dcLibras { get; set; }

        [Display(Name = "master")]
        [Column("master")]
        [JsonProperty("master")]
        public double dcMaster { get; set; }

        [Display(Name = "clas01")]
        [Column("clas01")]
        [JsonProperty("clas01")]
        public string strClas01 { get; set; }

        [Display(Name = "pro_clas05")]
        [Column("pro_clas05")]
        [JsonProperty("pro_clas05")]
        public string strProClas05 { get; set; }

        [Display(Name = "GRUPO")]
        [Column("GRUPO")]
        [JsonProperty("GRUPO")]
        public string? strGrupo { get; set; }

        [Display(Name = "codigo-talla")]
        [Column("codigo-talla")]
        [JsonProperty("codigo-talla")] // <--- AGREGA ESTA LÍNEA
        public string strCodigoTalla { get; set; }

        [Display(Name = "COSTO")]
        [Column("COSTO")]
        [JsonProperty("COSTO")]
        public double dcCosto { get; set; }

        [Display(Name = "TOTAL")]
        [Column("TOTAL")]
        [JsonProperty("TOTAL")]
        public double dcTotal { get; set; }


        [Newtonsoft.Json.JsonIgnore]
        public short? stEmpCodigo { get; set; }


        [JsonIgnore]
        public string? strTipoLote { get; set; }


        [JsonIgnore]
        public byte? btMedCodigo { get; set; }


        [JsonIgnore]
        public string? strEmbCodigo { get; set; }


        [JsonIgnore]
        public short? stTalCodigo { get; set; }
    }
}
