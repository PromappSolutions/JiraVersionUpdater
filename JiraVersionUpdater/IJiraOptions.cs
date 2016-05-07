using System;

namespace JiraVersionUpdater
{
	public interface IJiraOptions
	{
		string JiraUri { get; set; }
		string Password { get; set; }
		string UserName { get; set; }
		Version FixVersion { get; set; }
		Version AvailableFromVersion { get; set; }
		string ProjectKey { get; set; }
	}
}