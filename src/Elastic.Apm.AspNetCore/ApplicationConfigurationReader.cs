﻿using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore
{
	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback.
	/// </summary>
	internal class ApplicationConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		internal const string Origin = "Configuration Provider";
		private LogLevel? _logLevel;

		internal static class Keys
		{
			internal const string LogLevelSubKey = "LogLevel";
			internal const string LogLevel = "ElasticApm:LogLevel";
			internal const string ServerUrls = "ElasticApm:ServerUrls";
			internal const string ServiceName = "ElasticApm:ServiceName";
			internal const string SecretToken = "ElasticApm:SecretToken";
			internal const string CaptureHeaders = "ElasticApm:CaptureHeaders";
			internal const string TransactionSampleRate = "ElasticApm:TransactionSampleRate";
			internal const string MetricsInterval = "ElasticApm:MetricsInterval";
		}

		private readonly IConfiguration _configuration;

		public ApplicationConfigurationReader(IConfiguration configuration, IApmLogger logger = null) : base(logger)
		{
			_configuration = configuration;
			_configuration.GetSection("ElasticApm")?.GetReloadToken().RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));
		}

		public LogLevel LogLevel
		{
			get
			{
				if (_logLevel.HasValue) return _logLevel.Value;

				_logLevel = ParseLogLevel(ReadFallBack(Keys.LogLevel, ConfigConsts.EnvVarNames.LogLevel));
				return _logLevel.Value;
			}
		}

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(ReadFallBack(Keys.ServerUrls, ConfigConsts.EnvVarNames.ServerUrls));

		public string ServiceName => ParseServiceName(ReadFallBack(Keys.ServiceName, ConfigConsts.EnvVarNames.ServiceName));

		public string SecretToken => ParseSecretToken(ReadFallBack(Keys.SecretToken, ConfigConsts.EnvVarNames.SecretToken));

		public bool CaptureHeaders => ParseCaptureHeaders(ReadFallBack(Keys.CaptureHeaders, ConfigConsts.EnvVarNames.CaptureHeaders));

		public double TransactionSampleRate => ParseTransactionSampleRate(ReadFallBack(Keys.TransactionSampleRate, ConfigConsts.EnvVarNames.TransactionSampleRate));

		public double MetricsIntervalInMillisecond  => ParseMetricsInterval(ReadFallBack(Keys.MetricsInterval , ConfigConsts.EnvVarNames.MetricsInterval));

		private ConfigurationKeyValue Read(string key) => Kv(key, _configuration[key], Origin);

		private ConfigurationKeyValue ReadFallBack(string key, string fallBackEnvVarName)
		{
			var primary = Read(key);
			return !string.IsNullOrWhiteSpace(primary.Value) ? primary : Kv(key, _configuration[fallBackEnvVarName], EnvironmentConfigurationReader.Origin);
		}

		private void ChangeCallback(object obj)
		{
			if (!(obj is IConfigurationSection section)) return;

			var newLogLevel = ParseLogLevel(Kv(Keys.LogLevelSubKey, section[Keys.LogLevelSubKey], Origin));
			if (_logLevel.HasValue && newLogLevel == _logLevel.Value) return;

			_logLevel = newLogLevel;
			Logger.Info()?.Log("Updated log level to {LogLevel}", newLogLevel);
		}
	}
}