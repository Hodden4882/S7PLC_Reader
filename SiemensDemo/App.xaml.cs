using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Hosting;
using Owin;
using SiemensDemo.Models;
using SiemensDemo.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Windows;

namespace SiemensDemo
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        private IDisposable _webHost;
        protected override void OnStartup(StartupEventArgs e)
        {
            Config _config = null;
            try
            {
                // 嘗試載入或生成配置
                _config = Config.LoadConfiguration();

                // 如果 LoadConfiguration() 成功回傳，則繼續執行程式
            }
            catch (Exception ex)
            {
                // 捕捉所有其他錯誤 (無法讀取、無法解析、無法創建檔案)
                MessageBox.Show($"設定檔發生錯誤：\n{ex.Message}",
                                "嚴重錯誤", MessageBoxButton.OK, MessageBoxImage.Error);

                // 無論什麼錯誤，只要無法取得有效配置，就關閉程式
                Environment.Exit(1);
                return;
            }

            var plcService = new PlcService();

            // 建立 ViewModel
            var vm = new MainViewModel(plcService);

            // 建立主視窗
            var window = new MainWindow(vm);
            Current.MainWindow = window;
            window.Show();

            // 啟動 Owin Host，並將 plcService 實例傳遞給它
            StartOwinHost(plcService, _config.ApiBaseUrl);

            base.OnStartup(e);
        }

        private void StartOwinHost(PlcService plcService, string baseUri)
        {
            _webHost = WebApp.Start(baseUri, appBuilder =>
            {
                var config = new HttpConfiguration();

                // 使用自訂的依賴解析器，並將 plcService 傳入
                config.DependencyResolver = new OwinDependencyResolver(plcService);

                config.MapHttpAttributeRoutes();
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );

                appBuilder.UseWebApi(config);
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 應用程式結束時，釋放 Web 主機
            _webHost?.Dispose();
            base.OnExit(e);
        }

        private class OwinDependencyResolver : IDependencyResolver
        {
            private readonly PlcService _plcService;

            // 讓建構子接收 PlcService 實例
            public OwinDependencyResolver(PlcService plcService)
            {
                _plcService = plcService;
            }

            public IDependencyScope BeginScope()
            {
                // 可以傳回同一個 scope，因為 PlcService 是單例
                return this;
            }

            public object GetService(Type serviceType)
            {
                // 如果請求的是 PlcService，我們就回傳我們有的單例實例
                if (serviceType == typeof(PlcService))
                {
                    return _plcService;
                }
                // 如果請求的是你的控制器，我們就手動建立它並傳入 PlcService
                if (serviceType == typeof(Controllers.PlcController))
                {
                    return new Controllers.PlcController(_plcService);
                }

                return null;
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {
                // 如果沒有多個實例，就傳回空的集合
                return Enumerable.Empty<object>();
            }

            public void Dispose()
            {
            }
        }
    }
}

