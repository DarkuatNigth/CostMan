namespace CostManagement.Dominio.Entidades
{
    public static class ValueObjectsCostoProductivo
    {
        // LECTURA: cada SP vive dentro de SU propia conexión/contexto.
        public const string spConfiguracionCostoProductivoCostos =
            "costos.sp_costoProductivo_costos_configuracion";

        public const string spSaldosCostoProductivoSong =
            "dbo.sp_costoProductivo_song_saldos";

        // PERSISTENCIA: ConnectionCostos.
        public const string spAnularPeriodo = "costos.sp_costoProductivo_anularPeriodo";
        public const string spGuardarFila = "costos.sp_costoProductivo_guardarFila";
        public const string spConsultarPeriodo = "costos.sp_costoProductivo_consultarPeriodo";
    }
}
