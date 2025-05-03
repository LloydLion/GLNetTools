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
			configuration = await _configuration.AwaitForNewConfiguration(c => c.Version is null || c.Version.Value > query.WaitForOlderThan.Value, checkCurrent: true);
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


		public string[] Scopes { get; set; } = []; //Format: "Master:*", "GuestMachine:123"

		public string[] Modules { get; set; } = []; //Format: "Base", "Network"

		public DateTime? WaitForOlderThan { get; set; } = null;


		public bool IsModuleRequested(string moduleName)
		{
			return Modules.Any(s => s == moduleName || s == AllModules);
		}

		public bool IsScopeRequested(string typeName, object key, IConfigurationScopeKeySerializer keySerializer)
		{
			var splittedScopes = Scopes.Select(s => { var split = s.Split(ScopeTypeSeparator); return split; }).ToArray();
			return splittedScopes.Any(s =>
				(s[0] == AllScopes && (s.Length == 1 || s[1] == AllScopes)) ||
				(s[0] == typeName && (s.Length == 1 || s[1] == AllScopes)) ||
				(s.Length == 2 && s[0] == typeName && keySerializer.Compare(key, s[1]))
			);
		}
	}
}