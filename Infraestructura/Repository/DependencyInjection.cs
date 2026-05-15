using CostManagement.Aplicación.Features;
using CostManagementService.Aplicacion.Features;
using CostManagement.Infraestructura.Utils;
using System.Reflection;

namespace CostManagementService.Infraestructura.Repository
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // 1. Registro Automático por Convención (Reflection)
            // Busca todas las clases en el namespace de Services y las asocia con su Interfaz I...
            var assembly = Assembly.GetExecutingAssembly();

            var serviceTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null && t.Namespace.EndsWith("Repository.Services"));

            foreach (var serviceType in serviceTypes)
            {
                // Busca la interfaz que coincida con el nombre de la clase (Ej: MateriaPrima -> IMateriaPrima)
                var interfaceType = serviceType.GetInterfaces()
                    .FirstOrDefault(i => i.Name == $"I{serviceType.Name}");

                if (interfaceType != null)
                {
                    services.AddScoped(interfaceType, serviceType);
                }
            }

            // 2. Registros específicos que no siguen la convención o requieren otro ciclo de vida
            services.AddSingleton<OpenJsonToInClauseInterceptor>();
            //services.AddScoped<CalculoCostosFeature>();
            //services.AddScoped<OperacionComercialFeature>();

            var featureTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Feature"));

            foreach (var featureType in featureTypes)
            {
                services.AddScoped(featureType);
            }


            return services;
        }
    }
}
