using System;

namespace JiraVersionUpdater
{
	public interface IJiraOptions
	{
		string JiraUri { get; set; }
		string Password { get; set; }
		string UserName { get; set; }
		Version FixVersion { get; }
		string AvailableFromVersionStr { get; set; }
        Version AvailableFromVersion { get; }

        string ProjectKey { get; set; }
		string CustomFieldName { get; set; }
	}
}