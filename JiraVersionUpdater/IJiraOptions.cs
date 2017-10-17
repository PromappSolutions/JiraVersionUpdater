using System;

namespace JiraVersionUpdater
{
	public interface IJiraOptions
	{
		string JiraUri { get; set; }
		string Password { get; set; }
		string UserName { get; set; }
		string FixVersion { get; }
        string AvailableFromVersion { get; }

        Version FixVersionObj { get; }
	    Version AvailableFromVersionObj { get; }

        string ProjectKey { get; set; }
		string CustomFieldName { get; set; }
	}
}