using robotManager.Products;
using System.Threading;

public class ToolBox
{
    public static void Restart()
    {
        new Thread(() =>
        {
            Products.ProductStop();
            Thread.Sleep(2000);
            Products.ProductStart();
        }).Start();
    }
}