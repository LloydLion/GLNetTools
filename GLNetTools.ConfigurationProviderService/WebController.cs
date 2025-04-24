using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.JsonSerialization;
using Microsoft.AspNetCore.Mvc;

namespace GLNetTools.ConfigurationProviderService;

internal class WebController
{
    private readonly ConfigurationHolder _configuration;
    private readonly IJsonServiceConfigurationSerializer _serializer;


    public WebController(ConfigurationHolder configuration, IJsonServiceConfigurationSerializer serializer)
	{
        _configuration = configuration;
        _serializer = serializer;
    }


	public async Task<string> HandleConfigurationRequest([FromBody] QueryModel query)
	{
		ServiceConfiguration configuration;
		if (query.WaitForOlderThan is not null)
		{
			//configuration = await _configuration.AwaitForNewConfiguration(c => c.Version is null || c.Version.Value > query.WaitForOlderThan.Value, checkCurrent: true);
			do
			{
				configuration = _configuration.Configuration;
			}
			while ((configuration.Version is null || configuration.Version.Value > query.WaitForOlderThan.Value) == false);
		}
		else configuration = _configuration.Configuration;

		var requestedConfigurationPart = new ServiceConfiguration(configuration.Scopes
		.Where(scope =>
		{
			if (scope.WeakScopeType.Serializer is null)
				return false;
			return query.IsScopeRequested(scope.WeakScopeType.Name, scope.WeakKey, scope.WeakScopeType.Serializer);
		})
		.Select(scope =>
		{
			var filteredProjection = scope.Projections.Where(proj => query.IsModuleRequested(proj.Key)).ToDictionary(s => s.Key, s => s.Value);
			return ConfigurationScope.CreateWeak(scope.WeakScopeType, scope.WeakKey, filteredProjection);
		}).ToArray()) { Version = configuration.Version };

		return _serializer.Serialize(requestedConfigurationPart);
	}


	public class QueryModel
	{
		private const string AllModules = "*";
		private const string AllScopes = "*";
		private const string ScopeTypeSeparator = ":";


		public string[] Scopes { get; set; } = []; //Format: "Master", "GuestMachine:123"

		public string[] Modules { get; set; } = []; //Format: "Base", "Network"

		public DateTime? WaitForOlderThan { get; set; } = null;


		public bool IsModuleRequested(string moduleName)
		{
			return Modules.Any(s => s == moduleName || s == AllModules);
		}

		public bool IsScopeRequested(string typeName, object key, IConfigurationScopeKeySerializer keySerializer)
		{
			var SplittedScopes = Scopes.Select(s => { var split = s.Split(ScopeTypeSeparator); return (split[0], split[1]); }).ToArray();
			return SplittedScopes.Any(s =>
				(s.Item1 == AllScopes && s.Item2 == AllScopes) ||
				(s.Item1 == typeName && s.Item2 == AllScopes) ||
				(s.Item1 == typeName && keySerializer.Compare(key, s.Item2))
			);
		}
	}
}