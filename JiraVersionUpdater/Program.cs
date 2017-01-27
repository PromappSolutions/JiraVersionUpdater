using System;
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
			fluentCommandLineParser.Setup(f => f.FixVersionStr)
				.As('f', "fix")
				.Required()
				.WithDescription("The main version we want to apply the fix for, e.g. 5.6.0 (not 5.6.0.XX)");
			fluentCommandLineParser.Setup(f => f.AvailableFromVersionStr)
				.As('a', "available")
				.WithDescription("The version the tickets will be available from, e.g. 5.6.0.XX");
			fluentCommandLineParser.Setup(f => f.ProjectKey)
				.As('k', "project")
				.WithDescription("The project key to update the version for");
			fluentCommandLineParser.Setup(f => f.CustomFieldName)
				.As('c', "fieldName")
				.WithDescription("The name of the custom field to update e.g. customfield_XXX");

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
				fluentCommandLineParser.SetupHelp("?", "help").Callback(text => Console.WriteLine(text));
				Environment.Exit(-1);
			}
		}
	}
}
