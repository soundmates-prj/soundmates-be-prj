using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class AccountContentDbContextFactory
    : IDesignTimeDbContextFactory<AccountContentDbContext>
{
    public AccountContentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountContentDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=account_db;Username=postgres;Password=postgres"
        );

        return new AccountContentDbContext(optionsBuilder.Options);
    }
}