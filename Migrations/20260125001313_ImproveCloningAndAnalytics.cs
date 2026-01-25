using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class ImproveCloningAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Šī daļa padara Version obligātu (aizpilda tukšos ar tukšu tekstu)
            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "ProjectTemplates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // Šeit ir mans uzlabojums:
            // Mēs izmantojam SQL funkciju GETUTCDATE(), lai veciem projektiem ieliktu šodienu,
            // nevis 0001. gadu.
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {         

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "ProjectTemplates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
