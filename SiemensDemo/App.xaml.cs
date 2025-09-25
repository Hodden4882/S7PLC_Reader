using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Hosting;
using Owin;
using SiemensDemo.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
            // 在這裡手動建立 PlcService 的單例實例
            var plcService = new PlcService();

            // 建立 ViewModel，並將 plcService 傳入其建構子
            var vm = new MainViewModel(plcService);

            // 建立主視窗，並將 ViewModel 傳入
            var window = new MainWindow(vm);
            Current.MainWindow = window;
            window.Show();

            // 啟動 Owin Host，並將 plcService 實例傳遞給它
            StartOwinHost(plcService);

            base.OnStartup(e);
        }

        private void StartOwinHost(PlcService plcService)
        {
            // 配置 Web 伺服器的 URL
            string baseUri = "http://localhost:5000/";

            // 使用 Startup 類別來啟動 Web 主機
            // 透過 WebApp.Start 的 overload，我們可以傳遞自訂的配置委派
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

        // 這是我們需要定義的依賴解析器，用來告訴 OWIN 如何取得服務
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
                // 我們可以傳回同一個 scope，因為 PlcService 是單例
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
                // 如果沒有多個實例，我們就傳回空的集合
                return Enumerable.Empty<object>();
            }

            public void Dispose()
            {
            }
        }
    }
}

