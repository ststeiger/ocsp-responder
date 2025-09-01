
namespace TestHttpsProxy
{


    internal class Program
    {
        
        
        static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            await ProxyTest.RunAsync();

            System.Console.WriteLine("Hello, World!");

            return 0;
        } // End Task Main 


    } // End Class Program 


} // End Namespace 
