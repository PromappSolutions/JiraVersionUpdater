using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using AnotherJiraRestClient;
using AnotherJiraRestClient.JiraModel;
using JetBrains.Annotations;
using JiraVersionUpdater.Promapp;
using NLog;
using NLogInjector;
using RestSharp;
using Version = System.Version;

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
				$"project={projectMeta.key} and statusCategory = Done and fixVersion = {_jiraOptions.FixVersion}";

			var client = new JiraClient(Account);
			Issues issues = client.GetIssuesByJql(allClosedTicketsWithoutAnAvailableVersion, 0, 500);

			if (!issues.issues.Any())
			{
				_logger.Info("No tickets found to update");
				return true;
			}
			AnotherJiraRestClient.JiraModel.Version addedVersion = AddOrGetExistingVersion(projectMeta);

			_logger.Info(
				$"Found <{issues.issues.Count}> issues for this release, will be updated to 'Available Version' <{addedVersion.name}>");

			var expando = new ExpandoObject();
			var asDict = (IDictionary<string, object>)expando;
			asDict.Add(_jiraOptions.CustomFieldName, addedVersion);

			var updateIssue = new
			{
				fields = expando
			};

			_logger.Info($"Found <{issues.issues.Count}> issues to process");
			
			foreach (var issue in issues.issues)
			{
				_logger.Info($"Processing <{issue.key}>");

				var request = new RestRequest
				{
					Resource = $"{ResourceUrls.IssueByKey(issue.key)}?fields={_jiraOptions.CustomFieldName}",
					Method = Method.GET
				};

				// TODO: custom logic to handle some version information specific to Promapp's needs
				var promappIssue = client.Execute<PromappReleaseIssue>(request, HttpStatusCode.OK);

				if (promappIssue.fields == null)
					throw new InvalidOperationException("Fields is empty, has the Jira API changed?");

				bool updateVersion = false;
				if (promappIssue.fields.customfield_11520 == null)
					updateVersion = true;
				else
				{
					Version customFieldAsVersion;
					if (!Version.TryParse(promappIssue.fields.customfield_11520.name, out customFieldAsVersion))
					{
						throw new InvalidOperationException($"Couldn't parse custom field value for ticket <{issue.key}> of <{promappIssue.fields.customfield_11520.name}> to a version");
					}

					// e.g. we have moved from dev->staging
					if (_jiraOptions.FixVersion >= Version.Parse("1.0.0.0") && customFieldAsVersion < Version.Parse("1.0.0.0"))
						updateVersion = true;
					else
					{
						_logger.Info($"Update issue <{issue.key}> with version <{customFieldAsVersion}>");
					}
				}

				if (updateVersion)
				{
					_logger.Info($"Update issue <{issue.key}> with version <{_jiraOptions.FixVersion}>");
					client.UpdateIssueFields(issue.key, updateIssue);
				}
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
				name = _jiraOptions.AvailableFromVersion,
				userStartDate = _dateTimeProvider.Now.ToString("dd/MMM/yyyy"),
				userReleaseDate = _dateTimeProvider.Now.ToString("dd/MMM/yyyy"),
				project = projectMeta.key,
				released = true,
			});

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