﻿//using IdentityServer4.AccessTokenValidation;
//using IdentityServer4.Models;
//using IdentityServer4.Test;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using System.Security.Claims;

namespace Ocelot.AcceptanceTests;

public sealed class ClaimsToQueryStringForwardingTests : IDisposable
{
    private readonly Steps _steps;
    private IWebHost _servicebuilder;

    //private IWebHost _identityServerBuilder;
    //private readonly Action<IdentityServerAuthenticationOptions> _options;
    private readonly string _identityServerRootUrl;
    private string _downstreamQueryString;

    public ClaimsToQueryStringForwardingTests()
    {
        _steps = new Steps();
        var identityServerPort = PortFinder.GetRandomPort();
        _identityServerRootUrl = $"http://localhost:{identityServerPort}";

        //_options = o =>
        //{
        //    o.Authority = _identityServerRootUrl;
        //    o.ApiName = "api";
        //    o.RequireHttpsMetadata = false;
        //    o.SupportedTokens = SupportedTokens.Both;
        //    o.ApiSecret = "secret";
        //};
    }

    [Fact(Skip = "TODO: Requires redevelopment because IdentityServer4 is deprecated")]
    public void Should_return_response_200_and_foward_claim_as_query_string()
    {
        //var user = new TestUser
        //{
        //    Username = "test",
        //    Password = "test",
        //    SubjectId = "registered|1231231",
        //    Claims = new List<Claim>
        //    {
        //        new("CustomerId", "123"),
        //        new("LocationId", "1"),
        //    },
        //};
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "Test",
                        AllowedScopes = new List<string>
                        {
                            "openid", "offline_access", "api",
                        },
                    },
                    AddQueriesToRequest =
                    {
                        {"CustomerId", "Claims[CustomerId] > value"},
                        {"LocationId", "Claims[LocationId] > value"},
                        {"UserType", "Claims[sub] > value[0] > |"},
                        {"UserId", "Claims[sub] > value[1] > |"},
                    },
                },
            },
        };

        this.Given(x => null) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt, user))
            .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200))
            .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))

            //.And(x => _steps.GivenOcelotIsRunning(_options, "Test"))
            .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("CustomerId: 123 LocationId: 1 UserType: registered UserId: 1231231"))
            .BDDfy();
    }

    [Fact(Skip = "TODO: Requires redevelopment because IdentityServer4 is deprecated")]
    public void Should_return_response_200_and_foward_claim_as_query_string_and_preserve_original_string()
    {
        //var user = new TestUser
        //{
        //    Username = "test",
        //    Password = "test",
        //    SubjectId = "registered|1231231",
        //    Claims = new List<Claim>
        //    {
        //        new("CustomerId", "123"),
        //        new("LocationId", "1"),
        //    },
        //};
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "Test",
                        AllowedScopes = new List<string>
                        {
                            "openid", "offline_access", "api",
                        },
                    },
                    AddQueriesToRequest =
                    {
                        {"CustomerId", "Claims[CustomerId] > value"},
                        {"LocationId", "Claims[LocationId] > value"},
                        {"UserType", "Claims[sub] > value[0] > |"},
                        {"UserId", "Claims[sub] > value[1] > |"},
                    },
                },
            },
        };

        this.Given(x => null) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt, user))
            .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200))
            .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))

            //.And(x => _steps.GivenOcelotIsRunning(_options, "Test"))
            .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/?test=1&test=2"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("CustomerId: 123 LocationId: 1 UserType: registered UserId: 1231231"))
            .And(_ => ThenTheQueryStringIs("?test=1&test=2&CustomerId=123&LocationId=1&UserId=1231231&UserType=registered"))
            .BDDfy();
    }

    private void ThenTheQueryStringIs(string queryString)
    {
        _downstreamQueryString.ShouldBe(queryString);
    }

    private void GivenThereIsAServiceRunningOn(string url, int statusCode)
    {
        _servicebuilder = TestHostBuilder.Create()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    _downstreamQueryString = context.Request.QueryString.Value;

                    context.Request.Query.TryGetValue("CustomerId", out var customerId);
                    context.Request.Query.TryGetValue("LocationId", out var locationId);
                    context.Request.Query.TryGetValue("UserType", out var userType);
                    context.Request.Query.TryGetValue("UserId", out var userId);

                    var responseBody = $"CustomerId: {customerId} LocationId: {locationId} UserType: {userType} UserId: {userId}";
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsync(responseBody);
                });
            })
            .Build();

        _servicebuilder.Start();
    }

    //private async Task GivenThereIsAnIdentityServerOn(string url, string apiName, AccessTokenType tokenType, TestUser user)
    //{
    //    _identityServerBuilder = TestHostBuilder.Create()
    //        .UseUrls(url)
    //        .UseKestrel()
    //        .UseContentRoot(Directory.GetCurrentDirectory())
    //        .UseIISIntegration()
    //        .UseUrls(url)
    //        .ConfigureServices(services =>
    //        {
    //            services.AddLogging();
    //            services.AddIdentityServer()
    //                .AddDeveloperSigningCredential()
    //                .AddInMemoryApiScopes(new List<ApiScope>
    //                {
    //                    new(apiName, "test"),
    //                    new("openid", "test"),
    //                    new("offline_access", "test"),
    //                    new("api.readOnly", "test"),
    //                })
    //                .AddInMemoryApiResources(new List<ApiResource>
    //                {
    //                    new()
    //                    {
    //                        Name = apiName,
    //                        Description = "My API",
    //                        Enabled = true,
    //                        DisplayName = "test",
    //                        Scopes = new List<string>
    //                        {
    //                            "api",
    //                            "openid",
    //                            "offline_access",
    //                        },
    //                        ApiSecrets = new List<Secret>
    //                        {
    //                            new()
    //                            {
    //                                Value = "secret".Sha256(),
    //                            },
    //                        },
    //                        UserClaims = new List<string>
    //                        {
    //                            "CustomerId", "LocationId", "UserType", "UserId",
    //                        },
    //                    },
    //                })
    //                .AddInMemoryClients(new List<Client>
    //                {
    //                    new()
    //                    {
    //                        ClientId = "client",
    //                        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
    //                        ClientSecrets = new List<Secret> {new("secret".Sha256())},
    //                        AllowedScopes = new List<string> { apiName, "openid", "offline_access" },
    //                        AccessTokenType = tokenType,
    //                        Enabled = true,
    //                        RequireClientSecret = false,
    //                    },
    //                })
    //                .AddTestUsers(new List<TestUser>
    //                {
    //                    user,
    //                });
    //        })
    //        .Configure(app =>
    //        {
    //            app.UseIdentityServer();
    //        })
    //        .Build();
    //    await _identityServerBuilder.StartAsync();
    //    await Steps.VerifyIdentityServerStarted(url);
    //}
    public void Dispose()
    {
        _servicebuilder?.Dispose();
        _steps.Dispose();

        //_identityServerBuilder?.Dispose();
    }
}
