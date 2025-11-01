using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PaymentService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddServices(this IServiceCollection services)
        {
            // Pega todas as classes públicas na assembly que terminam com "Service"
            var serviceTypes = Assembly.GetExecutingAssembly()
                                       .GetTypes()
                                       .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Services"));

            foreach (var type in serviceTypes)
            {
                services.AddScoped(type); // registra como Scoped
            }
        }
    }
}