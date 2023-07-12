using EventSourcing.Backbone;

// Configuration: https://medium.com/@gparlakov/the-confusion-of-asp-net-configuration-with-environment-variables-c06c545ef732

namespace Demo;

/// <summary>
///  DI Extensions for ASP.NET Core
/// </summary>
public static class ConsumerExtensions
{
    /// <summary>
    /// Register a consumer.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="s3Bucket">The s3 bucket.</param>
    /// <returns></returns>
    public static WebApplicationBuilder AddConsumer (
                    this WebApplicationBuilder builder,
                    string uri
                    , string s3Bucket
                    )
    {
        IServiceCollection services = builder.Services;
        IWebHostEnvironment environment = builder.Environment;
        string env = environment.EnvironmentName;

        var s3Options = new S3ConsumerOptions { Bucket = s3Bucket };
        services.AddSingleton(ioc =>
        {
            return BuildConsumer(uri, env, ioc 
            , s3Options 
            );
        });

        return builder;
    }

    /// <summary>
    /// Register a consumer when the URI of the service used as the registration's key.
    /// See: https://medium.com/weknow-network/keyed-dependency-injection-using-net-630bd73d3672
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="uri">The URI of the stream (which is also used as the DI key).</param>
    /// <param name="s3Bucket">The s3 bucket.</param>
    /// <returns></returns>
    public static WebApplicationBuilder AddKeyedConsumer (
                    this WebApplicationBuilder builder,
                    string uri
                    , string s3Bucket
                    )
    {
        IServiceCollection services = builder.Services;
        IWebHostEnvironment environment = builder.Environment;
        string env = environment.EnvironmentName;

        var s3Options = new S3ConsumerOptions { Bucket = s3Bucket };
        services.AddKeyedSingleton(ioc =>
        {
            return BuildConsumer(uri
                                , env
                                , ioc
                                , s3Options
                                );
        }, uri);

        return builder;
    }

    /// <summary>
    /// Builds the consumer.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <param name="env">The environment.</param>
    /// <param name="ioc">The DI provider.</param>
    /// <param name="s3Options">The s3 options.</param>
    /// <returns></returns>
    private static IConsumerReadyBuilder BuildConsumer(string uri
                                                        , Env env, IServiceProvider ioc
                                                        , S3ConsumerOptions s3Options
                                                        )
    {
        return ioc.ResolveRedisConsumerChannel()
                        // Redis Storage hold the PII
                        .ResolveRedisHashStorage()
                        // S3 storage
                        .ResolveS3Storage(s3Options)
                        .WithOptions(o => o with
                        {
                            OriginFilter = MessageOrigin.Original,
                            AckBehavior = AckBehavior.OnSucceed,
                        })
                        .Environment(env)
                        .Uri(uri);
    }
}
