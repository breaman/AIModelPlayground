using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UserGroupSiteOpus5.Tests.Infrastructure;

/// <summary>
/// Adapts the production model so it can be created and queried on SQLite.
/// </summary>
/// <remarks>
/// <para>
/// Two provider differences have to be bridged, neither of which reflects a problem with the
/// production configuration:
/// </para>
/// <list type="bullet">
/// <item>
/// The model pins SQL Server column types (see <c>ColumnTypes</c>). One of them,
/// <c>nvarchar(max)</c>, is not valid SQLite DDL — SQLite accepts only numeric arguments in a type
/// specifier — so <c>EnsureCreated</c> fails without clearing them.
/// </item>
/// <item>
/// SQLite cannot <c>ORDER BY</c> a <see cref="DateTimeOffset"/>. Storing them via
/// <see cref="DateTimeOffsetToBinaryConverter"/> yields a long that sorts in true chronological
/// order across offsets, so the ordering the services ask for is the ordering the tests observe.
/// </item>
/// </list>
/// <para>
/// Keys, indexes, and relationships are left exactly as production declares them, which is what
/// the data-layer tests are actually exercising.
/// </para>
/// </remarks>
internal sealed class SqliteModelCustomizer(ModelCustomizerDependencies dependencies)
    : RelationalModelCustomizer(dependencies)
{
    /// <inheritdoc />
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        var dateTimeOffsetConverter = new DateTimeOffsetToBinaryConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnType(null);

                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
            }
        }
    }
}
