
namespace TestHttpProxy
{


    internal class Program
    {


        static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            await Level4HttpHostProxy.Test();

            System.Console.WriteLine(string.Join(" ", args));
            return 0;
        } // End Task Main 


    } // End Class Program 


} // End Namespace 
