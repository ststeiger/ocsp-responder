namespace OCSPGuardian
{

    public class SimpleOcspLogger
        : global::OcspResponder.Core.IOcspLogger
    {
        void OcspResponder.Core.IOcspLogger.Debug(string message)
        {
            System.Console.Write("DEBUG: ");
            System.Console.WriteLine(message);
        }

        void OcspResponder.Core.IOcspLogger.Error(string message)
        {
            System.Console.Write("ERROR: ");
            System.Console.WriteLine(message);
        }

        void OcspResponder.Core.IOcspLogger.Warn(string message)
        {
            System.Console.Write("WARN: ");
            System.Console.WriteLine(message);
        }
    }


}
