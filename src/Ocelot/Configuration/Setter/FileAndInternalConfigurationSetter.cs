using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;

namespace Ocelot.Configuration.Setter;

public class FileAndInternalConfigurationSetter : IFileConfigurationSetter
{
    private readonly IInternalConfigurationRepository _internalConfigRepo;
    private readonly IInternalConfigurationCreator _configCreator;
    private readonly IFileConfigurationRepository _repo;

    public FileAndInternalConfigurationSetter(
        IInternalConfigurationRepository configRepo,
        IInternalConfigurationCreator configCreator,
        IFileConfigurationRepository repo)
    {
        _internalConfigRepo = configRepo;
        _configCreator = configCreator;
        _repo = repo;
    }

    public async Task<Response> Set(FileConfiguration fileConfig)
    {
        var response = await _repo.Set(fileConfig);

        if (response.IsError)
        {
            return new ErrorResponse(response.Errors);
        }

        var config = await _configCreator.Create(fileConfig);

        if (!config.IsError)
        {
            _internalConfigRepo.AddOrReplace(config.Data);
        }

        return new ErrorResponse(config.Errors);
    }
}
