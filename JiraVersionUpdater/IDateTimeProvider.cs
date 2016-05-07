using System;

namespace JiraVersionUpdater
{
	public interface IDateTimeProvider
	{
		DateTime Now { get; }

		DateTime Today { get; }

		DateTime UtcNow { get; }
	}
}