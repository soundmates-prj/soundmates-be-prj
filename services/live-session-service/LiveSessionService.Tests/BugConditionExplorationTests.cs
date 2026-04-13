using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace LiveSessionService.Tests;

/// <summary>
/// Bug Condition Exploration Tests for Missing Music Upload Migration
/// 
/// CRITICAL: These tests are EXPECTED TO FAIL on unfixed database.
/// Failure confirms the bug exists (missing schema elements).
/// 
/// After migrations are applied, these same tests should PASS,
/// confirming the bug is fixed.
/// </summary>
public class BugConditionExplorationTests : IDisposable
{
    private readonly LiveSessionDbContext _dbContext;
    private readonly NpgsqlConnection _connection;

    public BugConditionExplorationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<LiveSessionDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        _dbContext = new LiveSessionDbContext(options, configuration);
        _connection = new NpgsqlConnection(GetConnectionString());
        _connection.Open();
    }

    private static string GetConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
        var database = "live_session_db";

        return $"Host={host};Port={port};Database={database};Username={user};Password={password};Ssl Mode=Disable;Trust Server Certificate=True";
    }

    /// <summary>
    /// **Validates: Requirements 2.5**
    /// 
    /// Property 1: Bug Condition - Database Schema Matches Code Expectations
    /// 
    /// Verifies that OriginalSourceType column exists in media_files table.
    /// 
    /// EXPECTED ON UNFIXED DATABASE: FAIL (column does not exist)
    /// EXPECTED AFTER MIGRATION: PASS (column exists)
    /// </summary>
    [Fact]
    public async Task OriginalSourceType_Column_Should_Exist_In_MediaFiles_Table()
    {
        // Arrange & Act
        var columnExists = await CheckColumnExists("media_files", "OriginalSourceType");

        // Assert
        Assert.True(columnExists, 
            "Column 'OriginalSourceType' should exist in 'media_files' table. " +
            "This indicates migration '20260412125000_AddMediaFileOriginalSourceType' has been applied.");
    }

    /// <summary>
    /// **Validates: Requirements 2.6**
    /// 
    /// Property 1: Bug Condition - Database Schema Matches Code Expectations
    /// 
    /// Verifies that station_media_files table exists.
    /// 
    /// EXPECTED ON UNFIXED DATABASE: FAIL (table does not exist)
    /// EXPECTED AFTER MIGRATION: PASS (table exists)
    /// </summary>
    [Fact]
    public async Task StationMediaFiles_Table_Should_Exist()
    {
        // Arrange & Act
        var tableExists = await CheckTableExists("station_media_files");

        // Assert
        Assert.True(tableExists, 
            "Table 'station_media_files' should exist. " +
            "This indicates migration '20260412130000_AddStationMediaFileMapping' has been applied.");
    }

    /// <summary>
    /// **Validates: Requirements 2.5**
    /// 
    /// Property 1: Bug Condition - Database Schema Matches Code Expectations
    /// 
    /// Verifies that migration 20260412125000_AddMediaFileOriginalSourceType is applied.
    /// 
    /// EXPECTED ON UNFIXED DATABASE: FAIL (migration not in history)
    /// EXPECTED AFTER MIGRATION: PASS (migration in history)
    /// </summary>
    [Fact]
    public async Task Migration_AddMediaFileOriginalSourceType_Should_Be_Applied()
    {
        // Arrange & Act
        var migrationApplied = await CheckMigrationApplied("20260412125000_AddMediaFileOriginalSourceType");

        // Assert
        Assert.True(migrationApplied, 
            "Migration '20260412125000_AddMediaFileOriginalSourceType' should be in __EFMigrationsHistory table.");
    }

    /// <summary>
    /// **Validates: Requirements 2.6**
    /// 
    /// Property 1: Bug Condition - Database Schema Matches Code Expectations
    /// 
    /// Verifies that migration 20260412130000_AddStationMediaFileMapping is applied.
    /// 
    /// EXPECTED ON UNFIXED DATABASE: FAIL (migration not in history)
    /// EXPECTED AFTER MIGRATION: PASS (migration in history)
    /// </summary>
    [Fact]
    public async Task Migration_AddStationMediaFileMapping_Should_Be_Applied()
    {
        // Arrange & Act
        var migrationApplied = await CheckMigrationApplied("20260412130000_AddStationMediaFileMapping");

        // Assert
        Assert.True(migrationApplied, 
            "Migration '20260412130000_AddStationMediaFileMapping' should be in __EFMigrationsHistory table.");
    }

    /// <summary>
    /// **Validates: Requirements 2.5**
    /// 
    /// Property 1: Bug Condition - Database Schema Matches Code Expectations
    /// 
    /// Verifies that OriginalSourceType column has correct data type and default value.
    /// 
    /// EXPECTED ON UNFIXED DATABASE: FAIL (column does not exist)
    /// EXPECTED AFTER MIGRATION: PASS (column has correct type and default)
    /// </summary>
    [Fact]
    public async Task OriginalSourceType_Column_Should_Have_Correct_Type_And_Default()
    {
        // Arrange & Act
        var columnInfo = await GetColumnInfo("media_files", "OriginalSourceType");

        // Assert
        Assert.NotNull(columnInfo);
        Assert.Equal("character varying", columnInfo.DataType);
        Assert.Equal(20, columnInfo.MaxLength);
        Assert.Equal("NO", columnInfo.IsNullable); // NOT NULL
        Assert.Equal("'system'::character varying", columnInfo.ColumnDefault);
    }

    /// <summary>
    /// **Validates: Requirements 2.6**
    /// 
    /// Property 1: Bug Condition - Database Schema Matches Code Expectations
    /// 
    /// Verifies that station_media_files table has correct structure.
    /// 
    /// EXPECTED ON UNFIXED DATABASE: FAIL (table does not exist)
    /// EXPECTED AFTER MIGRATION: PASS (table has correct columns)
    /// </summary>
    [Fact]
    public async Task StationMediaFiles_Table_Should_Have_Correct_Structure()
    {
        // Arrange & Act
        var columns = await GetTableColumns("station_media_files");

        // Assert
        Assert.Contains(columns, c => c.ColumnName == "Id");
        Assert.Contains(columns, c => c.ColumnName == "MediaFileId");
        Assert.Contains(columns, c => c.ColumnName == "StationId");
        Assert.Contains(columns, c => c.ColumnName == "AzuraCastMediaId");
        Assert.Contains(columns, c => c.ColumnName == "ImportedAt");
    }

    // Helper methods

    private async Task<bool> CheckColumnExists(string tableName, string columnName)
    {
        var sql = @"
            SELECT EXISTS (
                SELECT 1 
                FROM information_schema.columns 
                WHERE table_name = @tableName 
                AND column_name = @columnName
            )";

        await using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        cmd.Parameters.AddWithValue("columnName", columnName);

        var result = await cmd.ExecuteScalarAsync();
        return result is bool exists && exists;
    }

    private async Task<bool> CheckTableExists(string tableName)
    {
        var sql = @"
            SELECT EXISTS (
                SELECT 1 
                FROM information_schema.tables 
                WHERE table_name = @tableName
            )";

        await using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("tableName", tableName);

        var result = await cmd.ExecuteScalarAsync();
        return result is bool exists && exists;
    }

    private async Task<bool> CheckMigrationApplied(string migrationId)
    {
        var sql = @"
            SELECT EXISTS (
                SELECT 1 
                FROM ""__EFMigrationsHistory"" 
                WHERE ""MigrationId"" = @migrationId
            )";

        await using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("migrationId", migrationId);

        var result = await cmd.ExecuteScalarAsync();
        return result is bool exists && exists;
    }

    private async Task<ColumnInfo?> GetColumnInfo(string tableName, string columnName)
    {
        var sql = @"
            SELECT 
                data_type,
                character_maximum_length,
                is_nullable,
                column_default
            FROM information_schema.columns 
            WHERE table_name = @tableName 
            AND column_name = @columnName";

        await using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        cmd.Parameters.AddWithValue("columnName", columnName);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ColumnInfo
            {
                DataType = reader.GetString(0),
                MaxLength = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                IsNullable = reader.GetString(2),
                ColumnDefault = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }

        return null;
    }

    private async Task<List<ColumnInfo>> GetTableColumns(string tableName)
    {
        var sql = @"
            SELECT 
                column_name,
                data_type,
                character_maximum_length,
                is_nullable,
                column_default
            FROM information_schema.columns 
            WHERE table_name = @tableName
            ORDER BY ordinal_position";

        await using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("tableName", tableName);

        var columns = new List<ColumnInfo>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                IsNullable = reader.GetString(3),
                ColumnDefault = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return columns;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _dbContext?.Dispose();
    }

    private class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public string IsNullable { get; set; } = string.Empty;
        public string? ColumnDefault { get; set; }
    }
}
