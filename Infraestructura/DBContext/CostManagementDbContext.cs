using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Dominio.Entidades;
using CostManagementService.Infraestructura.EF_Core.SONG;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace CostManagement.Infraestructura.DBContext;

public partial class CostManagementDbContext : DbContext
{
    public CostManagementDbContext()
    {
    }

    public CostManagementDbContext(DbContextOptions<CostManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TbBodega> TbBodega { get; set; }

    public virtual DbSet<TbClasificadora> TbClasificadora { get; set; }

    public virtual DbSet<TbColor> TbColor { get; set; }

    public virtual DbSet<TbEmbala> TbEmbala { get; set; }

    public virtual DbSet<TbGuitra> TbGuitra { get; set; }

    public virtual DbSet<TbLiqtun> TbLiqtun { get; set; }

    public virtual DbSet<TbLiqvad> TbLiqvad { get; set; }

    public virtual DbSet<TbLiqvag> TbLiqvag { get; set; }

    public virtual DbSet<TbLitund> TbLitund { get; set; }

    public virtual DbSet<TbLitvad> TbLitvad { get; set; }

    public virtual DbSet<TbLototr> TbLototr { get; set; }

    public virtual DbSet<TbMedida> TbMedida { get; set; }

    public virtual DbSet<TbPais> TbPais { get; set; }

    public virtual DbSet<TbPlantaProcOe> TbPlantaProcOe { get; set; }

    public virtual DbSet<TbProduc> TbProduc { get; set; }

    public virtual DbSet<TbProvee> TbProvee { get; set; }

    public virtual DbSet<TbReglot> TbReglot { get; set; }

    public virtual DbSet<TbTallas> TbTallas { get; set; }

    public virtual DbSet<TbTiplot> TbTiplot { get; set; }

    public virtual DbSet<TbGrupo> TbGrupo { get; set; }

    public virtual DbSet<TbSubgrupo> TbSubgrupo { get; set; }

    public virtual DbSet<TbCietun> TbCietun { get; set; }

    public virtual DbSet<TbMarca> TbMarca { get; set; }

    public virtual DbSet<TbAlmace> TbAlmace { get; set; }

    public virtual DbSet<TbDetproces> TbDetproces { get; set; }

    public virtual DbSet<TbProces> TbProces { get; set; }

    public virtual DbSet<TbRelode> TbRelode { get; set; }

    public virtual DbSet<TbProcesosRecep> TbProcesosRecep { get; set; }

    public virtual DbSet<TbEmbalaFichaTecnica> TbEmbalaFichaTecnica { get; set; }

    public virtual DbSet<TbBoditesaldMes> TbBoditesaldMes { get; set; }

    public virtual DbSet<TbClacli> TbClacli { get; set; }

    public virtual DbSet<TbCertificadoFt> TbCertificadoFt { get; set; }

    public virtual DbSet<TbLiqvalPremiosCab> TbLiqvalPremiosCab { get; set; }

    public virtual DbSet<TbLiqvalPremiosDet> TbLiqvalPremiosDet { get; set; }

    public virtual DbSet<VwCatalogo> VwCatalogo { get; set; }

    public virtual DbSet<TbTracadAuto> TbTracadAuto { get; set; }

    public virtual DbSet<TbTracamAuto> TbTracamAuto { get; set; }

    public virtual DbSet<TbTransa> TbTransa { get; set; }

    public virtual DbSet<TbCabtrans> TbCabtrans { get; set; }

    public virtual DbSet<TbDetrans> TbDetrans { get; set; }

    public virtual DbSet<TbRelprodvar> TbRelprodvar { get; set; }

    public virtual DbSet<TbVarios> TbVarios { get; set; }

    public virtual DbSet<TblCtrltrathid> TblCtrltrathid { get; set; }

    public virtual DbSet<TblRecetahidrat> TblRecetahidrat { get; set; }

    public virtual DbSet<TbRelIngredientesTrataItem> TbRelIngredientesTrataItem { get; set; }

    public virtual DbSet<TbDetalleRetractilado> TbDetalleRetractilado { get; set; }

    public virtual DbSet<TbRetalm> TbRetalm { get; set; }

    public virtual DbSet<TbTipdec> TbTipdec { get; set; }


    public DbSet<SaldoBodegaDto> SpSaldoBodega { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseSqlServer("Name=DefaultConnection").AddInterceptors(new OpenJsonToInClauseInterceptor());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbBodega>(entity =>
        {
            entity.ToTable("tb_bodega", tb => tb.HasTrigger("TR_tb_bodega_audit"));

            entity.HasIndex(e => new { e.BodCodigo, e.BodPlanta }, "bod_codigo").HasFillFactor(90);

            entity.Property(e => e.BodBodFinal).HasComment("Se usará siempre que las bodegas sean de tipo transito, para que el sistema, al crear los secuenciales, sepa en que bodega se debe de extraer el saldo");
            entity.Property(e => e.BodCodigoPadre).HasComment("Codigo de bodega fisica o principal a la que pertenece la bodega logica.");
            entity.Property(e => e.BodDarDeBajaEtiquetas).HasComment("Indica si la bodega debe incluirse en los egresos por consummo de etiquetas");
            entity.Property(e => e.BodEsDeLiquidacion).HasComment("Indica si la bodega es utilizada para liquidacion directa.");
            entity.Property(e => e.BodEsDecongelado).HasComment("Identificar las bodegas que pasan por el proceso de descongelado para pelar o para descabezar");
            entity.Property(e => e.BodListCuarent).IsFixedLength();
            entity.Property(e => e.BodMovBalanzasAuto).IsFixedLength();
            entity.Property(e => e.BodMovBodega).IsFixedLength();
            entity.Property(e => e.BodPallet).IsFixedLength();
            entity.Property(e => e.BodPatio).IsFixedLength();
            entity.Property(e => e.BodTieneQr).HasComment("Indicador si la bodega tiene configurado codigo QR pare restriccion de lectura manual.");
            entity.Property(e => e.BodValidaRequerimiento).HasComment("Indica si la bodega valida requerimiento de producto");
            entity.Property(e => e.BodValmast).HasDefaultValue("S");
        });

        modelBuilder.Entity<TbClasificadora>(entity =>
        {
            entity.ToTable("tb_Clasificadora", tb => tb.HasTrigger("TR_tb_Clasificadora_audit"));

            entity.Property(e => e.ClaCodigo).IsFixedLength();
            entity.Property(e => e.ClaAplicaLiquidacion).HasComment("Indicador si la clasificadora debe considerarse para liquidaciones.");
            entity.Property(e => e.ClaEstado)
                .HasDefaultValue("AC")
                .IsFixedLength();
            entity.Property(e => e.ClaManchekclistcal).HasComment("Indica si se debe crear un registro de limpieza al inicio de turno.");
            entity.Property(e => e.ClaMaqCalculaEficiencia).IsFixedLength();
            entity.Property(e => e.ClaParadaAuto).HasComment("Indicador para identificar clasificadoras que generan paradas auotmaticas.");
            entity.Property(e => e.ClaVariacionMaxGramaje).HasComment("Variación de gramaje máxima permitida para unificación de lotes");
        });

        modelBuilder.Entity<TbColor>(entity =>
        {
            entity.HasKey(e => e.ColCodigo).IsClustered(false);
        });

        modelBuilder.Entity<TbEmbala>(entity =>
        {
            entity.ToTable("tb_embala", tb => tb.HasTrigger("TR_tb_embala_audit"));

            entity.HasIndex(e => e.EmbCodigo, "emb_codigo").HasFillFactor(90);

            entity.Property(e => e.EmbCodigo).IsFixedLength();
        });

        modelBuilder.Entity<TbGuitra>(entity =>
        {
            entity.ToTable("tb_guitra", tb =>
            {
                tb.HasComment("Guias de remision del Sistema de Produccion");
                tb.HasTrigger("Trg_AuditoriaGuitra");
                tb.HasTrigger("Trg_ExistMovrecepGuitra");
                tb.HasTrigger("Trg_actualizaEstado");
                tb.HasTrigger("Trg_actualizaEstado2");
                tb.HasTrigger("Trg_datosxdefault");
                tb.HasTrigger("trg_guitra_anula");
            });

            entity.HasIndex(e => e.GtrClaveAcceso, "IX_tb_guitra_1").HasFillFactor(90);

            entity.Property(e => e.GtrNumero).HasComment("Numero de guia");
            entity.Property(e => e.GtrCodpesca).HasComment("Codigo del programa de pesca. Corresponde al campo PRODUCCION.tb_ProgramaPescas.ppe_secuenc.");
            entity.Property(e => e.GtrCodpro).HasComment("Codigo del proveedor. Codgio debe existir en tabla tb_provee.");
            entity.Property(e => e.GtrConHieloTerceros).HasComment("Especifica si la guia lleva o no hielo cuando el tipo de transporte es terceros.");
            entity.Property(e => e.GtrDifsello).IsFixedLength();
            entity.Property(e => e.GtrLibrep).HasComment("\"Libras reportadas\". A la fecha no se logra validar este numero con otros reportes, y no se logra identificar como se actualiza este valor.");
            entity.Property(e => e.GtrLlesal).HasDefaultValue(-1m);
            entity.Property(e => e.GtrNrolot)
                .HasDefaultValue(0L)
                .HasComment("Numero de lote de produccion. El valor debe existir en la tabla tb_reglot.");
            entity.Property(e => e.GtrPiscin).HasComment("Numero de piscina (programada) del proveedor o finca.");
            entity.Property(e => e.GtrRecepLibras).HasComment("Libras reportadas por la finca, o libras recibidas desde el punto de vista de logistica. No son las libras recibidas desde el punto de vista de produccion.");
            entity.Property(e => e.GtrSerie).HasDefaultValue("001-001");
            entity.Property(e => e.GtrTipPis).HasDefaultValue("NOR");
            entity.Property(e => e.GtrUseapr).IsFixedLength();
            entity.Property(e => e.GtrUsecre).IsFixedLength();
            entity.Property(e => e.GtrUseeli).IsFixedLength();
            entity.Property(e => e.GtrUsemod).IsFixedLength();
        });

        modelBuilder.Entity<TbLiqtun>(entity =>
        {
            entity.HasKey(e => e.LiqNumero).IsClustered(false);

            entity.ToTable("tb_liqtun", tb =>
            {
                tb.HasTrigger("TR_tb_liqtun_UPDEstado");
                tb.HasTrigger("TR_tb_liqtun_loteEstadoCE");
                tb.HasTrigger("insert_liquidacion");
            });

            entity.Property(e => e.LiqCietun).HasDefaultValue(0);
            entity.Property(e => e.LiqCopack).IsFixedLength();
            entity.Property(e => e.LiqIsUnif).IsFixedLength();
            entity.Property(e => e.LiqLotval).HasDefaultValue(0m);
            entity.Property(e => e.LiqPppt)
                .HasDefaultValue("PT")
                .IsFixedLength();
            entity.Property(e => e.LiqTurno)
                .HasDefaultValueSql("((1))")
                .IsFixedLength();
        });

        modelBuilder.Entity<TbLiqvad>(entity =>
        {
            entity.ToTable("tb_liqvad", tb => tb.HasComment("Detalle de liquidaciones valorizadas."));

            entity.HasIndex(e => new { e.LidTipolote, e.LidNoliqu }, "IX_tb_liqvad").HasFillFactor(90);

            entity.Property(e => e.LidClase).IsFixedLength();
            entity.Property(e => e.LidEstado).IsFixedLength();
            entity.Property(e => e.LidTipolote).IsFixedLength();
        });

        modelBuilder.Entity<TbLiqvag>(entity =>
        {
            entity.ToTable("tb_liqvag", tb => tb.HasTrigger("TR_tb_liqvag_UPDEstado"));

            entity.Property(e => e.LiqPppt)
                .HasDefaultValue("PT")
                .IsFixedLength();
            entity.Property(e => e.LiqTurno)
                .HasDefaultValueSql("((1))")
                .IsFixedLength();
        });

        modelBuilder.Entity<TbLitund>(entity =>
        {
            entity.ToTable("tb_litund", tb =>
            {
                tb.HasTrigger("TR_tb_litund_loteEstadoCE");
                tb.HasTrigger("trig_tb_litund");
            });

            entity.HasIndex(e => e.LidProduc, "IX03_tb_litund").HasFillFactor(90);

            entity.Property(e => e.LidClasificadora).HasComment("Abreviatura de clasificadora (tb_Clasificadora.cla_abrev)");
            entity.Property(e => e.LidLocero)
                .HasDefaultValue("0")
                .IsFixedLength();

            entity.HasOne(d => d.LidNumeroNavigation).WithMany().HasConstraintName("FK_tb_litund_tb_liqtun");
        });

        modelBuilder.Entity<TbLitvad>(entity =>
        {
            entity.ToTable("tb_litvad", tb => tb.HasTrigger("trig_tb_litvad"));

            entity.HasIndex(e => e.LidNumero, "IX_tb_litvad").HasFillFactor(90);

            entity.HasIndex(e => e.LidProduc, "ix_litvadAsig").HasFillFactor(90);

            entity.Property(e => e.LidBroken).IsFixedLength();
            entity.Property(e => e.LidClasificadora).HasComment("Abreviatura de clasificadora (tb_Clasificadora.cla_abrev)");
            entity.Property(e => e.LidShello).IsFixedLength();

            entity.HasOne(d => d.LidNumeroNavigation).WithMany().HasConstraintName("FK_tb_litvad_tb_liqvag");
        });

        modelBuilder.Entity<TbLototr>(entity =>
        {
            entity.ToTable("tb_lototr", tb =>
            {
                tb.HasTrigger("TR_tb_lototrProcesadoCola");
                tb.HasTrigger("TR_tb_lototr_ValidaLoteUnificadoYProducto");
                tb.HasTrigger("TR_tb_lototr_extrapolar");
                tb.HasTrigger("TR_tb_lototr_validarHidratado");
                tb.HasTrigger("TR_trazaLote");
                tb.HasTrigger("trg_ValidaProceso_lototr");
                tb.HasTrigger("trg_update_tblototr");
            });

            entity.HasIndex(e => e.LotRloNumero, "IDX_LOTNUMERO").HasFillFactor(90);

            entity.HasIndex(e => e.LotTipo, "IDX_LOTtipo").HasFillFactor(90);

            entity.HasIndex(e => e.LotTipo, "idx_ForVA").HasFillFactor(90);

            entity.HasIndex(e => new { e.LotProdPed, e.LotTalPed }, "ixLotOtrProdped").HasFillFactor(90);

            entity.Property(e => e.LotTipo).IsFixedLength();
            entity.Property(e => e.LotCodbod).IsFixedLength();
            entity.Property(e => e.LotCopack)
                .HasDefaultValue("0")
                .IsFixedLength();
            entity.Property(e => e.LotFredes)
                .HasDefaultValue("FRE")
                .IsFixedLength();
            entity.Property(e => e.LotNumdoc).HasDefaultValue(0);
            entity.Property(e => e.LotPermiteLiqDifLotes).HasComment("Columna para controlar si permite liquidar diferentes lotes del lote unificado. 1 si permite 0 no permite");
            entity.Property(e => e.LotResp).HasDefaultValue("");
            entity.Property(e => e.LotTiplot).IsFixedLength();
            entity.Property(e => e.LotValagrvl).HasDefaultValue(0m);
        });

        modelBuilder.Entity<TbMedida>(entity =>
        {
            entity.Property(e => e.MedAbrev).IsFixedLength();
            entity.Property(e => e.MedAbrev4).IsFixedLength();
            entity.Property(e => e.MedAbrevIng).IsFixedLength();
        });

        modelBuilder.Entity<TbPais>(entity =>
        {
            entity.Property(e => e.PaiCodigo).ValueGeneratedOnAdd();
            entity.Property(e => e.PaiAbrev).IsFixedLength();
            entity.Property(e => e.PaiAgrupa).IsFixedLength();
            entity.Property(e => e.PaiDescri).IsFixedLength();
            entity.Property(e => e.PaiDescriIng).IsFixedLength();
            entity.Property(e => e.PaiEstado).IsFixedLength();
        });

        modelBuilder.Entity<TbPlantaProcOe>(entity =>
        {
            entity.HasKey(e => e.PaCodigo).HasName("PK__tb_plant__46CA1D7E2E44DB4D");

            entity.Property(e => e.PaEsCopaking).HasComment("Indica si la bodega es de coopacking o no");
            entity.Property(e => e.PaSecuenEmpresExport).HasComment("Codigo/secuencia de la empresa exportadora. Es el valor de la columna eex_secuen de la tabla dbo.tb_empresExport.");
        });

        modelBuilder.Entity<TbProduc>(entity =>
        {
            entity.ToTable("tb_produc", tb =>
            {
                tb.HasComment("Maestros de productos del sistema de Produccion");
                tb.HasTrigger("Insert_Tb_Produc");
                tb.HasTrigger("TR_tb_produc_audit");
                tb.HasTrigger("TR_tb_produc_pesoBrutoLbValidar");
                tb.HasTrigger("trg_AuditoriaProducto");
                tb.HasTrigger("trg_NotificaCambioCamposEnProducto");
                tb.HasTrigger("trg_ValidaProducto");
                tb.HasTrigger("trg_actuproducto");
            });

            entity.HasIndex(e => new { e.ProClas01, e.ProClas05 }, "IX01_tb_produc").HasFillFactor(90);

            entity.HasIndex(e => new { e.ProClas02, e.ProEstado, e.ProClas04 }, "ixtb_producpro_clas02pro_estadopro_clas04").HasFillFactor(90);

            entity.Property(e => e.ChkEanImp)
                .HasDefaultValue("S")
                .IsFixedLength();
            entity.Property(e => e.Chkgranel)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.ProChkcli)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.ProChkdec)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.ProChkpesovta).IsFixedLength();
            entity.Property(e => e.ProChkpro)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.ProCiaconsigcodigo).HasComment("Codigo de la planta al que corresponde el producto. El valor del campo corresponde a la columna pla_abrev de la tabla dbo.tb_planta, donde el estado es activo.");
            entity.Property(e => e.ProConsidGenerico)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.ProEmbala).IsFixedLength();
            entity.Property(e => e.ProEspeci).HasDefaultValue(1m);
            entity.Property(e => e.ProExcepEtiq).IsFixedLength();
            entity.Property(e => e.ProGeneri)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.ProGlaseo).IsFixedLength();
            entity.Property(e => e.ProIva).IsFixedLength();
            entity.Property(e => e.ProLinpro).HasDefaultValue(1m);
            entity.Property(e => e.ProLiquidado).HasDefaultValue("N");
            entity.Property(e => e.ProLoteEmpExt).IsFixedLength();
            entity.Property(e => e.ProMerca)
                .HasDefaultValue("")
                .IsFixedLength();
            entity.Property(e => e.ProMuestr)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.ProNumsolicitud).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.ProOrigen).HasDefaultValue(1m);
            entity.Property(e => e.ProPaCodigoExportadora).HasComment("Codigo de planta exportadora. Codigo debe existir en tb_plantaProc_OE. Se crea para exportaciones de Songa 2. Este valor tambien puede existir en la tabla tb_NumSecFacExport.");
            entity.Property(e => e.ProPoInterno).IsFixedLength();
            entity.Property(e => e.ProPoclte).IsFixedLength();
            entity.Property(e => e.ProPreglaseado).IsFixedLength();
            entity.Property(e => e.ProProesp).IsFixedLength();
            entity.Property(e => e.ProReqAnalisisMicrobiologia).HasComment("Indica si el producto requiere analisis de microbiologia");
            entity.Property(e => e.ProTratam).HasDefaultValue(13m);
        });

        modelBuilder.Entity<TbProvee>(entity =>
        {
            entity.ToTable("tb_provee", tb =>
            {
                tb.HasTrigger("TRG_CONTROLAINGRESO");
                tb.HasTrigger("TRG_grupoProceso");
                tb.HasTrigger("TR_tb_provee_audit");
                tb.HasTrigger("trg_AuditoriaProveedor");
            });

            entity.HasIndex(e => e.ClpEstado, "IDX_Tb_provee_01").HasFillFactor(90);

            entity.Property(e => e.CiaCodigo).IsFixedLength();
            entity.Property(e => e.CiuCodigo).IsFixedLength();
            entity.Property(e => e.ClpCampamento).IsFixedLength();
            entity.Property(e => e.ClpClaprov).HasDefaultValue("A");
            entity.Property(e => e.ClpCodpos).IsFixedLength();
            entity.Property(e => e.ClpCorreoEnvioOrganoleptico).HasComment("Lista de correos para envio de correos de organoleptico");
            entity.Property(e => e.ClpEstado).IsFixedLength();
            entity.Property(e => e.ClpGrupo).IsFixedLength();
            entity.Property(e => e.ClpGrupoProc).HasDefaultValue("TE");
            entity.Property(e => e.ClpIvapag).IsFixedLength();
            entity.Property(e => e.ClpLispre).IsFixedLength();
            entity.Property(e => e.ClpMostrarUti)
                .HasDefaultValue("S")
                .IsFixedLength();
            entity.Property(e => e.ClpPriori).IsFixedLength();
            entity.Property(e => e.ClpTerpag).IsFixedLength();
            entity.Property(e => e.ClpTipocl).IsFixedLength();
            entity.Property(e => e.ComCodigo).IsFixedLength();
            entity.Property(e => e.EstCodigo).IsFixedLength();
            entity.Property(e => e.PaiCodigo).IsFixedLength();
            entity.Property(e => e.RegCodigo).IsFixedLength();
            entity.Property(e => e.ZonCodigo).IsFixedLength();
        });

        modelBuilder.Entity<TbReglot>(entity =>
        {
            entity.ToTable("tb_reglot", tb =>
            {
                tb.HasComment("Maestro de lotes de produccion.");
                tb.HasTrigger("Log_tb_reglot");
                tb.HasTrigger("TR_tb_reglotProcesadoCola");
                tb.HasTrigger("TR_tb_reglot_EstadoCE");
                tb.HasTrigger("TR_tb_reglot_audit");
                tb.HasTrigger("TR_tb_reglot_rloNetasValidar");
                tb.HasTrigger("TR_tb_reglot_rlo_recibi_validar");
                tb.HasTrigger("TR_tb_reglot_validarCambioPiscina");
                tb.HasTrigger("TR_tb_reglot_validar_promOrganol");
                tb.HasTrigger("UPDATE_INSERT_LOTEADI");
                tb.HasTrigger("UPDATE_INSERT_LOTEADI2");
                tb.HasTrigger("UPDATE_lote");
                tb.HasTrigger("UPDATE_lote_positiva");
                tb.HasTrigger("insert_lote");
                tb.HasTrigger("insert_lote2");
                tb.HasTrigger("trg_reglotPiscinaReal");
            });

            entity.HasIndex(e => new { e.RloGuitra, e.PteNumlote }, "IX_tb_reglot").HasFillFactor(90);

            entity.Property(e => e.RloComplocal).IsFixedLength();
            entity.Property(e => e.RloEnvioXmlNaturisa).IsFixedLength();
            entity.Property(e => e.RloEnvioXmlPrecio).IsFixedLength();
            entity.Property(e => e.RloEnvioXmlProceso).IsFixedLength();
            entity.Property(e => e.RloFecha).HasComment("Fecha de creacion del lote.");
            entity.Property(e => e.RloGuitra).HasComment("Numero de guia. Actualmente un lote puede estar relacionada a varias guias, relacion que se guarda en la tabla tb_guitra.");
            entity.Property(e => e.RloOkTrazabilidad).HasDefaultValue("N");
            entity.Property(e => e.RloPermitecerrarLote).HasDefaultValue("N");
            entity.Property(e => e.RloPlantaproceso).HasDefaultValueSql("((1))");
            entity.Property(e => e.RloTermEntero).HasDefaultValue("N");
            entity.Property(e => e.RloTipolote)
                .IsFixedLength()
                .HasComment("Tipo de lote. Se encuentra definido en tb_general campo gen_tipo = TLO. Tambien se encuentra definido en vista general.vw_catalogo campo cat_codigo = TLO");
        });

        modelBuilder.Entity<TbTallas>(entity =>
        {
            entity.HasKey(e => e.TalCodigo).HasName("tal_codigo");

            entity.HasIndex(e => new { e.TalTipo, e.TalOrdvis }, "pk_ordtal")
                .IsUnique()
                .HasFillFactor(90);

            entity.Property(e => e.TalBroken)
                .HasDefaultValue("0")
                .IsFixedLength();
            entity.Property(e => e.TalFeccre).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TalOcultexp)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.TalOrdvis).HasComment("Ordenamiento en reportes");
            entity.Property(e => e.TalPermiteUsoLoteClon).HasDefaultValue(true);
        });

        modelBuilder.Entity<TbTiplot>(entity =>
        {
            entity.HasKey(e => e.TipCodigo).IsClustered(false);

            entity.Property(e => e.TipCodigo).IsFixedLength();
            entity.Property(e => e.TipEstado).IsFixedLength();
            entity.Property(e => e.TipValida).HasDefaultValue(true);
            entity.Property(e => e.TipValidaCierreLoteProduccion).HasComment("Columna para controlar si un tipo de secuencial debe o no validar el cierre de lotes produccion 1 valida, 0 o null no valida");
        });



        modelBuilder.Entity<TbCietun>(entity =>
        {
            entity.ToTable("tb_cietun", tb =>
            {
                tb.HasTrigger("Insert_CierreTunel");
                tb.HasTrigger("Update_CierreTunel");
                tb.HasTrigger("trg_anulaReqProd");
            });

            entity.HasIndex(e => new { e.CtuTunpla, e.CtuEstado }, "IX_XESTADO").HasFillFactor(90);

            entity.Property(e => e.CtuNumero).ValueGeneratedNever();
            entity.Property(e => e.CtuConsid).IsFixedLength();
            entity.Property(e => e.CtuEstProceso).IsFixedLength();
            entity.Property(e => e.CtuEstado).IsFixedLength();
            entity.Property(e => e.CtuRevetiq).IsFixedLength();
            entity.Property(e => e.CtuTiplot).IsFixedLength();
        });

        modelBuilder.Entity<TbGrupo>(entity =>
        {
            entity.Property(e => e.GrpCodigo).IsFixedLength();
            entity.Property(e => e.GrpCiarealcionada).IsFixedLength();
        });

        modelBuilder.Entity<TbSubgrupo>(entity =>
        {
            entity.HasKey(e => e.SgrCodigo).HasName("PK__tb_subgr__522B37977715C9C4");
        });


        modelBuilder.Entity<TbEmbalaFichaTecnica>(entity =>
        {
            entity.HasKey(e => new { e.EftId, e.EftItem, e.EftIdioma, e.EftTipo, e.EftGrupo }).HasName("PK_tb_EmbalaFichaTecnica_1");

            entity.ToTable("tb_EmbalaFichaTecnica", tb =>
            {
                tb.HasTrigger("tb_EmbalaFichaTecnica_Correo_Enviar");
                tb.HasTrigger("trg_AuditoriaEmbalaFicha");
            });

            entity.HasIndex(e => e.EftItem, "IX_tb_EmbalaFichaTecnica").HasFillFactor(90);

            entity.Property(e => e.EftTipo)
                .HasDefaultValue("")
                .IsFixedLength();
            entity.Property(e => e.EftGrupo)
                .HasDefaultValue("")
                .IsFixedLength();
            entity.Property(e => e.EftEquipo).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.EftFecdig).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EftRequerido)
                .HasDefaultValue("S")
                .IsFixedLength();
        });


        modelBuilder.Entity<TbMarca>(entity =>
        {
            entity.HasKey(e => e.MarCodigo).IsClustered(false);

            entity.ToTable("tb_marca", tb => tb.HasTrigger("trg_Marca"));

            entity.Property(e => e.MarClase).IsFixedLength();
            entity.Property(e => e.MarDesVta).HasDefaultValue("A");
            entity.Property(e => e.MarOrden).HasDefaultValue(100m);
        });


        modelBuilder.Entity<TbProcesosRecep>(entity =>
        {
            entity.Property(e => e.PrrCodigo).ValueGeneratedOnAdd();
            entity.Property(e => e.PrrMostrar).IsFixedLength();
        });

        modelBuilder.Entity<TbAlmace>(entity =>
        {
            entity.HasKey(e => e.AlmCodigo).IsClustered(false);
        });



        modelBuilder.Entity<TbProces>(entity =>
        {
            entity.HasKey(e => e.ProCodigo).HasName("PK_tbproces");

            entity.ToTable("tb_proces", tb => tb.HasTrigger("TR_tb_proces_audit"));

            entity.Property(e => e.ProConsidNugget).IsFixedLength();
            entity.Property(e => e.ProSubcls).IsFixedLength();
        });

        modelBuilder.Entity<TbRelode>(entity =>
        {
            entity.Property(e => e.RldCabcol).IsFixedLength();
            entity.Property(e => e.RldTipo).IsFixedLength();

            entity.HasOne(d => d.TbLototr).WithMany().HasConstraintName("FK_tb_relode_tb_lototr");
        });

        modelBuilder.Entity<TbDetproces>(entity =>
        {
            entity.HasKey(e => e.DprCodigo).HasName("PK__tb_detpr__89ED7439DC0643D6");

            entity.Property(e => e.DprCodigo).ValueGeneratedOnAdd();
            entity.Property(e => e.DprDescri2).HasDefaultValue("");
            entity.Property(e => e.DprEstado).IsFixedLength();
            entity.Property(e => e.DprSubcls).IsFixedLength();
        });

        modelBuilder.Entity<TbClacli>(entity =>
        {
            entity.Property(e => e.ClaCodigo).IsFixedLength();
            entity.Property(e => e.ClaListaBase).IsFixedLength();
            entity.Property(e => e.ClaListaPrecioEmails).HasComment("Correos de usuarios y proveedores a quienes se notificara la aprobacion de una lista de precios.");
        });

        modelBuilder.Entity<TbLiqvalPremiosCab>(entity =>
        {
            entity.ToTable("tb_liqvalPremiosCab", tb =>
            {
                tb.HasComment("Cabecera de premios de liquidaciones valorizadas.");
                tb.HasTrigger("TR_tb_liqvalPremiosCab_audit");
                tb.HasTrigger("trg_tb_liqvalPremiosCab");
            });

            entity.Property(e => e.LipEstado).HasDefaultValue(true);
            entity.Property(e => e.LipTipolote).IsFixedLength();
        });

        modelBuilder.Entity<TbLiqvalPremiosDet>(entity =>
        {
            entity.ToTable("tb_liqvalPremiosDet", tb =>
            {
                tb.HasComment("Detalle de premios de liquidaciones valorizadas.");
                tb.HasTrigger("trg_tb_liqvalPremiosDet");
            });

            entity.Property(e => e.LidClase).IsFixedLength();
            entity.Property(e => e.LidTipo).IsFixedLength();
        });


        modelBuilder.Entity<VwCatalogo>(entity =>
        {
            entity.ToView("vw_catalogo", "general");
        });


        modelBuilder.Entity<TbTracadAuto>(entity =>
        {
            entity.HasIndex(e => new { e.TcdNumero, e.TcdLote, e.TcdProduc, e.TcdCodtal }, "ix_tb_tracadauto_numero").IsClustered();
        });

        modelBuilder.Entity<TbTracamAuto>(entity =>
        {
            entity.ToTable("tb_tracamAuto", tb => tb.HasTrigger("trg_ValidatracamAuto"));

            entity.Property(e => e.TrcAutori).IsFixedLength();
        });

        modelBuilder.Entity<TbTransa>(entity =>
        {
            entity.HasKey(e => e.TrsCodigo).IsClustered(false);

            entity.ToTable("tb_transa", tb => tb.HasComment("Motivos de movimiento de inventario"));

            entity.Property(e => e.TrsCodigo).IsFixedLength();
            entity.Property(e => e.TrsDescri).IsFixedLength();
            entity.Property(e => e.TrsEstado).IsFixedLength();
            entity.Property(e => e.TrsTipo).IsFixedLength();
            entity.Property(e => e.TrsTomainv).HasDefaultValue("N");
            entity.Property(e => e.TrsTumbada).HasDefaultValue("N");
        });


        modelBuilder.Entity<TbCabtrans>(entity =>
        {
            entity.HasIndex(e => e.TraSecuencial, "iccabtransSecuencial").HasFillFactor(90);

            entity.HasIndex(e => e.TraSscc, "ixReqCabtrans").HasFillFactor(90);

            entity.HasIndex(e => e.CodDestino, "ixtb_cabtransDestino").HasFillFactor(90);

            entity.Property(e => e.CodDestino).IsFixedLength();
            entity.Property(e => e.CodOrigen).IsFixedLength();
            entity.Property(e => e.Id780).IsFixedLength();
            entity.Property(e => e.Lote).IsFixedLength();
            entity.Property(e => e.Modulo).IsFixedLength();
            entity.Property(e => e.TipProc1).UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.TipProc2).UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.TraEstadoProcPeso)
                .HasDefaultValue("AC")
                .IsFixedLength();
            entity.Property(e => e.TraSsccEstado).IsFixedLength();
            entity.Property(e => e.TroCodigo).IsFixedLength();
        });

        modelBuilder.Entity<TbDetrans>(entity =>
        {
            entity.ToTable("tb_detrans", tb => tb.HasTrigger("TR_tb_detrans_validaPesoDescabezado"));

            entity.HasIndex(e => new { e.Nsecuencial, e.Ntrans780, e.Hora, e.ProCodcor, e.TalCodigo, e.Id780 }, "clustInd_detrans")
                .IsClustered()
                .HasFillFactor(90);

            entity.Property(e => e.DetEstado).IsFixedLength();
            entity.Property(e => e.Id780).IsFixedLength();
        });

        modelBuilder.Entity<TbProces>(entity =>
        {
            entity.HasKey(e => e.ProCodigo).HasName("PK_tbproces");

            entity.ToTable("tb_proces", tb => tb.HasTrigger("TR_tb_proces_audit"));

            entity.Property(e => e.ProConsidNugget).IsFixedLength();
            entity.Property(e => e.ProSubcls).IsFixedLength();
        });

        modelBuilder.Entity<TbRelprodvar>(entity =>
        {
            entity.ToTable("tb_relprodvar", tb =>
            {
                tb.HasTrigger("TR_tb_relprodvar_audit");
                tb.HasTrigger("TR_tb_relprodvar_validarHidratado");
            });

            entity.Property(e => e.RpvCodigo).IsFixedLength();
            entity.Property(e => e.RpvIdioma).IsFixedLength();
        });

        modelBuilder.Entity<TbVarios>(entity =>
        {
            entity.Property(e => e.VarGrupo)
                .HasDefaultValue("")
                .IsFixedLength();
        });

        modelBuilder.Entity<TblCtrltrathid>(entity =>
        {
            entity.Property(e => e.CthCodigo).ValueGeneratedOnAdd();
            entity.Property(e => e.CthCrucocido).IsFixedLength();
            entity.Property(e => e.CthEstado).IsFixedLength();
            entity.Property(e => e.CthTurno).IsFixedLength();
        });

        modelBuilder.Entity<TblRecetahidrat>(entity =>
        {
            entity.Property(e => e.RecEstado).IsFixedLength();
            entity.Property(e => e.RecTipo).IsFixedLength();
            entity.Property(e => e.RecTipohid).IsFixedLength();
        });

        modelBuilder.Entity<TbDetalleRetractilado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_tb_DetalleRetract");
        });

        modelBuilder.Entity<TbRelIngredientesTrataItem>(entity =>
        {
            entity.HasKey(e => e.RtId).HasName("pk_detIngreTrata");

            entity.Property(e => e.RtId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<TbRetalm>(entity =>
        {
            entity.Property(e => e.RtaCodigo).ValueGeneratedOnAdd();
            entity.Property(e => e.RtaDescri).HasDefaultValue("");
            entity.Property(e => e.RtaEstado).IsFixedLength();
        });

        modelBuilder.Entity<TbTipdec>(entity =>
        {
            entity.Property(e => e.TidCodigo).ValueGeneratedOnAdd();
            entity.Property(e => e.TidDescri).HasDefaultValue("");
            entity.Property(e => e.TidEstado).IsFixedLength();
        });

        modelBuilder.Entity<SaldoBodegaDto>()
                    .HasNoKey()
                    .ToView(null);

        modelBuilder.Entity<LoteCertificacionDto>()
                    .HasNoKey()
                    .ToView(null);

        modelBuilder.Entity<CopackingLbs>()
                    .HasNoKey()
                    .ToView(null);

        modelBuilder.Entity<ResumenEstiloLbsDto>()
                    .HasNoKey()
                    .ToView(null);

        modelBuilder.Entity<RptCongInd>()
                    .HasNoKey()
                    .ToView(null);

        modelBuilder.Entity<LibrasProcesadasDto>().HasNoKey();

        modelBuilder.Entity<TracamAutoResult>().HasNoKey();

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
