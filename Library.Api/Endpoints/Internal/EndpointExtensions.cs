using System.Reflection;

namespace Library.Api.Endpoints.Internal;

public static class EndpointExtensions
{
    public static void AddEndpoints<TMarker>(this IServiceCollection services, IConfiguration configuration)
        => AddEndpoints(services, typeof(TMarker), configuration);

    public static void AddEndpoints(this IServiceCollection services, Type typeMarker, IConfiguration configuration)
    {
        IEnumerable<TypeInfo> endpointsTypes = GetEndpointTypesFromAssemblyContaining(typeMarker);

        foreach (var endpointsType in endpointsTypes)
        {
            endpointsType.GetMethod(nameof(IEndpoints.AddServices))!
                .Invoke(null, new object[] { services, configuration });
        }
    }


    public static void UseEndpoints<TMarker>(this IApplicationBuilder app)
        => UseEndpoints(app, typeof(TMarker));
    public static void UseEndpoints(this IApplicationBuilder app, Type typeMarker)
    {
        IEnumerable<TypeInfo> endpointsTypes = GetEndpointTypesFromAssemblyContaining(typeMarker);

        foreach (var endpointsType in endpointsTypes)
        {
            endpointsType.GetMethod(nameof(IEndpoints.DefineEndpoints))!
                .Invoke(null, new object[] { app });
        }
    }
    
    private static IEnumerable<TypeInfo> GetEndpointTypesFromAssemblyContaining(Type typeMarker)
    {
        var endpointsTypes = typeMarker.Assembly.DefinedTypes
            .Where(x => !x.IsAbstract)
            .Where(x => !x.IsInterface)
            .Where(x => typeof(IEndpoints).IsAssignableFrom(x));
        return endpointsTypes;
    }
}