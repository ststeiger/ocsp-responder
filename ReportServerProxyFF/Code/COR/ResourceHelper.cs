
namespace ReportServerProxyFF
{
    
    

    public static class ResourceHelper
    {
        /// <summary>
        /// Finds and reads an embedded resource file whose name ends with the given suffix (case-insensitive).
        /// </summary>
        /// <param name="suffix">The file name suffix to search for (e.g., "data.json").</param>
        /// <returns>The UTF-8 content of the embedded file, or null if not found.</returns>
        public static string ReadEmbeddedFileEndingWith(string suffix)
        {
            string ret = null;

            if (string.IsNullOrWhiteSpace(suffix))
                throw new System.ArgumentException("Suffix must not be null or whitespace.", nameof(suffix));

            // var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Reflection.Assembly assembly = typeof(ResourceHelper).Assembly;

            string[] resourceNames = assembly.GetManifestResourceNames();

            string matchingResourceName = null;
            foreach (string name in resourceNames)
            {
                if (name.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchingResourceName = name;
                    break; // Exit the loop once a match is found
                }
            }

            if (matchingResourceName == null)
                return null;

            using (System.IO.Stream stream = assembly.GetManifestResourceStream(matchingResourceName))
            {
                if (stream == null)
                    return null;

                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    ret = reader.ReadToEnd();
                }
                    
            }

            return ret;
        }
    }

}