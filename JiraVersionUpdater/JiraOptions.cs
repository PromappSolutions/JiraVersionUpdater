using System;

namespace JiraVersionUpdater
{
	internal class JiraOptions : IJiraOptions
	{
		public string JiraUri { get; set; }

		public string Password { get; set; }

		public string UserName { get; set; }

		public Version FixVersion { get; set; }

		public Version AvailableFromVersion { get; set; }

		public string ProjectKey { get; set; }
	}
}