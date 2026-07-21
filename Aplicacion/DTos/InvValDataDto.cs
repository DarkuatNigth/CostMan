
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


        [Column("codTal")]
        //[JsonIgnore]
        public short? stTalCodigo { get; set; }


        /// <summary>
        /// Normaliza la descripción de talla del Excel para que coincida con TbTallas.
        /// Maneja:
        ///   - Fechas parseadas por Excel: "2026-05-09 00:00:00" no se normaliza (se corrige antes en ObtenerDatosProd)
        ///   - Ceros iniciales: "5-09" → "5-9"
        ///   - Errores tipográficos: "MEDIUN" → "MEDIUM"
        /// El resultado usa '-' como separador (consistente con lstCodTalla que ya hizo Replace('/','-')).
        /// </summary>
        public static string NormalizarTalla(string strNomTal)
        {
            if (string.IsNullOrWhiteSpace(strNomTal))
                return strNomTal;

            // Si es una fecha (parseada por Excel), no intentar normalizar
            // La corrección se hace antes en ObtenerDatosProd desde strCodigoTalla
            //if (DateTime.TryParse(strNomTal, out _))
            //    return strNomTal;

            // Corrección de errores tipográficos conocidos
            string strResultado = strNomTal.Trim().Replace("MEDIUN", "MEDIUM");

            string[] arrStrPartes = strResultado.Split('-');

            // Si tiene exactamente 2 partes y ambas son números, es una TALLA
            if (arrStrPartes.Length == 2 && arrStrPartes.All(strP => strP.Trim().All(char.IsDigit)))
            {
                // Limpiamos ceros a la izquierda: "09" -> "9"
                strResultado = string.Join("-", arrStrPartes.Select(strP =>
                {
                    string strLimpio = strP.Trim().TrimStart('0');
                    return string.IsNullOrEmpty(strLimpio) ? "0" : strLimpio;
                }));

                return strResultado; // Retornamos la talla ya limpia ("5-9")
            }

            return strResultado;
        }

    }
}
