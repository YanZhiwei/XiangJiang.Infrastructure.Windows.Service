using System;
using System.Diagnostics;
using Topshelf;
using Topshelf.HostConfigurators;
using XiangJiang.Core;
using XiangJiang.Infrastructure.Abstractions;
using XiangJiang.Infrastructure.Abstractions.Enums;
using XiangJiang.Infrastructure.Abstractions.Models;

namespace XiangJiang.Windows.Service
{
    /// <summary>
    ///     服务配置
    /// </summary>
    public sealed class ServiceConfigure<T> where T : IWindowsService, new()
    {
        /// <summary>
        ///     运行服务
        /// </summary>
        /// <param name="args">参数</param>
        /// <param name="option">ServiceOption</param>
        /// <param name="recoveryCallback">服务恢复选项callback</param>
        /// <param name="debug">是否启用调试模式</param>
        public static void Run(string[] args, ServiceOption option,
            Action<ServiceRecoveryConfigurator> recoveryCallback = null, bool debug = false)
        {
            Checker.Begin().NotNull(option, nameof(option))
                .NotNullOrEmpty(option.ServiceName, nameof(option.ServiceName));
            var rc = HostFactory.Run(host =>
            {
                var windowsService = new T();
                host.Service<IWindowsService>(service =>
                {
                    service.ConstructUsing(() => windowsService);
                    service.WhenStarted(s =>
                    {
                        if (debug)
                        {
                            if (!Debugger.IsAttached)
                                Debugger.Launch();
                            Debugger.Break();
                        }
                        s.Start(args, option);
                    });
                    service.WhenStopped(s => s.Stop());
                    service.WhenPaused(s => s.Paused());
                    service.WhenContinued(s => s.Continued());
                });
                SetRunAs(host, option);
                if (recoveryCallback != null)
                    host.EnableServiceRecovery(service => { recoveryCallback?.Invoke(service); });
                if (!string.IsNullOrEmpty(option.Description))
                    host.SetDescription(option.Description);
                if (!string.IsNullOrEmpty(option.DisplayName))
                    option.DisplayName = option.ServiceName;
                host.SetDisplayName(option.DisplayName);
                host.SetServiceName(option.ServiceName);
                SetStartPattern(host, option);
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }

        private static void SetStartPattern(HostConfigurator host, ServiceOption option)
        {
            switch (option.StartPattern)
            {
                case ServiceStartPattern.Automatically:
                    host.StartAutomatically();
                    break;
                case ServiceStartPattern.AutomaticallyDelayed:
                    host.StartAutomaticallyDelayed();
                    break;
                case ServiceStartPattern.Manually:
                    host.StartManually();
                    break;
            }
        }

        private static void SetRunAs(HostConfigurator host, ServiceOption option)
        {
            switch (option.RunAs)
            {
                case ServiceRunAs.LocalService:
                    host.RunAsLocalService();
                    break;
                case ServiceRunAs.LocalSystem:
                    host.RunAsLocalSystem();
                    break;
                case ServiceRunAs.NetworkService:
                    host.RunAsNetworkService();
                    break;
                case ServiceRunAs.Prompt:
                    host.RunAsPrompt();
                    break;
            }
        }
    }
}