using Microsoft.Extensions.DependencyInjection;
using Radzen;

namespace FlexRt.Ui;

/// <summary>Главное окно: BlazorWebView с корневым компонентом <c>Components/Shell/Viewer.razor</c>.</summary>
public partial class MainWindow
{
    /// <summary>
    /// Создаёт окно: регистрирует службы BlazorWebView и Radzen (без них компоненты не запускаются) в ресурсе <c>services</c>,
    /// на который ссылается разметка, и строит окно. В сборке Debug включены инструменты разработчика WebView (F12).
    /// </summary>
    public MainWindow()
    {
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddRadzenComponents();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        Resources.Add("services", services.BuildServiceProvider());
        InitializeComponent();
    }
}
