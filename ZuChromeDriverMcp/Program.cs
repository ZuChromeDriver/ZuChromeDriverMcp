namespace ZuChromeDriverMcp;

public static class Program
{
    [STAThread]
    public static int Main(string[] args) => WpfAppBootstrap.Run(args);
}
