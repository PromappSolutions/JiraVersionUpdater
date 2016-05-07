using System;

namespace JiraVersionUpdater
{
	public interface IJiraOptions
	{
		string JiraUri { get; set; }
		string Password { get; set; }
		string UserName { get; set; }
		Version FixVersion { get; }
		string AvailableFromVersion { get; set; }
		string ProjectKey { get; set; }
		string CustomFieldName { get; set; }
	}
}