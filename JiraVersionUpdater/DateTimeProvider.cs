using System;

namespace JiraVersionUpdater
{
	internal class DateTimeProvider : IDateTimeProvider
	{
		public DateTime Now
		{
			get
			{
				return DateTime.Now;
			}
		}

		public DateTime Today
		{
			get
			{
				return DateTime.Today;
			}
		}

		public DateTime UtcNow
		{
			get
			{
				return DateTime.UtcNow;
			}
		}
	}
}