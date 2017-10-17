using System;

namespace JiraVersionUpdater
{
	internal class JiraOptions : IJiraOptions
	{
		public string JiraUri { get; set; }

		public string Password { get; set; }

		public string UserName { get; set; }

		public string FixVersion { get; set; }
        
        public string AvailableFromVersion { get; set; }

        public Version FixVersionObj
        {
            get
            {
                FixVersion.TrySeparateVersionAndProject(out var version, out string _);
                return version;
            }
        }

	    public Version AvailableFromVersionObj
	    {
	        get
	        {
	            AvailableFromVersion.TrySeparateVersionAndProject(out var version, out string _);
	            return version;
	        }
	    }

        public string ProjectKey { get; set; }

		public string CustomFieldName { get; set; }
	}
}