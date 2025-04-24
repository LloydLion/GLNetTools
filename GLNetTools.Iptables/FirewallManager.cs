using System.Diagnostics;
using System.Net;
using GLNetTools.Common;

namespace GLNetTools.Iptables;

class FirewallManager(FirewallManager.Options options) : IFirewallManager
{
    private const string NatTable = "nat";
    private const string FilterTable = "filter";


    private readonly Options _options = options;
    private IPAddress? _networkAddress;


    public void UseSubNetwork(IPAddress networkAddress)
    {
        _networkAddress = networkAddress;
    }

    public void Add(FirewallRule rule)
    {
        if (_networkAddress is null)
            throw new InvalidOperationException("No network address set");

        if (rule.Protocol == FirewallProtocol.None)
            return;

        if (rule.Protocol == FirewallProtocol.Any)
        {
            Add(rule with { Protocol = FirewallProtocol.UDP });
            Add(rule with { Protocol = FirewallProtocol.TCP });
            return;
        }

        var arguments = new List<(string, object)>();

        if (rule.Type == FirewallRule.RuleType.Forward)
        {
            arguments.Add(("-t", FilterTable));
            arguments.Add(("-j", "ACCEPT"));
            arguments.Add(("-A", _options.ForwardChainName));

            if (rule.SourcePort != 0)
                arguments.Add(("--sport", rule.SourcePort));
            if (rule.DestinationPort != 0)
                arguments.Add(("--dport", rule.DestinationPort));
            if (rule.SourceMachineId != GuestMachineId.AnyMachine)
                arguments.Add(("-s", FormHostAddress(_networkAddress, rule.SourceMachineId)));
            if (rule.DestinationMachineId != GuestMachineId.AnyMachine)
                arguments.Add(("-d", FormHostAddress(_networkAddress, rule.DestinationMachineId)));
        }
        else if (rule.Type == FirewallRule.RuleType.DNat)
        {
            arguments.Add(("-t", NatTable));
            arguments.Add(("-A", _options.DNATChainName));
            arguments.Add(("--dport", rule.SourcePort));
            arguments.Add(("--to-destination", $"{FormHostAddress(_networkAddress, rule.DestinationMachineId)}:{rule.DestinationPort}"));
            arguments.Add(("-j", "DNAT"));

        }
        else throw new NotSupportedException($"Unknown RuleType {rule.Type}");

        arguments.Add(("-p", rule.Protocol == FirewallProtocol.TCP ? "tcp" : "udp"));

		PerformIptablesCall(arguments);
    }

    public void Reset()
    {
		var process = Process.Start(new ProcessStartInfo
        {
            FileName = _options.IptablesInitExecutablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        });

		if (process is null)
			throw new Exception("Failed to start iptables init process");

		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			string stdOut = process.StandardOutput.ReadToEnd();
			string stdErr = process.StandardError.ReadToEnd();
			throw new Exception($"""
				Iptables init process exited with {process.ExitCode} exit code.
				StdOut: {stdOut}.
				StdErr: {stdErr}
				""");
		}
    }

	private void PerformIptablesCall(IEnumerable<(string, object)> arguments)
	{
		var  planarArguments = string.Join(" ", arguments.Select(s => $"{s.Item1} {s.Item2}"));

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _options.IptablesExecutablePath,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
                Arguments = planarArguments
            }
        };

        process.Start();
        process.WaitForExit();

		if (process.ExitCode != 0)
		{
			string stdOut = process.StandardOutput.ReadToEnd();
			string stdErr = process.StandardError.ReadToEnd();
			throw new Exception($"""
				Iptables exited with {process.ExitCode} exit code.
				Command: {_options.IptablesExecutablePath} {planarArguments}.
				StdOut: {stdOut}.
				StdErr: {stdErr}
				""");
		}
	}

    private static IPAddress FormHostAddress(IPAddress networkAddress, GuestMachineId gm)
    {
        Span<byte> bytes = stackalloc byte[4];
        networkAddress.TryWriteBytes(bytes, out _);
        bytes[3] = gm.Id;
        return new IPAddress(bytes);
    }


    public class Options
    {
        //TODO: support of this
        //public string DNATCallTemplate { get; init; } = "[--dport {SourcePort}] -j DNAT [--to-destination {DestinationMachine}:{DestinationPort}]";
        //public string ForwardCallTemplate { get; init; } = "[--sport {SourcePort}] [--dport {DestinationPort}] [-s {SourceMachine}] [-d {DestinationMachine}] -j ACCEPT";

        public string IptablesExecutablePath { get; init; } = "/usr/sbin/iptables";

		public string IptablesInitExecutablePath { get; init; } = "./initFirewall.sh";

        public string ForwardChainName { get; init; } = "GM-TO-GM";
        
        public string DNATChainName { get; init; } = "GM-DNAT";
    }
}
