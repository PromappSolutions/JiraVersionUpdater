using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Fclp;
using NLogInjector;

namespace JiraVersionUpdater
{
	class Program
	{
		static void Main(string[] args)
		{
			// create a generic parser for the ApplicationArguments type
			var fluentCommandLineParser = new FluentCommandLineParser<JiraOptions>();
			fluentCommandLineParser.Setup(f => f.JiraUri).As('j', "uri").Required().WithDescription("Url to access Jira");
			fluentCommandLineParser.Setup(f => f.Password)
				.As('p', "password")
				.Required()
				.WithDescription("Password to access TC");
			fluentCommandLineParser.Setup(f => f.UserName)
				.As('u', "username")
				.Required()
				.WithDescription("Username to access TC");
			fluentCommandLineParser.Setup(f => f.FixVersion)
				.As('f', "fix")
				.Required()
				.WithDescription("The main version we want to apply the fix for, e.g. 5.6.0 (not 5.6.0.XX)");
			fluentCommandLineParser.Setup(f => f.AvailableFromVersion)
				.As('a', "available")
				.WithDescription("The version the tickets will be available from, e.g. 5.6.0.XX");
			fluentCommandLineParser.Setup(f => f.ProjectKey)
				.As('k', "project")
				.WithDescription("The project key to update the version for");

			ICommandLineParserResult commandLineParserResult = fluentCommandLineParser.Parse(args);

			if (!commandLineParserResult.HasErrors)
			{
				var jiraOptions = fluentCommandLineParser.Object;

				var containerBuilder = new ContainerBuilder();
				containerBuilder.RegisterModule<NLogModule>();
				containerBuilder.RegisterInstance(jiraOptions).As<IJiraOptions>().SingleInstance();
				containerBuilder.RegisterType<MyApplication>().As<IApplication>().SingleInstance();
				containerBuilder.RegisterType<DateTimeProvider>().As<IDateTimeProvider>().SingleInstance();

				var container = containerBuilder.Build();
				var application = container.Resolve<IApplication>();
				bool success = application.Run();
				if (!success)
					Environment.Exit(-1);
			}
			else
			{
				Console.WriteLine(
					$"Usage: jiraVersionUpdater -[t|url] http://companyname.atlassian.net -[u|username] admin -[p|password] XXX");
				foreach (var commandLineParserError in commandLineParserResult.Errors)
				{
					Console.WriteLine($"{commandLineParserError.Option.Description} is a required parameter");
				}
				Environment.Exit(-1);
			}
		}
	}
}
