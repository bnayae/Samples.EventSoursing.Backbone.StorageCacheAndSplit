using EventSourcing.Backbone;
using Demo.Abstractions;

// Configuration: https://medium.com/@gparlakov/the-confusion-of-asp-net-configuration-with-environment-variables-c06c545ef732

namespace Demo;

/// <summary>
///  DI Extensions for ASP.NET Core
/// </summary>
public static class JobOfferProducerExtensions
{
    /// <summary>
    /// Register a producer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="s3Bucket">The s3 bucket.</param>
    /// <returns></returns>
    public static WebApplicationBuilder AddJobOfferProducer (
            this WebApplicationBuilder builder,
            string uri
            , string s3Bucket
            )
    {
        IServiceCollection services = builder.Services;
        IWebHostEnvironment environment = builder.Environment;
        string env = environment.EnvironmentName;

        var s3Options = new S3Options { Bucket = s3Bucket };
        services.AddSingleton(ioc =>
        {
            return BuildProducer(uri, env, ioc
                                , s3Options 
                                );
        });

        return builder;
    }

    /// <summary>
    /// Register a producer when the URI of the service used as the registration's key.
    /// See: https://medium.com/weknow-network/keyed-dependency-injection-using-net-630bd73d3672.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="s3Bucket">The s3 bucket.</param>
    /// <returns></returns>
    public static WebApplicationBuilder AddKeyedJobOfferProducer (
                this WebApplicationBuilder builder,
                string uri
                , string s3Bucket
                )
    {
        IServiceCollection services = builder.Services;
        IWebHostEnvironment environment = builder.Environment;
        string env = environment.EnvironmentName;
        
        var s3Options = new S3Options { Bucket = s3Bucket };
        services.AddKeyedSingleton(ioc =>
        {
            return BuildProducer(uri, env, ioc
                                    , s3Options
            );
        }, uri);

        return builder;
    }

    private static IJobOfferProducer BuildProducer(string uri, Env env, IServiceProvider ioc
    , S3Options s3Options
    )
    {
        ILogger logger = ioc.GetService<ILogger<Program>>() ?? throw new EventSourcingException("Logger is missing");
        IJobOfferProducer producer = ioc.ResolveRedisProducerChannel()
                                .ResolveRedisHashStorage()
                                .ResolveS3Storage(s3Options)
                                .Environment(env)
                                .Uri(uri)
                                .WithLogger(logger)
                                .BuildJobOfferProducer();
        return producer;
    }
}
