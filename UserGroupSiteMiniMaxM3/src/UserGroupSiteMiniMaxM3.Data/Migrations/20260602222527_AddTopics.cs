using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserGroupSiteMiniMaxM3.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicSuggestions_AspNetUsers_SuggestedByUserId",
                        column: x => x.SuggestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TopicVolunteers",
                columns: table => new
                {
                    TopicSuggestionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicVolunteers", x => new { x.TopicSuggestionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TopicVolunteers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicVolunteers_TopicSuggestions_TopicSuggestionId",
                        column: x => x.TopicSuggestionId,
                        principalTable: "TopicSuggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicVotes",
                columns: table => new
                {
                    TopicSuggestionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicVotes", x => new { x.TopicSuggestionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TopicVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopicVotes_TopicSuggestions_TopicSuggestionId",
                        column: x => x.TopicSuggestionId,
                        principalTable: "TopicSuggestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicSuggestions_CreatedOn",
                table: "TopicSuggestions",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_TopicSuggestions_SuggestedByUserId",
                table: "TopicSuggestions",
                column: "SuggestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicVolunteers_UserId",
                table: "TopicVolunteers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicVotes_UserId",
                table: "TopicVotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicVolunteers");

            migrationBuilder.DropTable(
                name: "TopicVotes");

            migrationBuilder.DropTable(
                name: "TopicSuggestions");
        }
    }
}