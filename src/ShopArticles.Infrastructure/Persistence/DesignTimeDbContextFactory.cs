using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopArticles.Infrastructure.Persistence;

// Used only by the dotnet-ef tool when creating migrations, so it works whatever Storage:Provider is set to.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ArticlesDbContext>
{
    public ArticlesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ArticlesDbContext>().UseSqlite("Data Source=articles.db").Options);
}
