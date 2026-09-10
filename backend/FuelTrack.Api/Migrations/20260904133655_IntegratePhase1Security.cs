using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class IntegratePhase1Security : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SecurityVersion ya agregada por AddUserSecurityVersion (20260830190110)
            // IX_Roles_Nombre ya creado por AddUniqueRoleName (20260830190256)
            // Trigger append-only ya creado por ProtectAuditAppendOnly (20260831222440)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: los rollbacks corresponden a cada migración individual
        }
    }
}
