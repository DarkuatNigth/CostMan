using System;
using System.Collections.Generic;
using CostManagement.Infraestructura.EF_Core;
using CostManagementService.Infraestructura.EF_Core;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.DBContext;

public partial class CostosDbContext : DbContext
{
    public CostosDbContext()
    {
    }

    public CostosDbContext(DbContextOptions<CostosDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TbCostoEmpaqueCab> TbCostoEmpaqueCab { get; set; }

    public virtual DbSet<TbCostoEmpaqueDet> TbCostoEmpaqueDet { get; set; }

    public virtual DbSet<TbMateriaPrimaFrescoValorizada> TbMateriaPrimaFrescoValorizada { get; set; }

    public virtual DbSet<TbMateriaPrimaReproValorizada> TbMateriaPrimaReproValorizada { get; set; }

    public virtual DbSet<TbMateriaPrimaFrescoValorizadaMotivoCambios> TbMateriaPrimaFrescoValorizadaMotivoCambios { get; set; }

    public virtual DbSet<TbMateriaPrimaSaldo> TbMateriaPrimaSaldo { get; set; }

    public virtual DbSet<TbCatalogoCab> TbCatalogoCab { get; set; }

    public virtual DbSet<TbCatalogoDet> TbCatalogoDet { get; set; }

    public virtual DbSet<TbParametroCosteo> TbParametroCosteo { get; set; }

    public virtual DbSet<TbProcesoCosteo> TbProcesoCosteo { get; set; }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbCostoEmpaqueCab>(entity =>
        {
            entity.ToTable("tb_costoEmpaqueCab", "costos", tb => tb.HasComment("Cabecera del cálculo de costos de materiales de empaque por lote y producto terminado"));

            entity.Property(e => e.CcEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.CcFechaCrea).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<TbCostoEmpaqueDet>(entity =>
        {
            entity.ToTable("tb_costoEmpaqueDet", "costos", tb => tb.HasComment("Detalle de materiales de empaque utilizados y sus costos por producto y lote"));

            entity.Property(e => e.CdEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.CdFechaCrea).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CdOrigenCosto).IsFixedLength();

            entity.HasOne(d => d.CdCc).WithMany(p => p.TbCostoEmpaqueDet)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_costoEmpaqueCab_costoEmpaqueDet");
        });

        modelBuilder.Entity<TbMateriaPrimaFrescoValorizada>(entity =>
        {
            entity.Property(e => e.MfEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.MfFechaCrea).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<TbMateriaPrimaReproValorizada>(entity =>
        {
            entity.HasKey(e => e.MrId).HasName("PK_tb_tb_materiaPrimaReproValorizada");

            entity.Property(e => e.MrEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.MrFechaCrea).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<TbMateriaPrimaSaldo>(entity =>
        {
            entity.Property(e => e.MpsEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.MpsFechaCrea).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.MpsTipo).IsFixedLength();
        });

        modelBuilder.Entity<TbCatalogoCab>(entity =>
        {
            entity.ToTable("tb_catalogoCab", "general", tb => tb.HasComment("Cabecera de catalogo"));

            entity.Property(e => e.CatId).HasComment("Id automatico.");
            entity.Property(e => e.CatCodigo).HasComment("Codigo de cabecera");
            entity.Property(e => e.CatDescripcion).HasComment("Descripcion");
            entity.Property(e => e.CatEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.CatEstado).HasComment("Activo / inactivo");
            entity.Property(e => e.CatFechaCrea).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CatUsuarioCrea).HasDefaultValueSql("(original_login())");
        });

        modelBuilder.Entity<TbCatalogoDet>(entity =>
        {
            entity.ToTable("tb_catalogoDet", "general", tb => tb.HasComment("Detalle de catalogo"));

            entity.Property(e => e.DetIdCab).HasComment("Id automatico.");
            entity.Property(e => e.DetCodigo).HasComment("Codigo del detalle");
            entity.Property(e => e.DetDescripcion).HasComment("Descripcion");
            entity.Property(e => e.DetDescripcionCorta).HasComment("Descripcion");
            entity.Property(e => e.DetEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.DetEstado)
                .HasDefaultValue(true)
                .HasComment("Activo / inactivo");
            entity.Property(e => e.DetFechaCrea).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DetUsuarioCrea).HasDefaultValueSql("(original_login())");

            entity.HasOne(d => d.DetIdCabNavigation).WithMany(p => p.TbCatalogoDet)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_catalogoDet_catalogoCab");
        });

        modelBuilder.Entity<TbParametroCosteo>(entity =>
        {
            entity.ToTable("tb_parametroCosteo", "costos", tb => tb.HasComment("Parametrización de costeo de produccion por periodo"));

            entity.Property(e => e.PcEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.PcFechaCrea).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.PcPr).WithMany(p => p.TbParametroCosteo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_parametroCosteo_procesoCosteo");
        });

        modelBuilder.Entity<TbProcesoCosteo>(entity =>
        {
            entity.ToTable("tb_procesoCosteo", "costos", tb => tb.HasComment("Procesos para costeo de produccion (primarios: recepcion, codificacion, descabezado); congelacion (brine, iqf, tunel), etc."));

            entity.Property(e => e.PrId).ValueGeneratedOnAdd();
            entity.Property(e => e.PrEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.PrFechaCrea).HasDefaultValueSql("(getdate())");
        });


        modelBuilder.Entity<TbMateriaPrimaFrescoValorizadaMotivoCambios>(entity =>
        {
            entity.ToTable("tb_materiaPrimaFrescoValorizada_MotivoCambios", "costos", tb => tb.HasComment("Motivos de cambios de datos"));

            entity.Property(e => e.McEquipoCrea).HasDefaultValueSql("(host_name())");
            entity.Property(e => e.McFechaCrea).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.McId).ValueGeneratedOnAdd();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
