using System.Net;
using GLNetTools.Common;
namespace GLNetTools.Iptables;


public interface IFirewallManager
{
	public void UseSubNetwork(IPAddress networkAddress);

	public void Add(FirewallRule rule);

	public void Reset();

}
