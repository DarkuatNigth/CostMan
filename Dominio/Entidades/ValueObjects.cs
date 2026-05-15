using System.Collections.Concurrent;

namespace CostManagement.Dominio.Entidades
{
    public record LoteRpcKey(int LotNumero, int LotUnificado);
    public record LoteRpcKeyXSec(int intLoteSecuencial, int intLoteUnificado);
    public record LoteFrsKey(int intLote, int intProdCod, int intTallaCod);
    public record LoteRpcKeyReci(int intLote, int intProdCod, int intTallaCod);
    public record LoteRpcKeyXProdTal(int ProdCod, int Codtal);
    public record LotePrecio(int intLote, int intProdCod, int intTallaCod, string strClase);
    public record PromXProdTal(string ProdCod, int Codtal);
    public record PromLoteXProdTal(int intLote, string strProdCod, int intTallaCod);
    public record LoteRpcValKey(int intSecuencialLote, int intLoteUnificado, int intCodProd, int intCodTal);
    public record ContextoCostos(
            ConcurrentDictionary<PromLoteXProdTal, decimal> DictPorLoteFrs,
            //ConcurrentDictionary<PromXProdTal, decimal> DictPromFrs,
            ConcurrentDictionary<PromLoteXProdTal, decimal> DictPorLoteRpc,
            //ConcurrentDictionary<PromXProdTal, decimal> DictPromRpc,
            ConcurrentDictionary<PromLoteXProdTal, decimal> DictPorLoteSld,
            ConcurrentDictionary<PromXProdTal, decimal> DictPromFrsRpcSld
        );
    public record CtCtblXClaseTipo(string strClase, string strTipo);
    public record NumDocXFactRef(int intNumDoc, string strFactRef);
    internal sealed class CostosUnitarios
    {
        // Proceso Primario
        public decimal dcRecepcion { get; init; }
        public decimal dcClasificacion { get; init; }
        public decimal dcCajas { get; init; }
        public decimal dcDescabezado { get; init; }
        // Proceso Presentación
        public decimal dcDecorado { get; init; }
        public decimal dcRetractilado { get; init; }
        // Proceso Congelación
        public decimal dcBrine { get; init; }
        public decimal dcIQF { get; init; }
        public decimal dcTunel { get; init; }
        // Proceso Secundario
        public decimal dcPelado { get; init; }
        public decimal dcHidratacion { get; init; }
        public decimal dcCocido { get; init; }
        // Costos estructurales
        public decimal dcCostDirectoVar { get; init; }
        public decimal dcCostDirectoFij { get; init; }
        public decimal dcCostIndirVar { get; init; }
        public decimal dcCostIndirFij { get; init; }
        public decimal dcCopacking { get; init; }
        public decimal dcExcedente { get; init; }
        public static CostosUnitarios ExtraerCostosUnitarios(
        ConcurrentDictionary<string, decimal> d) => new()
        {
            // Proceso Primario
            dcRecepcion = d.GetValueOrDefault("Recepcion"),
            dcClasificacion = d.GetValueOrDefault("Clasificacion"),
            dcCajas = d.GetValueOrDefault("Cajas"),
            dcDescabezado = d.GetValueOrDefault("Descabezado"),
            // Proceso Presentación
            dcDecorado = d.GetValueOrDefault("Decorado"),
            dcRetractilado = d.GetValueOrDefault("Retractilado"),
            // Proceso Congelación
            dcBrine = d.GetValueOrDefault("Brine"),
            dcIQF = d.GetValueOrDefault("IQF"),
            dcTunel = d.GetValueOrDefault("Tunel"),
            // Proceso Secundario
            dcPelado = d.GetValueOrDefault("Pelado"),
            dcHidratacion = d.GetValueOrDefault("Hidratacion"),
            dcCocido = d.GetValueOrDefault("Cocido"),
            // Costos estructurales
            dcCostDirectoVar = d.GetValueOrDefault("C.D.Variables"),
            dcCostDirectoFij = d.GetValueOrDefault("C.D.Fijos"),
            dcCostIndirVar = d.GetValueOrDefault("C.I.Variables"),
            dcCostIndirFij = d.GetValueOrDefault("C.I.Fijos"),
            dcCopacking = d.GetValueOrDefault("C.Copacking"),
            dcExcedente = d.GetValueOrDefault("Excedente M.E.")
        };
    }
    public record LoteRpcKeyLoteXProd(int intLoteSecuencial, string strCodProd);

