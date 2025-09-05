
namespace ReportServerProxyCore
{


    public sealed class AjaxDelta
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Content { get; set; }
    } // End Class AjaxDelta 


    public static class AjaxDeltaParser
    {


        public static System.Collections.Generic.List<AjaxDelta> Parse(string response)
        {
            System.Collections.Generic.List<AjaxDelta> result = new System.Collections.Generic.List<AjaxDelta>();
            int index = 0;
            string data = response;

            while (index < data.Length)
            {
                // Find next pipe separator for length
                int pipeIndex = data.IndexOf('|', index);
                if (pipeIndex == -1)
                    break;

                // Parse length as integer
                string lengthStr = data.Substring(index, pipeIndex - index);
                if (!int.TryParse(
                    lengthStr, 
                    System.Globalization.NumberStyles.None, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    out int length)
                )
                    break;

                index = pipeIndex + 1;

                // Find next pipe separator for type
                pipeIndex = data.IndexOf('|', index);
                if (pipeIndex == -1)
                    break;

                string type = data.Substring(index, pipeIndex - index);
                index = pipeIndex + 1;

                // Find next pipe separator for id
                pipeIndex = data.IndexOf('|', index);
                if (pipeIndex == -1)
                    break;

                string id = data.Substring(index, pipeIndex - index);
                index = pipeIndex + 1;

                // Check if we have enough characters for the content
                if (index + length >= data.Length)
                    break;

                // Extract content of specified length
                string content = data.Substring(index, length);
                index += length;

                // Expect a pipe separator after content
                if (index >= data.Length || data[index] != '|')
                    break;

                index++; // Skip the pipe

                // Add the parsed delta
                result.Add(new AjaxDelta
                {
                    Type = type,
                    Id = id,
                    Content = content
                });
            } // Whend 

            return result;
        } // End Function Parse 


        public static string Recombine(System.Collections.Generic.List<AjaxDelta> deltas)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (AjaxDelta delta in deltas)
            {
                // Write: length|type|id|content|
                sb.Append(delta.Content.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append('|');
                sb.Append(delta.Type);
                sb.Append('|');
                sb.Append(delta.Id);
                sb.Append('|');
                sb.Append(delta.Content);
                sb.Append('|');
            } // Next delta 

            return sb.ToString();
        } // End Function Recombine 


    } // End Class AjaxDeltaParser 


} // End Namespace 
