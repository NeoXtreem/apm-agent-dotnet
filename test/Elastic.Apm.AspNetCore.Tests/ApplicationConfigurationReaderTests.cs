﻿using System;
using System.IO;
using System.Linq;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="ApplicationConfigurationReader" /> class.
	/// It loads the JSON config files from the TestConfig folder.
	/// </summary>
	public class ApplicationConfigurationReaderTests
	{
		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_valid.json config file and passes it to the agent.
		/// Makes sure that the values from the config file are applied to the agent.
		/// </summary>
		[Fact]
		public void ReadValidConfigsFromAppSettingsJson()
		{
			var config = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"), new TestLogger());
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls.Single().Should().Be(new Uri("http://myServerFromTheConfigFile:8080"));
			config.ServiceName.Should().Be("My_Test_Application");
			config.ServiceVersion.Should().Be("2.1.0.5");
			config.CaptureHeaders.Should().BeFalse();
			config.TransactionSampleRate.Should().Be(0.456);
			config.CaptureBody.Should().Be(ConfigConsts.SupportedValues.CaptureBodyAll);
			var supportedContentTypes = new[] { "application/x-www-form-urlencoded*", "text/*", "application/json*", "application/xml*" };
			config.CaptureBodyContentTypes.Should().BeEquivalentTo(supportedContentTypes);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the LogLevel.
		/// </summary>
		[Fact]
		public void ReadInvalidLogLevelConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger);
			config.LogLevel.Should().Be(LogLevel.Error);
			logger.Lines.Should().NotBeEmpty();
			logger.Lines.Single().Should().ContainAll(
				$"{{{nameof(ApplicationConfigurationReader)}}}",
				"Failed parsing log level from",
				ApplicationConfigurationReader.Origin,
				ApplicationConfigurationReader.Keys.LogLevel,
				"Defaulting to ");

			config.CaptureHeaders.Should().BeTrue();
			config.TransactionSampleRate.Should().Be(1d);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the ServerUrls.
		/// </summary>
		[Fact]
		public void ReadInvalidServerUrlsConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger);
			config.LogLevel.Should().Be(LogLevel.Error);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines.Single().Should()
				.ContainAll(
					$"{{{nameof(ApplicationConfigurationReader)}}}",
					"Failed parsing log level from",
					ApplicationConfigurationReader.Origin,
					ApplicationConfigurationReader.Keys.LogLevel,
					"Defaulting to ",
					"DbeugMisspelled"
				);
		}

		/// <summary>
		/// Environment variables also can be the data source fetched by IConfiguration.
		/// This test makes sure that configs are applied to the agent when those are stored in env vars.
		/// </summary>
		[Fact]
		public void ReadConfigsFromEnvVarsViaIConfig()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.LogLevel, "Debug");
			const string serverUrl = "http://myServerFromEnvVar.com:1234";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServerUrls, serverUrl);
			const string serviceName = "MyServiceName123";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceName, serviceName);
			const string serviceVersion = "2.1.0.5";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceVersion, serviceVersion);
			const string secretToken = "SecretToken";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.SecretToken, secretToken);
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.CaptureHeaders, false.ToString());
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.TransactionSampleRate, "0.123");
			var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables().Build();

			var config = new ApplicationConfigurationReader(configBuilder, new TestLogger());
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls.Single().Should().Be(new Uri(serverUrl));
			config.ServiceName.Should().Be(serviceName);
			config.ServiceVersion.Should().Be(serviceVersion);
			config.SecretToken.Should().Be(secretToken);
			config.CaptureHeaders.Should().BeFalse();
			config.TransactionSampleRate.Should().Be(0.123);
		}

		/// <summary>
		/// Makes sure that <see cref="ApplicationConfigurationReader" /> logs in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			var testLogger = new TestLogger();
			var serverUrl = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), testLogger).ServerUrls.FirstOrDefault();
			serverUrl.Should().NotBeNull();
			testLogger.Lines.Should().NotBeEmpty();
		}

		internal static IConfiguration GetConfig(string path) => new ConfigurationBuilder().AddJsonFile(path).Build();
	}
}