    public record TipoCopacking(string Codigo, string Descripcion);
    public record LoteRpcNivelCosteo(int intLotNumero, int intNivel);

    public class ValueObjects
    {

        public string strLibrasProces { get; set; } = @"

        IF OBJECT_ID('tempdb..#tmpPelado') IS NOT NULL 
            DROP TABLE #tmpPelado;
        
        SELECT 
            lot_numero  as lotNumero,
            lot_rloNumero as rloNumero,
	        (SELECT   SUM(PESO)   
	        FROM dbo.tb_cabtrans A  
	        INNER JOIN TB_DETRANS B ON A.NSECUENCIAL = B.Nsecuencial AND A.ID_780 = B.id_780  
	        WHERE fecha>= DATEADD(DAY,-7,@feini)  and   LOTE= lot_rlonumero AND A.tra_secuencial= lot_numero     
	        AND A.Tro_codigo='15' AND B.pro_codcor<> '3473') as  Peso  
	    INTO #tmpPelado 
	    FROM dbo.tb_lototr  
	    INNER JOIN dbo.tb_tiplot ON lot_tiplot= tip_codigo  
	    left JOIN tb_produc PR ON pro_codcor= lot_prodPed  
	    LEFT JOIN TB_PROCES PP ON PP.pro_codigo= PRO_cLAS06 --AND PP.pro_pelado='S'  
	    WHERE lot_tipo='va' 
	        AND CONVERT(VARCHAR,lot_fecha,111) BETWEEN CONVERT(VARCHAR,@feini,111) and CONVERT(VARCHAR,@feifin  ,111)     
	        AND lot_brutas>0  and lot_estado <> 'AN'  
        group by lot_numero ,
        lot_rloNumero;


        delete from #tmpPelado where Peso is null;
        

