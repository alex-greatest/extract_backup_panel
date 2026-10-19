using System.IO;
using FlexRt.Ui.Panel.Pictures;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using Radzen;

namespace FlexRt.Ui;

/// <summary>Главное окно: BlazorWebView с корневым компонентом <c>Components/Shell/Viewer.razor</c>.</summary>
public partial class MainWindow
{
    /// <summary>Картинки открытой панели — та же служба, что у <c>Viewer</c>: по ней окно отвечает на адреса картинок.</summary>
    private readonly PictureSelection _pictures;

    /// <summary>
    /// Создаёт окно: регистрирует службы BlazorWebView и Radzen (без них компоненты не запускаются) и картинки открытой
    /// панели (<see cref="PictureSelection"/>, одна на окно) в ресурсе <c>services</c>, на который ссылается разметка, и
    /// строит окно. В сборке Debug включены инструменты разработчика WebView (F12).
    /// </summary>
    public MainWindow()
    {
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddRadzenComponents();
        services.AddSingleton<PictureSelection>();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        var provider = services.BuildServiceProvider();
        _pictures = provider.GetRequiredService<PictureSelection>();
        Resources.Add("services", provider);
        InitializeComponent();
    }

    /// <summary>WebView2 готов: запросы по адресам картинок (<see cref="PictureHost.Filter"/>) отвечает окно.</summary>
    private void OnWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        var web = e.WebView.CoreWebView2;
        web.AddWebResourceRequestedFilter(PictureHost.Filter, CoreWebView2WebResourceContext.All);
        web.WebResourceRequested += (_, args) => ServePicture(web, args);
    }

    /// <summary>
    /// Ответить на запрос картинки байтами её файла (тип — по формату, кэш браузера — надолго: адрес с номером открытия
    /// панели не меняет содержимое); картинки нет у открытой панели — 404. Запросы самого BlazorWebView (другой адрес)
    /// не трогает.
    /// </summary>
    private void ServePicture(CoreWebView2 web, CoreWebView2WebResourceRequestedEventArgs args)
    {
        var url = args.Request.Uri;
        if (!url.StartsWith(PictureHost.Origin + "/", StringComparison.Ordinal))
        {
            return;
        }
        var picture = _pictures.Pictures.Find(url);
        args.Response = picture is null
            ? web.Environment.CreateWebResourceResponse(null, 404, "Not Found", "")
            : web.Environment.CreateWebResourceResponse(new MemoryStream(picture.Picture.Data, false), 200, "OK",
                $"Content-Type: {picture.MediaType}\r\nCache-Control: max-age=31536000, immutable");
    }
}
