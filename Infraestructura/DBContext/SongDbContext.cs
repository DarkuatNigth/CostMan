using CostManagement.Aplicación.DTos;
using CostManagement.Infraestructura.EF_Core.SONG;
using CostManagement.Infraestructura.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace CostManagement.Infraestructura.DBContext;

public partial class SongDbContext : DbContext
{
    public SongDbContext()
    {
    }

    public SongDbContext(DbContextOptions<SongDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TbCabmov2> TbCabmov2 { get; set; }

    public virtual DbSet<TbDetmov2> TbDetmov2 { get; set; }

    public virtual DbSet<TbCabreq> TbCabreq { get; set; }

    public virtual DbSet<TbBodite> TbBodite { get; set; }

    public virtual DbSet<TbBodega> TbBodega { get; set; }

    public virtual DbSet<TbCabmov1> TbCabmov1 { get; set; }

    public virtual DbSet<TbDetmov1> TbDetmov1 { get; set; }

    public virtual DbSet<TbGrupo> TbGrupo { get; set; }

    public virtual DbSet<TbItem> TbItem { get; set; }

    public virtual DbSet<TbLinea> TbLinea { get; set; }

    public virtual DbSet<TbCobpag1> TbCobpag1 { get; set; }

    public virtual DbSet<TbCobpag1Dxl> TbCobpag1Dxl { get; set; }

    public virtual DbSet<TbDatfac> TbDatfac { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbCabmov2>(entity =>
        {
            entity.HasKey(e => e.MovNummov).IsClustered(false);

            entity.ToTable("tb_cabmov2", tb =>
            {
                tb.HasTrigger("Insert2_Dcto_Tb_Cabmov2");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov2");
                tb.HasTrigger("ValidaInventarioCerrado2");
            });

            entity.HasIndex(e => e.MovTipo, "IX_CABMOV2_MOVTIPO").HasFillFactor(90);

            entity.HasIndex(e => new { e.MovTipo, e.MovEstado }, "IX_tb_cabmov2_tipo_estado").HasFillFactor(100);

            entity.Property(e => e.MovNummov).IsFixedLength();
            entity.Property(e => e.ModElidat).IsFixedLength();
            entity.Property(e => e.ModEliuse).IsFixedLength();
            entity.Property(e => e.MovBoddes).IsFixedLength();
            entity.Property(e => e.MovBodori).IsFixedLength();
            entity.Property(e => e.MovCentro).IsFixedLength();
            entity.Property(e => e.MovCierre).IsFixedLength();
            entity.Property(e => e.MovCodcia).IsFixedLength();
            entity.Property(e => e.MovConfir).IsFixedLength();
            entity.Property(e => e.MovCredat).IsFixedLength();
            entity.Property(e => e.MovCreuse).IsFixedLength();
            entity.Property(e => e.MovDescue).IsFixedLength();
            entity.Property(e => e.MovEstado).IsFixedLength();
            entity.Property(e => e.MovFecdig).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.MovFecha).IsFixedLength();
            entity.Property(e => e.MovFhasta).IsFixedLength();
            entity.Property(e => e.MovForpag).IsFixedLength();
            entity.Property(e => e.MovLinea).IsFixedLength();
            entity.Property(e => e.MovLocdes).IsFixedLength();
            entity.Property(e => e.MovLocori).IsFixedLength();
            entity.Property(e => e.MovModdat).IsFixedLength();
            entity.Property(e => e.MovModuse).IsFixedLength();
            entity.Property(e => e.MovMuestra).HasDefaultValue(false);
            entity.Property(e => e.MovNmovfa)
                .HasDefaultValue("0")
                .IsFixedLength();
            entity.Property(e => e.MovNumdoc).IsFixedLength();
            entity.Property(e => e.MovNumord).IsFixedLength();
            entity.Property(e => e.MovOrdpro).IsFixedLength();
            entity.Property(e => e.MovProvee).IsFixedLength();
            entity.Property(e => e.MovRefere).IsFixedLength();
            entity.Property(e => e.MovRespon).IsFixedLength();
            entity.Property(e => e.MovRuccli).IsFixedLength();
            entity.Property(e => e.MovSubcen).IsFixedLength();
            entity.Property(e => e.MovTipcom).IsFixedLength();
            entity.Property(e => e.MovTipegr).IsFixedLength();
            entity.Property(e => e.MovTipo).IsFixedLength();
        });

        modelBuilder.Entity<TbDetmov2>(entity =>
        {
            entity.ToTable("tb_detmov2", tb =>
            {
                tb.HasTrigger("Insert_Tb_Detmov2");
                tb.HasTrigger("Insert_Tb_Detmov2_2");
                tb.HasTrigger("TR_tb_detmov2_det_preuni_Validar");
            });

            entity.HasIndex(e => e.DetCodart, "IX_tbDetmov2_detCodart").HasFillFactor(100);

            entity.HasIndex(e => new { e.DetCodart, e.DetBodega }, "IX_tb_detmov2").HasFillFactor(90);

            entity.Property(e => e.DetBodega).IsFixedLength();
            entity.Property(e => e.DetCabece).IsFixedLength();
            entity.Property(e => e.DetCentro).IsFixedLength();
            entity.Property(e => e.DetCodart).IsFixedLength();
            entity.Property(e => e.DetCodcia).IsFixedLength();
            entity.Property(e => e.DetCodcubbori).HasDefaultValue("A");
            entity.Property(e => e.DetCodparbori).HasDefaultValueSql("((1))");
            entity.Property(e => e.DetEstado).IsFixedLength();
            entity.Property(e => e.DetIva).IsFixedLength();
            entity.Property(e => e.DetNomart).IsFixedLength();
            entity.Property(e => e.DetNotfac).IsFixedLength();
            entity.Property(e => e.DetNumero).IsFixedLength();
            entity.Property(e => e.DetOrdpro).IsFixedLength();
            entity.Property(e => e.DetSubcen).IsFixedLength();
            entity.Property(e => e.DetTipineg).IsFixedLength();
            entity.Property(e => e.DetTipo).IsFixedLength();
            entity.Property(e => e.DetUnimed).IsFixedLength();
        });


        modelBuilder.Entity<TbCabreq>(entity =>
        {
            entity.HasKey(e => new { e.ReqCodcia, e.ReqNumreq, e.ReqTipo }).IsClustered(false);

            entity.ToTable("tb_cabreq", tb =>
            {
                tb.HasComment("Cabecera de requisiciones de inventario");
                tb.HasTrigger("ActualizaPresup_Tb_Cabreq");
                tb.HasTrigger("ValidaReqAut_Tb_Cabreq");
                tb.HasTrigger("trg_Cabreq1");
                tb.HasTrigger("trg_Cabreq2");
                tb.HasTrigger("trg_Cabreq3");
                tb.HasTrigger("trg_Cabreq4");
            });

            entity.HasIndex(e => e.ReqEstado, "IX_tb_cabreq_1").HasFillFactor(90);

            entity.Property(e => e.ReqCodcia).IsFixedLength();
            entity.Property(e => e.ReqTipo)
                .IsFixedLength()
                .HasComment("Tipo de requisicion, siempre es RQ.");
            entity.Property(e => e.Credat).IsFixedLength();
            entity.Property(e => e.Creuse).IsFixedLength();
            entity.Property(e => e.Elidat).IsFixedLength();
            entity.Property(e => e.Elihor).IsFixedLength();
            entity.Property(e => e.Eliuse).IsFixedLength();
            entity.Property(e => e.Moddat).IsFixedLength();
            entity.Property(e => e.Modhora).IsFixedLength();
            entity.Property(e => e.Moduse).IsFixedLength();
            entity.Property(e => e.ReqAprob).IsFixedLength();
            entity.Property(e => e.ReqBodega).IsFixedLength();
            entity.Property(e => e.ReqCentro).IsFixedLength();
            entity.Property(e => e.ReqCompcont).IsFixedLength();
            entity.Property(e => e.ReqConfir).IsFixedLength();
            entity.Property(e => e.ReqEstado).IsFixedLength();
            entity.Property(e => e.ReqFecha).IsFixedLength();
            entity.Property(e => e.ReqFechap).IsFixedLength();
            entity.Property(e => e.ReqFlag)
                .IsFixedLength()
                .HasComment("Toma el valor A cuando el registro representa una requisicion de egreso automatico. Toma el valor vacio cuando el registro representa una requisicion de egreso general.");
            entity.Property(e => e.ReqHora).IsFixedLength();
            entity.Property(e => e.ReqHorapr).IsFixedLength();
            entity.Property(e => e.ReqMuestra).HasDefaultValue(false);
            entity.Property(e => e.ReqNomsol).IsFixedLength();
            entity.Property(e => e.ReqOrdpro).IsFixedLength();
            entity.Property(e => e.ReqSolici).IsFixedLength();
            entity.Property(e => e.ReqSubcen).IsFixedLength();
            entity.Property(e => e.ReqSucurs).IsFixedLength();
            entity.Property(e => e.ReqTipReq).HasComment("Subtipo o 2do tipo de requisicion. ST = requisicion de compra. EG = requisicion de egreso.");
            entity.Property(e => e.ReqTipcot).IsFixedLength();
            entity.Property(e => e.ReqTipegr).IsFixedLength();
            entity.Property(e => e.ReqUsuapr).IsFixedLength();
        });


        modelBuilder.Entity<TbBodite>(entity =>
        {
            entity.HasKey(e => new { e.BodCodigo, e.SucCodigo, e.CiaCodigo, e.IteCodigo, e.BodLote, e.BodCodcub, e.BodCodpar }).HasName("PK__tb_bodit__EAAE631273BBBED3");

            entity.ToTable("tb_bodite", tb =>
            {
                tb.HasTrigger("Insert_Tb_Bodite_ValNeg");
                tb.HasTrigger("TR_tb_bodite_audit");
            });

            entity.HasIndex(e => e.IteCodigo, "IX_tb_bodite_iteCodigo").HasFillFactor(100);

            entity.HasIndex(e => new { e.BodCodigo, e.IteCodigo, e.BodLote }, "ixBoditeItem").HasFillFactor(100);

            entity.Property(e => e.BodCodigo).IsFixedLength();
            entity.Property(e => e.SucCodigo).IsFixedLength();
            entity.Property(e => e.CiaCodigo).IsFixedLength();
            entity.Property(e => e.IteCodigo).IsFixedLength();
            entity.Property(e => e.BodCanmax).HasDefaultValue(0.0);
            entity.Property(e => e.BodCanmin).HasDefaultValue(0.0);
            entity.Property(e => e.BodCanreo).HasDefaultValue(0.0);
            entity.Property(e => e.BodCospro).HasDefaultValue(0m);
            entity.Property(e => e.BodCredat)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.BodCreuse)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.BodElidat)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.BodEliuse)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.BodLoteFecCad).IsFixedLength();
            entity.Property(e => e.BodLoteFechaL).IsFixedLength();
            entity.Property(e => e.BodModdat)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.BodModuse)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.BodProrot).HasDefaultValue(0.0);
            entity.Property(e => e.BodStatus)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.BodStock).HasDefaultValue(0.0);
            entity.Property(e => e.BodTireor).HasDefaultValue(0.0);
            entity.Property(e => e.BodTotal).HasDefaultValue(0m);
            entity.Property(e => e.BodUnidad).HasDefaultValue(0.0);
            entity.Property(e => e.IteDescri).HasDefaultValue(" ");
        });



        modelBuilder.Entity<TbCabmov1>(entity =>
        {
            entity.ToTable("tb_cabmov1", tb =>
            {
                tb.HasTrigger("ActualizaPresup_Tb_Cabmov1");
                tb.HasTrigger("AprobacionOC_Tb_Cabmov1");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_10");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_11");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_2");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_3");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_4");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_5");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_6");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_7");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_8");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov1_9");
                tb.HasTrigger("Insert_Dcto_Tb_Cabmov_5");
                tb.HasTrigger("TR_tb_cabmov1_subtot_isNull");
            });

            entity.HasIndex(e => e.MovAresol, "IX_tb_cabmov1").HasFillFactor(90);

            entity.HasIndex(e => e.MovClaveAcceso, "IX_tb_cabmov1_5").HasFillFactor(90);

            entity.HasIndex(e => new { e.MovOrdpro, e.MovTipo, e.MovEstado, e.MovPorcot }, "IX_tb_cabmov1_6").HasFillFactor(90);

            entity.HasIndex(e => new { e.MovCodcia, e.MovTipo, e.MovEstado }, "idx_tb_cabmov2").HasFillFactor(90);

            entity.Property(e => e.MovCodcia).IsFixedLength();
            entity.Property(e => e.MovNummov).IsFixedLength();
            entity.Property(e => e.MovTipo).IsFixedLength();
            entity.Property(e => e.ModElidat)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.ModEliuse)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovAutimp)
                .HasDefaultValueSql("((0))")
                .IsFixedLength();
            entity.Property(e => e.MovAutsri).HasDefaultValueSql("((0))");
            entity.Property(e => e.MovBoddes)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovBodori)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovCargadic).IsFixedLength();
            entity.Property(e => e.MovCentro)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovCierre)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovClascli).IsFixedLength();
            entity.Property(e => e.MovComent).HasDefaultValue(" ");
            entity.Property(e => e.MovCompra)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovConcep).HasDefaultValue(" ");
            entity.Property(e => e.MovConfir)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovConsdirec).IsFixedLength();
            entity.Property(e => e.MovContrato).HasDefaultValue(false);
            entity.Property(e => e.MovCosven).HasDefaultValue(0.0);
            entity.Property(e => e.MovCredat)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovCreuse)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovDescue)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovDespac)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.MovDirpro).HasDefaultValue(" ");
            entity.Property(e => e.MovEstado)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovExporta)
                .HasDefaultValue("N")
                .IsFixedLength()
                .HasComment("VENTANA QUE GRABO REGISTRO");
            entity.Property(e => e.MovFactura).IsFixedLength();
            entity.Property(e => e.MovFecbl).IsFixedLength();
            entity.Property(e => e.MovFecdig).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.MovFecdoc).IsFixedLength();
            entity.Property(e => e.MovFecent).IsFixedLength();
            entity.Property(e => e.MovFecha)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovFhasta)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovFormaPago).HasDefaultValue(13m);
            entity.Property(e => e.MovForpag)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovGastos).HasDefaultValue(0.0);
            entity.Property(e => e.MovIntere).HasDefaultValue(0.0);
            entity.Property(e => e.MovLinea)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovLocdes)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovLocori)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovModdat)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovModuse)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovNdnc)
                .HasDefaultValueSql("((0))")
                .IsFixedLength();
            entity.Property(e => e.MovNomadi1)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovNomadi2)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovNumdoc)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovNumord)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovOrdpro)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasComment("Numero de cotizacion si el documento es o/c bienes");
            entity.Property(e => e.MovPordes).HasDefaultValue(0.0);
            entity.Property(e => e.MovPoriva).HasDefaultValue(0.0);
            entity.Property(e => e.MovProvee)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovRefere)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovRespon)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovRuccli)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovSerie)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovSerie2).IsFixedLength();
            entity.Property(e => e.MovSolici)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovSubcen)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovSubtot).HasDefaultValue(0.0);
            entity.Property(e => e.MovTipadi1)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovTipadi2)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovTipcom)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovTipegr)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.MovTipocom)
                .HasDefaultValue("COM")
                .IsFixedLength();
            entity.Property(e => e.MovTipotran)
                .IsFixedLength()
                .HasComment("TIPO DE TRANSACCION");
            entity.Property(e => e.MovTotal).HasDefaultValue(0.0);
            entity.Property(e => e.MovValdes).HasDefaultValue(0.0);
            entity.Property(e => e.MovValiva).HasDefaultValue(0.0);
            entity.Property(e => e.MovViaent)
                .HasDefaultValue(" ")
                .IsFixedLength()
                .HasComment("Tipo de compra bienes (1), servicio (2), activo fijo (3)");
            entity.Property(e => e.MovVistobueno).IsFixedLength();
        });

        modelBuilder.Entity<TbDetmov1>(entity =>
        {
            entity.ToTable("tb_detmov1", tb =>
            {
                tb.HasTrigger("Insert_Tb_detmov1");
                tb.HasTrigger("Insert_Tb_detmov1Neg");
                tb.HasTrigger("Insert_Tb_detmov1_IngReq");
                tb.HasTrigger("TR_tb_detmov1_audit");
                tb.HasTrigger("ValidaInventarioCerrado_DetMov1");
            });

            entity.HasIndex(e => e.DetCodart, "IX_tb_detmov1_2").HasFillFactor(90);

            entity.Property(e => e.DetCodcia).IsFixedLength();
            entity.Property(e => e.DetCabece).IsFixedLength();
            entity.Property(e => e.DetTipo).IsFixedLength();
            entity.Property(e => e.DetNumero).IsFixedLength();
            entity.Property(e => e.DetBodega).IsFixedLength();
            entity.Property(e => e.DetCentro).IsFixedLength();
            entity.Property(e => e.DetCodart).IsFixedLength();
            entity.Property(e => e.DetCodcub).HasDefaultValue("A");
            entity.Property(e => e.DetCodpar).HasDefaultValueSql("((1))");
            entity.Property(e => e.DetEstado).IsFixedLength();
            entity.Property(e => e.DetIva).IsFixedLength();
            entity.Property(e => e.DetNomart).IsFixedLength();
            entity.Property(e => e.DetOrdpro).IsFixedLength();
            entity.Property(e => e.DetPunto).IsFixedLength();
            entity.Property(e => e.DetSubcen).IsFixedLength();
            entity.Property(e => e.DetTipine).IsFixedLength();
            entity.Property(e => e.DetUnimed).IsFixedLength();
        });


        modelBuilder.Entity<TbGrupo>(entity =>
        {
            entity.HasKey(e => new { e.LinCodigo, e.GrpCodigo }).IsClustered(false);

            entity.Property(e => e.CreaUser).IsFixedLength();
            entity.Property(e => e.EliUser).IsFixedLength();
            entity.Property(e => e.GrpEstado).IsFixedLength();
            entity.Property(e => e.GrpNombre).IsFixedLength();
            entity.Property(e => e.GrpObserv).IsFixedLength();
            entity.Property(e => e.GrpStatus).IsFixedLength();
            entity.Property(e => e.ModUser).IsFixedLength();
        });

        modelBuilder.Entity<TbItem>(entity =>
        {
            entity.HasKey(e => e.IteCodigo).IsClustered(false);

            entity.ToTable("tb_item", tb => tb.HasTrigger("Trg_tb_Item"));

            entity.HasIndex(e => e.LinCodigo, "IX_tb_item").HasFillFactor(90);

            entity.Property(e => e.IteCodigo).IsFixedLength();
            entity.Property(e => e.CiaCodigo).IsFixedLength();
            entity.Property(e => e.ClaCodigo).IsFixedLength();
            entity.Property(e => e.ConCodigo).IsFixedLength();
            entity.Property(e => e.CosCodigo).IsFixedLength();
            entity.Property(e => e.ImpCodigo).IsFixedLength();
            entity.Property(e => e.IteClacli).IsFixedLength();
            entity.Property(e => e.IteClave).IsFixedLength();
            entity.Property(e => e.IteCodbar).IsFixedLength();
            entity.Property(e => e.IteCombo).IsFixedLength();
            entity.Property(e => e.IteControlEtiq).HasDefaultValue("S");
            entity.Property(e => e.IteControlLote).HasDefaultValue("S");
            entity.Property(e => e.IteControlaPptoEgresos).HasComment("Indica si el item debe controlar presupuesto en cantiades en las transacciones de egreso.");
            entity.Property(e => e.IteCreaLoteMater).HasDefaultValue(true);
            entity.Property(e => e.IteCreuse).IsFixedLength();
            entity.Property(e => e.IteCtacvt).IsFixedLength();
            entity.Property(e => e.IteCtadev).IsFixedLength();
            entity.Property(e => e.IteCtainv).IsFixedLength();
            entity.Property(e => e.IteCtavta).IsFixedLength();
            entity.Property(e => e.IteCtdvta).IsFixedLength();
            entity.Property(e => e.IteElidat).IsFixedLength();
            entity.Property(e => e.IteEliuse).IsFixedLength();
            entity.Property(e => e.IteFeccad).IsFixedLength();
            entity.Property(e => e.IteFeccre).IsFixedLength();
            entity.Property(e => e.IteFoto).IsFixedLength();
            entity.Property(e => e.IteFulcon).IsFixedLength();
            entity.Property(e => e.IteKardex).IsFixedLength();
            entity.Property(e => e.IteLocali).IsFixedLength();
            entity.Property(e => e.IteLotes).IsFixedLength();
            entity.Property(e => e.IteModdat).IsFixedLength();
            entity.Property(e => e.IteModpre)
                .HasDefaultValue("0")
                .IsFixedLength();
            entity.Property(e => e.IteModuse).IsFixedLength();
            entity.Property(e => e.IteMostrarHojaDiaria)
                .HasDefaultValue("N")
                .IsFixedLength();
            entity.Property(e => e.IteNumpar).IsFixedLength();
            entity.Property(e => e.ItePercha).IsFixedLength();
            entity.Property(e => e.ItePeriodoPpto).IsFixedLength();
            entity.Property(e => e.ItePescon).IsFixedLength();
            entity.Property(e => e.ItePrcCodigo).HasComment("Se crea este campo para implementacion de IVA 5%. Este codigo debe existir en dbo.tb_imppor.");
            entity.Property(e => e.IteSerial).IsFixedLength();
            entity.Property(e => e.IteStatus).IsFixedLength();
            entity.Property(e => e.IteTipfac).IsFixedLength();
            entity.Property(e => e.IteUnimin).IsFixedLength();
            entity.Property(e => e.IteUnivol).IsFixedLength();
            entity.Property(e => e.IteVta)
                .HasDefaultValue("0")
                .IsFixedLength();
            entity.Property(e => e.MarCodigo).IsFixedLength();
            entity.Property(e => e.MedCodigo).IsFixedLength();
            entity.Property(e => e.SecCodigo).IsFixedLength();
            entity.Property(e => e.TipCodigo).IsFixedLength();
        });

        modelBuilder.Entity<TbLinea>(entity =>
        {
            entity.HasKey(e => e.LinCodigo).IsClustered(false);

            entity.Property(e => e.CreaUser).IsFixedLength();
            entity.Property(e => e.EliUser).IsFixedLength();
            entity.Property(e => e.LinCta1).IsFixedLength();
            entity.Property(e => e.LinCta2).IsFixedLength();
            entity.Property(e => e.LinCta3).IsFixedLength();
            entity.Property(e => e.LinCta4).IsFixedLength();
            entity.Property(e => e.LinCta5).IsFixedLength();
            entity.Property(e => e.LinEstado).IsFixedLength();
            entity.Property(e => e.LinNombre).IsFixedLength();
            entity.Property(e => e.ModUser).IsFixedLength();
        });


        modelBuilder.Entity<TbCobpag1>(entity =>
        {
            entity.ToTable("tb_cobpag1", tb =>
            {
                tb.HasTrigger("Insert_Tb_cobpag1");
                tb.HasTrigger("Insert_Tb_cobpag1GeneraXMLLiqCompra");
                tb.HasTrigger("Insert_Tb_cobpag1Neg");
                tb.HasTrigger("Insert_Tb_cobpag1NoAn1");
                tb.HasTrigger("Insert_Tb_cobpag1NoAn2");
                tb.HasTrigger("Insert_Tb_cobpag1NoAn3");
                tb.HasTrigger("Insert_Tb_cobpag1_2");
                tb.HasTrigger("Insert_Tb_cobpag1_DupliFact");
                tb.HasTrigger("Insert_Tb_cobpag1_ValidaFechaLiqCom");
                tb.HasTrigger("Insert_tb_cobpag1_NoDupliDctoFiscal");
                tb.HasTrigger("trg_tb_cobpag1");
            });

            entity.Property(e => e.CiaCodigo).IsFixedLength();
            entity.Property(e => e.CpgNumero).IsFixedLength();
            entity.Property(e => e.CpgTipo).IsFixedLength();
            entity.Property(e => e.CpgActu).HasDefaultValue("N");
            entity.Property(e => e.CpgAplret)
                .HasDefaultValue("1")
                .IsFixedLength();
            entity.Property(e => e.CpgAplretiva)
                .HasDefaultValueSql("((0))")
                .IsFixedLength();
            entity.Property(e => e.CpgBajoPresupuesto).HasComment("Indicador si las compras relacionadas a la deuda fueron generadas con o sin presupuesto de compras. El campo es actualizado al ejecutar lista de pagos pendientes (spr_OrdxApr2).");
            entity.Property(e => e.CpgBasecer).HasDefaultValue(0.0);
            entity.Property(e => e.CpgBaseimp).HasDefaultValue(0.0);
            entity.Property(e => e.CpgBl).IsFixedLength();
            entity.Property(e => e.CpgClasecli).IsFixedLength();
            entity.Property(e => e.CpgCrdtrib)
                .HasDefaultValue("06")
                .IsFixedLength();
            entity.Property(e => e.CpgCtaban).IsFixedLength();
            entity.Property(e => e.CpgCtades).IsFixedLength();
            entity.Property(e => e.CpgDeviva)
                .HasDefaultValue("S")
                .IsFixedLength();
            entity.Property(e => e.CpgEstado).IsFixedLength();
            entity.Property(e => e.CpgEstdeu)
                .HasDefaultValue("AC")
                .IsFixedLength();
            entity.Property(e => e.CpgFecapr).IsFixedLength();
            entity.Property(e => e.CpgFecbl).IsFixedLength();
            entity.Property(e => e.CpgFeccad).IsFixedLength();
            entity.Property(e => e.CpgFeccompexp).IsFixedLength();
            entity.Property(e => e.CpgFecdig).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CpgFecint).IsFixedLength();
            entity.Property(e => e.CpgFecinv).IsFixedLength();
            entity.Property(e => e.CpgFecliqant).IsFixedLength();
            entity.Property(e => e.CpgFeclle).IsFixedLength();
            entity.Property(e => e.CpgFiscal)
                .HasDefaultValue("S")
                .IsFixedLength();
            entity.Property(e => e.CpgFlujocaja)
                .HasDefaultValueSql("(0)")
                .IsFixedLength();
            entity.Property(e => e.CpgForpag).IsFixedLength();
            entity.Property(e => e.CpgLocext)
                .HasDefaultValue("1")
                .IsFixedLength();
            entity.Property(e => e.CpgMovimi).IsFixedLength();
            entity.Property(e => e.CpgNaviera).IsFixedLength();
            entity.Property(e => e.CpgNdias).IsFixedLength();
            entity.Property(e => e.CpgNdnc).IsFixedLength();
            entity.Property(e => e.CpgNpag).HasDefaultValue(1);
            entity.Property(e => e.CpgNrocompexp).IsFixedLength();
            entity.Property(e => e.CpgOrdpag)
                .HasDefaultValue("0")
                .IsFixedLength();
            entity.Property(e => e.CpgProvee).IsFixedLength();
            entity.Property(e => e.CpgRefere).IsFixedLength();
            entity.Property(e => e.CpgReferendo).IsFixedLength();
            entity.Property(e => e.CpgRetfue).IsFixedLength();
            entity.Property(e => e.CpgRetiva).IsFixedLength();
            entity.Property(e => e.CpgTipdeu).IsFixedLength();
            entity.Property(e => e.CpgTipo2)
                .HasDefaultValue("01")
                .IsFixedLength();
            entity.Property(e => e.CpgTipodocexp).IsFixedLength();
            entity.Property(e => e.CpgUltfec).IsFixedLength();
            entity.Property(e => e.CpgValice).HasDefaultValue(0.0);
        });

        modelBuilder.Entity<TbCobpag1Dxl>(entity =>
        {
            entity.Property(e => e.DxlTiplot).IsFixedLength();
        });

        modelBuilder.Entity<TbDatfac>(entity =>
        {
            entity.Property(e => e.DfaNumpag).IsFixedLength();
            entity.Property(e => e.DfaCampo3).IsFixedLength();
            entity.Property(e => e.DfaCampo4).IsFixedLength();
            entity.Property(e => e.DfaCheque).IsFixedLength();
            entity.Property(e => e.DfaCiudad).IsFixedLength();
            entity.Property(e => e.DfaCodcia).HasDefaultValue("SONG");
            entity.Property(e => e.DfaEstado)
                .HasDefaultValue("AC")
                .IsFixedLength();
            entity.Property(e => e.DfaFeccad).IsFixedLength();
            entity.Property(e => e.DfaFecent).IsFixedLength();
            entity.Property(e => e.DfaFecha).IsFixedLength();
            entity.Property(e => e.DfaFecpag).IsFixedLength();
            entity.Property(e => e.DfaFiscal).IsFixedLength();
            entity.Property(e => e.DfaNroser)
                .HasDefaultValue(" ")
                .IsFixedLength();
            entity.Property(e => e.DfaNroser2).IsFixedLength();
            entity.Property(e => e.DfaNumfac).IsFixedLength();
            entity.Property(e => e.DfaNumimp).IsFixedLength();
            entity.Property(e => e.DfaProvee).IsFixedLength();
            entity.Property(e => e.DfaProvin).IsFixedLength();
            entity.Property(e => e.DfaTipo).IsFixedLength();
        });


        modelBuilder
            .Entity<CostoMatEmpaDto>()
            .HasNoKey()
            .ToView(null);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
