﻿using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Provider.Eureka;
using Steeltoe.Common.Discovery;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class EurekaServiceDiscoveryTests : Steps
{
    private readonly List<IServiceInstance> _eurekaInstances;
    private readonly ServiceHandler _serviceHandler;
    private readonly ServiceHandler _eurekaHandler;

    public EurekaServiceDiscoveryTests()
    {
        _serviceHandler = new ServiceHandler();
        _eurekaHandler = new ServiceHandler();
        _eurekaInstances = new List<IServiceInstance>();
    }

    [Theory]
    [Trait("Feat", "262")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_use_eureka_service_discovery_and_make_request(bool dotnetRunningInContainer)
    {
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", dotnetRunningInContainer.ToString());
        var serviceName = "product";
        var eurekaPort = 8761;
        var port = PortFinder.GetRandomPort();
        var downstreamServiceOneUrl = DownstreamUrl(port);
        var fakeEurekaServiceDiscoveryUrl = DownstreamUrl(eurekaPort);

        var instanceOne = new FakeEurekaService(serviceName, "localhost", port, false,
            new Uri(downstreamServiceOneUrl), new Dictionary<string, string>());

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = Uri.UriSchemeHttp,
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new() { HttpMethods.Get },
                    ServiceName = serviceName,
                    LoadBalancerOptions = new() { Type = nameof(LeastConnection) },
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new()
                {
                    Type = nameof(Eureka),
                },
            },
        };

        GivenEurekaProductServiceOneIsRunning(downstreamServiceOneUrl, HttpStatusCode.OK);
        GivenThereIsAFakeEurekaServiceDiscoveryProvider(fakeEurekaServiceDiscoveryUrl, serviceName);
        GivenTheServicesAreRegisteredWithEureka(instanceOne);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithEureka();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe(nameof(Should_use_eureka_service_discovery_and_make_request));
    }

    private void GivenTheServicesAreRegisteredWithEureka(params IServiceInstance[] serviceInstances)
    {
        foreach (var instance in serviceInstances)
        {
            _eurekaInstances.Add(instance);
        }
    }

    private void GivenThereIsAFakeEurekaServiceDiscoveryProvider(string url, string serviceName)
    {
        _eurekaHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            if (context.Request.Path.Value == "/eureka/apps/")
            {
                var apps = new List<Application>();

                foreach (var serviceInstance in _eurekaInstances)
                {
                    var a = new Application
                    {
                        name = serviceName,
                        instance = new List<Instance>
                        {
                            new()
                            {
                                instanceId = $"{serviceInstance.Host}:{serviceInstance}",
                                hostName = serviceInstance.Host,
                                app = serviceName,
                                ipAddr = "127.0.0.1",
                                status = "UP",
                                overriddenstatus = "UNKNOWN",
                                port = new Port {value = serviceInstance.Port, enabled = "true"},
                                securePort = new SecurePort {value = serviceInstance.Port, enabled = "true"},
                                countryId = 1,
                                dataCenterInfo = new DataCenterInfo {value = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", name = "MyOwn"},
                                leaseInfo = new LeaseInfo
                                {
                                    renewalIntervalInSecs = 30,
                                    durationInSecs = 90,
                                    registrationTimestamp = 1457714988223,
                                    lastRenewalTimestamp= 1457716158319,
                                    evictionTimestamp = 0,
                                    serviceUpTimestamp = 1457714988223,
                                },
                                metadata = new()
                                {
                                    value = "java.util.Collections$EmptyMap",
                                },
                                homePageUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                statusPageUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                healthCheckUrl = $"{serviceInstance.Host}:{serviceInstance.Port}",
                                vipAddress = serviceName,
                                isCoordinatingDiscoveryServer = "false",
                                lastUpdatedTimestamp = "1457714988223",
                                lastDirtyTimestamp = "1457714988172",
                                actionType = "ADDED",
                            },
                        },
                    };

                    apps.Add(a);
                }

                var applications = new EurekaApplications
                {
                    applications = new Applications
                    {
                        application = apps,
                        apps__hashcode = "UP_1_",
                        versions__delta = "1",
                    },
                };

                var json = JsonConvert.SerializeObject(applications);
                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
            }
        });
    }

    private void GivenEurekaProductServiceOneIsRunning(string baseUrl, HttpStatusCode statusCode, [CallerMemberName] string responseBody = null)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
        {
            try
            {
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(responseBody);
            }
            catch (Exception exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(exception.StackTrace);
            }
        });
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        _eurekaHandler?.Dispose();
        base.Dispose();
    }
}

public class FakeEurekaService : IServiceInstance
{
    public FakeEurekaService(string serviceId, string host, int port, bool isSecure, Uri uri, IDictionary<string, string> metadata)
    {
        ServiceId = serviceId;
        Host = host;
        Port = port;
        IsSecure = isSecure;
        Uri = uri;
        Metadata = metadata;
    }

    public string ServiceId { get; }
    public string Host { get; }
    public int Port { get; }
    public bool IsSecure { get; }
    public Uri Uri { get; }
    public IDictionary<string, string> Metadata { get; }
}

public class Port
{
    [JsonProperty("$")]
    public int value { get; set; }

    [JsonProperty("@enabled")]
    public string enabled { get; set; }
}

public class SecurePort
{
    [JsonProperty("$")]
    public int value { get; set; }

    [JsonProperty("@enabled")]
    public string enabled { get; set; }
}

public class DataCenterInfo
{
    [JsonProperty("@class")]
    public string value { get; set; }

    public string name { get; set; }
}

public class LeaseInfo
{
    public int renewalIntervalInSecs { get; set; }

    public int durationInSecs { get; set; }

    public long registrationTimestamp { get; set; }

    public long lastRenewalTimestamp { get; set; }

    public int evictionTimestamp { get; set; }

    public long serviceUpTimestamp { get; set; }
}

public class ValueMetadata
{
    [JsonProperty("@class")]
    public string value { get; set; }
}

public class Instance
{
    public string instanceId { get; set; }
    public string hostName { get; set; }
    public string app { get; set; }
    public string ipAddr { get; set; }
    public string status { get; set; }
    public string overriddenstatus { get; set; }
    public Port port { get; set; }
    public SecurePort securePort { get; set; }
    public int countryId { get; set; }
    public DataCenterInfo dataCenterInfo { get; set; }
    public LeaseInfo leaseInfo { get; set; }
    public ValueMetadata metadata { get; set; }
    public string homePageUrl { get; set; }
    public string statusPageUrl { get; set; }
    public string healthCheckUrl { get; set; }
    public string vipAddress { get; set; }
    public string isCoordinatingDiscoveryServer { get; set; }
    public string lastUpdatedTimestamp { get; set; }
    public string lastDirtyTimestamp { get; set; }
    public string actionType { get; set; }
}

public class Application
{
    public string name { get; set; }
    public List<Instance> instance { get; set; }
}

public class Applications
{
    public string versions__delta { get; set; }
    public string apps__hashcode { get; set; }
    public List<Application> application { get; set; }
}

public class EurekaApplications
{
    public Applications applications { get; set; }
}
