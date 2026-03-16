using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AccountContentService.Infrastructure.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IBlogPostRepository, BlogPostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();

            return services;
        }
    }
}
