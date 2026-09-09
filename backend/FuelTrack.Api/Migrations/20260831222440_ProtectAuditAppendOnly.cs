using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProtectAuditAppendOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION prevent_auditorias_modification()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'Auditorias es append-only: UPDATE y DELETE están prohibidos.'
                        USING ERRCODE = '55000';
                END;
                $function$;

                CREATE TRIGGER auditorias_append_only
                BEFORE UPDATE OR DELETE ON "Auditorias"
                FOR EACH ROW EXECUTE FUNCTION prevent_auditorias_modification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS auditorias_append_only ON "Auditorias";
                DROP FUNCTION IF EXISTS prevent_auditorias_modification();
                """);
        }
    }
}
