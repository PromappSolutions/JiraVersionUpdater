using System;
using System.Collections.Generic;
using System.Linq;
using AnotherJiraRestClient;
using AnotherJiraRestClient.JiraModel;
using JetBrains.Annotations;
using NLog;
using NLogInjector;

namespace JiraVersionUpdater
{
	internal class MyApplication : IApplication
	{
		private readonly IJiraOptions _jiraOptions;
		private readonly IDateTimeProvider _dateTimeProvider;

		[InjectLogger]
		private readonly ILogger _logger = NullLogger.Instance;

		public MyApplication(IJiraOptions jiraOptions, IDateTimeProvider dateTimeProvider)
		{
			_jiraOptions = jiraOptions;
			_dateTimeProvider = dateTimeProvider;
		}

		public bool Run()
		{
			_logger.Info("Updating project <{0}>", _jiraOptions.ProjectKey);
			// https://confluence.jetbrains.com/display/TCD65/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ReportingMessagesForBuildLog
			_logger.Info("##teamcity[progressStart 'Updating Jira tickets']");

			ProjectMeta projectMeta;
			if (!VerifyProjectKey(out projectMeta))
			{
				_logger.Error($"Project <{_jiraOptions.ProjectKey}> doesn't exist");
				return false;
			}

			bool success = UpdateResolvedTicketsToVersion(projectMeta);
			_logger.Info("##teamcity[progressFinish 'Updated Jira tickets']");
			return success;
		}

		private bool UpdateResolvedTicketsToVersion(ProjectMeta projectMeta)
		{
			_logger.Info("Getting all versions which have the fix version of <{0}>", _jiraOptions.FixVersion);
			string allClosedTicketsWithoutAnAvailableVersion =
				$"project={projectMeta.key} and \"Available From\" is EMPTY and statusCategory = Done and fixVersion = {_jiraOptions.FixVersion}";

			var client = new JiraClient(Account);
			Issues issues = client.GetIssuesByJql(allClosedTicketsWithoutAnAvailableVersion, 0, 500);

			if (!issues.issues.Any())
			{
				_logger.Info("No tickets found to update");
				return true;
			}
			AnotherJiraRestClient.JiraModel.Version addedVersion = AddOrGetExistingVersion(projectMeta);

			_logger.Info("Found <{0}> issues for this release, will be updated to 'Available Version' <{1}>", issues.issues.Count, addedVersion.name);
			foreach (var issue in issues.issues)
			{
				_logger.Info("Update issue <{0}>", issue.key);
				client.UpdateIssueFields(issue.key, addedVersion,);
			}
			return true;
		}

		private bool VerifyProjectKey(out ProjectMeta projectMeta)
		{
			var client = new JiraClient(Account);
			projectMeta = client.GetProjectMeta(_jiraOptions.ProjectKey);
			return projectMeta != null;
		}

		private IEnumerable<AnotherJiraRestClient.JiraModel.Version> GetVersions()
		{
			var client = new JiraClient(Account);
			return client.GetVersions(_jiraOptions.ProjectKey);
		}

		private AnotherJiraRestClient.JiraModel.Version AddOrGetExistingVersion(ProjectMeta projectMeta)
		{
			IEnumerable<AnotherJiraRestClient.JiraModel.Version> versions = GetVersions();
			if (versions.Any(v => v.name == _jiraOptions.AvailableFromVersion.ToString()))
			{
				_logger.Info("Version <{0}> already exists in Jira", _jiraOptions.AvailableFromVersion);
				return versions.Single(v => v.name == _jiraOptions.AvailableFromVersion.ToString());
			}

			// add the version
			_logger.Info("Adding version <{0}>", _jiraOptions.AvailableFromVersion);
			var client = new JiraClient(Account);
			var addedVersion = client.CreateVersion(new NewVersion
			{
				description = "Automatically added release version via TC on " + _dateTimeProvider.Now.ToShortDateString() + " " + _dateTimeProvider.Now.ToShortTimeString(),
				name = _jiraOptions.AvailableFromVersion.ToString(),
				userStartDate = _dateTimeProvider.Now.ToString("dd/MMM/yyyy"),
				userReleaseDate = _dateTimeProvider.Now.ToString("dd/MMM/yyyy"),
				project = projectMeta.key,
				released = true,
			});


			// TODO: Figure out hot to automatically mark version as released
			// due to a Jira REST issue, we need to mark it as released
			//var updated = client.UpdateVersion(new UpdateVersion
			//{
			//    //name = addedVersion.name,
			//    released = true,
			//    id = addedVersion.id,
			//    //project = projectMeta.key
			//});

			return addedVersion;
		}

		[NotNull]
		private JiraAccount Account => new JiraAccount
		{
			ServerUrl = _jiraOptions.JiraUri,
			User = _jiraOptions.UserName,
			Password = _jiraOptions.Password
		};
	}
}