        -------------------------------------------------------------------
        -- 2. CREACIÓN DE TABLA TEMPORAL PARA HIDRATACIÓN GLOBAL (#tmpHidra)
        -------------------------------------------------------------------
        IF OBJECT_ID('tempdb..#tmpHidra') IS NOT NULL 
            DROP TABLE #tmpHidra;

        SELECT 
             ctrh.cth_lote                              AS CthLote
            ,ctrh.cth_seclote                           AS CthSecLote
            -- Quitamos cth_talla de aquí
            ,COALESCE(SUM(ctrh.cth_hidlbs), 0.0)        AS CthHidlbs
            ,COALESCE(SUM(ctrh.cth_sallbs), 0.0)        AS CthSallbs
            ,MAX(rece.rec_nombre)                       AS RecNombre
            ,MAX(rece.rec_tipo)                         AS RecTipo
            ,MAX(ring.rt_codItem)                       AS RtCodItem
            ,MAX(rece.rec_porcsal)                      AS recSal
            ,MAX(rece.rec_porchid)                      AS recHid
        INTO #tmpHidra
        FROM tbl_ctrltrathid ctrh WITH(NOLOCK)
        INNER JOIN tbl_recetahidrat rece WITH(NOLOCK) 
            ON  (ctrh.cth_hidratante = rece.rec_tipohid OR (ctrh.cth_hidratante IS NULL AND rece.rec_tipohid IS NULL)) 
            AND (ctrh.cth_crucocido  = rece.rec_tipo    OR (ctrh.cth_crucocido  IS NULL AND rece.rec_tipo    IS NULL))
        INNER JOIN tb_relIngredientesTrata_Item ring WITH(NOLOCK) 
            ON  (rece.rec_codrelingr = ring.rt_codrel OR (rece.rec_codrelingr IS NULL AND ring.rt_codrel IS NULL)) 
            AND ring.rt_estado = 'AC'
        -- Agregamos el filtro de fecha usando los parámetros de cadena
        WHERE ctrh.cth_fecha BETWEEN CONVERT(VARCHAR,@feini,111) and CONVERT(VARCHAR,@feifin  ,111)   
        GROUP BY 
             ctrh.cth_lote, ctrh.cth_seclote; -- Ya no agrupamos por talla
     
        CREATE CLUSTERED INDEX IX_tmpHidra ON #tmpHidra (CthLote, CthSecLote);


        SELECT 
     RTRIM(tp.tip_codigo)                                                AS [TipCodigo]
    ,tp.tip_descri                                                       AS [tip_descri]
    ,lot.lot_tipo                                                        AS [lot_tipo]
    ,lot.lot_copack                                                      AS [LotCopack]
    ,tpc.Descripcion                                                     AS [TipoCopacking]
    ,lot.lot_numero                                                      AS [lot_numero]
    ,MAX(pro.pro_clas02)                                                 AS [Clase Prod]
    ,MAX(pro.pro_clas03)                                                 AS [Proc Prod]
    ,lot.lot_rloNumero                                                   AS [LoteUnificado]
    ,planta.pa_descri                                                    AS [PlantaProceso]
    ,CASE 
        WHEN pro.pro_clas01 = 'CC' AND pro.pro_clas05 = 'EN' THEN 'ENTERO'    
        WHEN pro.pro_clas01 = 'SC' AND pro.pro_clas05 = 'SH' THEN 'COLA'    
        WHEN pro.pro_clas01 = 'CC' AND pro.pro_clas05 = 'VA' THEN 'ENTERO VALOR AGREGADO'    
        WHEN pro.pro_clas01 = 'SC' AND pro.pro_clas05 = 'VA' THEN 'COLA VALOR AGREGADO' 
        ELSE '' 
     END                                                                 AS [TipoProducto]
    ,detpres.dpr_descri                                                  AS [Congelamiento Producto]
    ,lot.lot_recibi                                                      AS [LotRecibi]
    ,lot.lot_proces                                                      AS [lot_proces]
    ,MAX(lot.lot_valagr)                                                 AS [LotValagr]
    ,CASE
    WHEN litv.lid_produc IN ('5909','5908','5907') THEN 0.0
    ELSE 
        -- Redondeamos el resultado final a 2 decimales para evitar descuadres en C#
        ROUND(
            MAX(COALESCE(peso.Peso, 0.0)) * (
                -- NUMERADOR: Libras de este producto
                SUM(litv.lid_canenv * emb.emb_peso * med.med_factor) 
                / 
                -- DENOMINADOR: Libras totales del lote, EXCLUYENDO los productos que no llevan peso
                NULLIF(SUM(SUM(
                    CASE 
                        WHEN litv.lid_produc IN ('5909','5908','5907') THEN 0.0 
                        ELSE litv.lid_canenv * emb.emb_peso * med.med_factor 
                    END
                )) OVER(PARTITION BY lot.lot_numero, lot.lot_rloNumero), 0)
            )
        , 2)
         END AS [Peso]
    --,MAX(COALESCE(retra.LbsCajasRetra, 0.0))                            AS [LbsCajasRetra]
    ,lot.lot_fecha                                                       AS [lot_fecha]
    ,litv.lid_produc                                                     AS [rld_prodcod]
    ,pro.pro_desesp                                                      AS [DescriProduc]
    ,tal.tal_descri                                                      AS [tal_descri]
    ,litv.lid_codtal                                                     AS [LidCodtal]
    ,ROUND(COALESCE(SUM(litv.lid_canenv * emb.emb_peso * med.med_factor), 0.0), 2)  AS [Libras]
    ,MAX(COALESCE(hidra.RecNombre, ''))                                  AS [RecNombre]
    ,MAX(COALESCE(hidra.RecTipo,   ''))                                  AS [RecTipo]
    ,MAX(CAST(COALESCE(hidra.RtCodItem, 0) AS int))                                             AS [RtCodItem]
    ,MAX(COALESCE(lot.lot_observ, ''))                                                 AS [lot_observacion]
    ,CAST(MAX(COALESCE(hidra.recSal, 0.0)) AS DECIMAL(18, 4)) AS [recSal]
    ,CAST(MAX(COALESCE(hidra.recHid, 0.0)) AS DECIMAL(18, 4)) AS [recHid]

    ,CAST(ROUND(
        MAX(COALESCE(hidra.CthHidlbs, 0.0)) * (
            SUM(litv.lid_canenv * emb.emb_peso * med.med_factor) 
            / 
            NULLIF(SUM(SUM(litv.lid_canenv * emb.emb_peso * med.med_factor)) OVER(PARTITION BY lot.lot_numero, lot.lot_rloNumero), 0)
        )
    , 4) AS DECIMAL(18, 4)) AS [CthHidlbs]

    ,CAST(ROUND(
        MAX(COALESCE(hidra.CthSallbs, 0.0)) * (
            SUM(litv.lid_canenv * emb.emb_peso * med.med_factor) 
            / 
            NULLIF(SUM(SUM(litv.lid_canenv * emb.emb_peso * med.med_factor)) OVER(PARTITION BY lot.lot_numero, lot.lot_rloNumero), 0)
        )
    , 4) AS DECIMAL(18, 4)) AS [CthSallbs]
    ,CASE 
    WHEN MAX(peso.Peso) IS NOT NULL 
    THEN CAST(1 AS bit) 
    ELSE CAST(0 AS bit) 
    END                                                                 AS [blPelado]
    ,CASE  WHEN MAX(CASE 
                WHEN (lot.lot_tipo   = 'RE' AND lot.lot_tiplot = 'DE')
                  OR (lot.lot_tiplot = 'RLL' AND pro.pro_clas03 = 'PT')
                THEN 1 ELSE 0 
             END) = 1 
    THEN CAST(1 AS bit) ELSE CAST(0 AS bit) 
     END                                                                 AS [blDecorado]
    /*,CASE 
        WHEN MAX(COALESCE(retra.LbsCajasRetra, 0.0)) > 0 
        THEN CAST(1 AS bit) ELSE CAST(0 AS bit) 
     END                                                                 AS [Retractilado]*/
    ,ROUND(COALESCE(SUM(litv.lid_canenv / emb.emb_cantid), 0.0), 2)     AS [CantCajas]
    ,MAX(bod.bod_codigo)                                                 AS [BodCodigo]
    ,MAX(emb.emb_codigo)                                                 AS [EmbCodigo]
    ,MAX(CAST(med.med_codigo AS int))                                    AS [MedCodigo]
    ,CAST(MAX(CAST(bod.bod_esBrine AS tinyint)) AS bit)                                 AS [BodEsBrine]
    ,MAX(cast(pro.pro_decora as int))                                                 AS [TidCodigo]
    ,MAX(cast(pro.pro_retrac as int))                                                 AS [RtaCodigo]
    ,CASE 
        WHEN MAX(CASE WHEN pro.pro_clas01 = 'SC' AND pro.pro_clas05 <> 'VA' THEN 1 ELSE 0 END) = 1 
        THEN CAST(1 AS bit) ELSE CAST(0 AS bit) 
     END                                                                 AS [blDescabezado]
    ,MAX(pro.pro_congela)                                                AS [ProCongela]
    ,MAX(COALESCE(prem.CertificadosConcat, '')) AS [Certificado]
    ,MAX(prem.TotalPremio) AS [LidPremio]

FROM tb_lototr lot WITH(NOLOCK)    
INNER JOIN tb_liqvag liq WITH(NOLOCK) 
    ON lot.lot_numero = liq.liq_lote AND lot.lot_tipo = liq.liq_tipo   
INNER JOIN (
        SELECT 0 AS codigo, N'NO COPACKING'         AS Descripcion
        UNION ALL SELECT 1, N'COPACKING EN SONGA'
        UNION ALL SELECT 2, N'COPACKING EN OTRAS CIAS.'
    ) AS tpc ON tpc.codigo = CAST(lot.lot_copack AS int)
INNER JOIN tb_litvad litv WITH(NOLOCK) 
    ON liq.liq_numero = litv.lid_numero    
INNER JOIN tb_tiplot tp WITH(NOLOCK) 
    ON lot.lot_tiplot = tp.tip_codigo     
INNER JOIN tb_produc pro WITH(NOLOCK) 
    ON litv.lid_produc = pro.pro_codcor    
INNER JOIN tb_embala emb WITH(NOLOCK) 
    ON pro.pro_embala = emb.emb_codigo    
INNER JOIN tb_medida med WITH(NOLOCK) 
    ON pro.pro_unimed = med.med_codigo    
INNER JOIN tb_tallas tal WITH(NOLOCK) 
    ON CAST(litv.lid_codtal AS numeric(18,0)) = tal.tal_codigo    
INNER JOIN tb_bodega bod WITH(NOLOCK) 
    ON liq.liq_tunpla = bod.bod_codigo      
INNER JOIN tb_plantaproc_oe planta WITH(NOLOCK) 
    ON bod.bod_plantaProcesoOE = planta.pa_codigo  
INNER JOIN dbo.tb_proces pres WITH(NOLOCK)
    ON pro.pro_clas06 = pres.pro_codigo    
INNER JOIN dbo.tb_detproces detpres WITH(NOLOCK)
    ON pres.pro_congel = detpres.dpr_codigo AND detpres.dpr_GruDetProces = 1    

-- ── HIDRATACIÓN ──
/*LEFT JOIN (
    SELECT 
         ctrh.cth_lote                              AS CthLote
        ,COALESCE(SUM(ctrh.cth_hidlbs), 0.0)       AS CthHidlbs
        ,COALESCE(SUM(ctrh.cth_sallbs), 0.0)       AS CthSallbs
        ,rece.rec_nombre                            AS RecNombre
        ,rece.rec_tipo                              AS RecTipo
        ,ring.rt_codItem                            AS RtCodItem
        ,ctrh.cth_seclote                           AS CthSecLote
        ,ctrh.cth_talla                             AS CthTalla
        ,rece.rec_porcsal                           AS recSal
        ,rece.rec_porchid                           AS recHid
    FROM tbl_ctrltrathid ctrh
    INNER JOIN tbl_recetahidrat rece 
        ON  (ctrh.cth_hidratante = rece.rec_tipohid OR (ctrh.cth_hidratante IS NULL AND rece.rec_tipohid IS NULL)) 
        AND (ctrh.cth_crucocido  = rece.rec_tipo    OR (ctrh.cth_crucocido  IS NULL AND rece.rec_tipo    IS NULL))
    INNER JOIN tb_relIngredientesTrata_Item ring 
        ON  (rece.rec_codrelingr = ring.rt_codrel OR (rece.rec_codrelingr IS NULL AND ring.rt_codrel IS NULL)) 
        AND ring.rt_estado = 'AC'
    GROUP BY 
         ctrh.cth_lote, ctrh.cth_seclote, ctrh.cth_talla
        ,rece.rec_nombre, rece.rec_tipo, ring.rt_codItem
        ,rece.rec_porchid, rece.rec_porcsal
) AS hidra 
    ON  (lot.lot_rloNumero = CAST(hidra.CthLote AS numeric(18,0)) OR (lot.lot_rloNumero IS NULL AND hidra.CthLote IS NULL)) 
    AND CONVERT(varchar(100), lot.lot_numero) = hidra.CthSecLote 
    AND CONVERT(varchar(11),  litv.lid_codtal) = hidra.CthTalla*/

-- ── HIDRATACIÓN ──
    LEFT JOIN #tmpHidra hidra 
        ON  (lot.lot_rloNumero = CAST(hidra.CthLote AS numeric(18,0)) OR (lot.lot_rloNumero IS NULL AND hidra.CthLote IS NULL)) 
        AND CONVERT(varchar(100), lot.lot_numero) = hidra.CthSecLote

-- ── PESO PELADO ──
LEFT JOIN #tmpPelado peso 
    ON lot.lot_rloNumero = peso.rloNumero  
   AND lot.lot_numero = peso.lotNumero 

-- ── RETRACTILADO ──
/*LEFT JOIN (
    SELECT 
         dret.cod_prod                              AS CodProd
        ,dret.lote                                  AS Lote
        ,dret.cod_tal                               AS CodTal
        ,COALESCE(SUM(CAST(
            (CAST(COALESCE(dret.cajas_retra, 0.0) AS float) * emb2.emb_peso) * med2.med_factor 
         AS decimal(18,2))), 0.0)                   AS LbsCajasRetra
    FROM tb_DetalleRetractilado dret
    INNER JOIN tb_produc pro2 ON dret.cod_prod    = pro2.pro_codcor
    INNER JOIN tb_medida med2 ON pro2.pro_unimed   = med2.med_codigo
    INNER JOIN tb_embala emb2 ON pro2.pro_embala   = emb2.emb_codigo
    WHERE dret.cajas > 0.0
    GROUP BY dret.cod_prod, dret.lote, dret.cod_tal
) AS retra 
    ON  (CONVERT(varchar(100), lot.lot_rloNumero) = retra.Lote OR (lot.lot_rloNumero IS NULL AND retra.Lote IS NULL)) 
    AND litv.lid_produc = retra.CodProd 
    AND litv.lid_codtal = CAST(retra.CodTal AS int)*/

-- ── PREMIOS (Suma de precios y concatenación de descripciones) ──
LEFT JOIN (
    SELECT 
         pcab.lip_noliqu
        ,pdet.lid_producto
        ,pdet.lid_talla
        ,SUM(pdet.lid_precio) AS TotalPremio
        -- STRING_AGG concatena los textos separados por coma y espacio
        ,MAX(CAST(cat.det_descripcion AS NVARCHAR(MAX))) AS CertificadosConcat
    FROM tb_liqvalPremiosCab pcab WITH(NOLOCK)
    INNER JOIN tb_liqvalPremiosDet pdet WITH(NOLOCK)
        ON pcab.lip_id     = pdet.lid_cabId 
       AND pcab.lip_noliqu = pdet.lid_noliqu
    INNER JOIN general.vw_catalogo cat WITH(NOLOCK)
        ON cat.cat_codigo = 'PREMIO' 
       AND cat.det_codigo = CONVERT(varchar(11), pcab.lip_tipoPremio)
    GROUP BY 
         pcab.lip_noliqu
        ,pdet.lid_producto
        ,pdet.lid_talla
) AS prem
    ON  litv.lid_lote   = CAST(prem.lip_noliqu AS bigint)
    AND litv.lid_codtal = prem.lid_talla
    AND litv.lid_produc = prem.lid_producto

WHERE lot.lot_fecha BETWEEN @feini  
                        AND @feifin
  AND lot.lot_estado <> 'AN'     
  AND liq.liq_estado = 'AC'

GROUP BY 
     tp.tip_codigo, tp.tip_descri, lot.lot_tipo, lot.lot_copack
    ,tpc.Descripcion, lot.lot_numero, lot.lot_rloNumero
    ,lot.lot_recibi, lot.lot_proces, lot.lot_fecha
    ,litv.lid_produc, litv.lid_codtal
    ,pro.pro_desesp, tal.tal_descri, planta.pa_descri, detpres.dpr_descri
    ,CASE 
        WHEN pro.pro_clas01 = 'CC' AND pro.pro_clas05 = 'EN' THEN 'ENTERO'    
        WHEN pro.pro_clas01 = 'SC' AND pro.pro_clas05 = 'SH' THEN 'COLA'    
        WHEN pro.pro_clas01 = 'CC' AND pro.pro_clas05 = 'VA' THEN 'ENTERO VALOR AGREGADO'    
        WHEN pro.pro_clas01 = 'SC' AND pro.pro_clas05 = 'VA' THEN 'COLA VALOR AGREGADO' 
        ELSE '' 
     END

";

        public string strRepTracamAuto { get; set; } = @"

set dateformat ymd;
WITH tmp1 AS (
    SELECT trc_numsec AS Numero, concat( rtrim(emb_serie), '-', rtrim(emb_nofactbce )) AS mov_numdoc
    FROM tb_tracamAuto WITH(NOLOCK)
    INNER JOIN  tb_progembarque on emb_factura = concat(trc_embfactura,'/', year(trc_fecha)) and emb_estado <> 'N'
    WHERE trc_fecha BETWEEN @feini AND @fefin  and trc_tipo in ( 'EX', 'VA', 'CNE') and trc_estado = 'AC'
),
tmp_pesoPag AS (

    select 
    distinct lid_noliqu lote, lid_codigo codprod, lid_codtal codtal, lid_pesopag pesopag
    FROM tb_reglot rlo WITH (NOLOCK)  
    INNER JOIN tb_liqval lit WITH (NOLOCK)  ON (liv_tipolote = rlo_tipolote AND liv_noliqu = rlo_numero)  
    INNER JOIN tb_liqvad liq WITH (NOLOCK)  ON (lid_tipolote = liv_tipolote AND lid_noliqu = liv_noliqu)
    where rlo.rlo_fecha BETWEEN @feini AND @fefin
    UNION
    SELECT DISTINCT lid_lote lote, lid_produc codprod, lid_codtal codtal, lid_pesopag pesopag
    FROM TB_LOTOTR lot WITH (NOLOCK)
    inner join tb_liqvag liq ON (liq.liq_lote = lot.Lot_numero and liq.liq_tipo = lot.lot_tipo) 
    inner join tb_litvad lit WITH (NOLOCK) ON (lid_numero = liq_numero)
    where lot.lot_fecha BETWEEN @feini AND @fefin
)
SELECT  
         CAST(trc_numsec       AS bigint)  AS trc_numsec        -- numeric(18,0) → long
    ,CAST(trc_numtra       AS bigint)  AS trc_numtra        -- numeric(18,0) → long
    ,trc_fecha                                              -- datetime      → DateTime
    ,trc_ingegr                                             -- varchar(1)    → string
    ,trc_tipo                                               -- varchar(6)    → string
    ,CAST(trc_codpla       AS bigint)  AS trc_codpla        -- numeric(18,0) → long
    ,trc_codcam                                             -- varchar(2)    → string
    ,CAST(trc_plades       AS bigint)  AS trc_plades        -- numeric(18,0) → long?
    ,trc_camdes                                             -- varchar(2)    → string?
    ,trc_compro                                             -- varchar(10)   → string?
    ,trc_estado                                             -- varchar(2)    → string
    ,tcd_lote                                               -- bigint        → 
    ,tcd_produc                                             -- varchar(30)   → string
    ,CAST(tcd_codtal       AS bigint)  AS tcd_codtal        -- numeric(18,0) → long
    ,tcd_cantid                                             -- numeric(12,2) → decimal
    ,pro_desesp                                             -- varchar(100)  → string
    ,pro_clas01                                             -- varchar(3)    → string
    ,pro_proesp                                             -- char(1)       → string?
    ,tal_descri                                             -- varchar(15)   → string?
    ,tal_ordvis                                             -- float         → double
    ,trs_descri                                             -- char(30)      → string
    ,B1.bod_descri                     AS bod_ori           -- varchar(50)   → string?
    ,B2.bod_descri                     AS bod_des           -- varchar(50)   → string?
    ,P1.pla_nombre                     AS pla_ori           -- varchar(20)   → string?
    ,P2.pla_nombre                     AS pla_des           -- varchar(20)   → string?
    ,(tcd_cantid * ISNULL(pesopag, EMB_PESO) * med_factor) AS libras        -- float → double?
    ,pro_clas02                                             -- varchar(3)    → string
    ,pro_clas05                                             -- varchar(3)    → string
    ,(tcd_cantid / emb_cantid)         AS master            -- float         → double?
    ,tcd_cubori + tcd_secori           AS Ubicacion         -- varchar(5)    → string?
    ,trc_embfactura                                         -- varchar(15)   → string?
    ,trc_observ                                             -- varchar(255)  → string?
    ,trc_resp                                               -- varchar(50)   → string?
    ,CAST(COALESCE(trc_numpallet, 0) AS int) AS pallets     -- numeric(2,0)  → int?
    ,((tcd_cantid * emb_peso) / med_kilo) AS kilos          -- float         → double?
    ,'LIBRAS'                          AS UniMedInv         -- varchar(6)    → string
    ,trc_serieFactLocal                                     -- varchar(7)    → string?
    ,CAST(trc_numerFactLocal AS bigint) AS trc_numerFactLocal -- numeric(18,0) → long?
    ,CAST(mov_numdoc AS VARCHAR(MAX))   AS  mov_numdoc
FROM tb_tracamAuto WITH(NOLOCK)
  INNER JOIN tmp1               ON trc_numsec = Numero
  INNER JOIN tb_tracadAuto  WITH(NOLOCK) ON tcd_numero   = trc_numsec
  INNER JOIN tb_produc      WITH(NOLOCK) ON pro_codcor   = tcd_produc
  INNER JOIN tb_tallas      WITH(NOLOCK) ON tal_codigo   = tcd_codtal
  INNER JOIN tb_embala      WITH(NOLOCK) ON emb_codigo   = pro_embala
  INNER JOIN tb_medida      WITH(NOLOCK) ON med_codigo   = pro_unimed
  INNER JOIN tb_transa      WITH(NOLOCK) ON (trs_codigo  = trc_tipo AND trs_tipo = trc_ingegr)
  LEFT OUTER JOIN tmp_pesoPag   ON lote = tcd_lote AND codprod = tcd_produc AND codtal = tcd_codtal
  LEFT OUTER JOIN tb_planta P1  WITH(NOLOCK) ON P1.pla_codigo = trc_codpla
  LEFT OUTER JOIN tb_bodega B1  WITH(NOLOCK) ON B1.bod_codigo = trc_codcam
  LEFT OUTER JOIN tb_planta P2  WITH(NOLOCK) ON P2.pla_codigo = trc_plades
  LEFT OUTER JOIN tb_bodega B2  WITH(NOLOCK) ON B2.bod_codigo = trc_camdes
  LEFT OUTER JOIN tb_PARAM      WITH(NOLOCK) ON PAR_COD = 'UMI'
";
    }
}
