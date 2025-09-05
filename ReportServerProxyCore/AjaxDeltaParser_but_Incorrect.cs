namespace ReportServerProxyCore.working
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;


    public sealed class AjaxDelta
    {
        public int CommandId { get; set; }
        public List<string> Metadata { get; } = new List<string>();
        public string? Payload { get; set; }   // HTML/script/data
        public bool HadTrailingPipeAfterPayload { get; set; }
        public int? LengthTokenIndex { get; set; }
    }

    public static class AjaxDeltaParser
    {
        // Known Microsoft AJAX response types that indicate a payload follows
        private static readonly HashSet<string> PAYLOAD_TYPES = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "updatePanel",
            "scriptBlock",
            "hiddenField",
            "arrayDeclaration",
            "scriptStartupBlock",
            "expando",
            "onSubmit",
            "asyncPostBackError",
            "pageTitle",
            "focus",
            "dataItem"
        };



        public static List<AjaxDelta> Parse(string response)
        {
            var result = new List<AjaxDelta>();
            int index = 0;

            while (index < response.Length)
            {
                // Read command ID
                string commandId = ReadToken(response, ref index);
                if (string.IsNullOrEmpty(commandId))
                    break;

                var delta = new AjaxDelta
                {
                    CommandId = int.Parse(commandId, CultureInfo.InvariantCulture)
                };

                // Collect tokens until we find a length+type combination or reach end
                bool payloadDetected = false;

                while (index < response.Length && !payloadDetected)
                {
                    string token = ReadToken(response, ref index);
                    if (token == null)
                        break;

                    // Check if this token is a length followed by a known payload type
                    if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int payloadLength))
                    {
                        // Look ahead to see if next token is a payload type
                        int tempIndex = index;
                        string nextToken = ReadToken(response, ref tempIndex);

                        if (nextToken != null && PAYLOAD_TYPES.Contains(nextToken))
                        {
                            // This is indeed a length token
                            delta.LengthTokenIndex = delta.Metadata.Count;
                            delta.Metadata.Add(token);    // length
                            delta.Metadata.Add(nextToken); // type
                            index = tempIndex; // commit the lookahead

                            // Read additional metadata (e.g., panel ID) if present
                            string panelId = ReadToken(response, ref index);
                            if (panelId != null)
                            {
                                delta.Metadata.Add(panelId);
                            }

                            // Read payload of exactly payloadLength chars
                            if (payloadLength > 0 && index + payloadLength <= response.Length)
                            {
                                string payload = response.Substring(index, payloadLength);
                                delta.Payload = payload;
                                index += payloadLength;
                            }
                            payloadDetected = true;
                        }
                        else
                        {
                            // Not a length token, just regular metadata
                            delta.Metadata.Add(token);
                        }
                    }
                    else
                    {
                        // Not numeric, add as metadata
                        delta.Metadata.Add(token);
                    }
                }

                // Check for trailing pipe after payload
                if (index < response.Length && response[index] == '|')
                {
                    delta.HadTrailingPipeAfterPayload = true;
                    index++; // skip the pipe
                }

                result.Add(delta);

                // Skip over any additional separators/newlines
                while (index < response.Length && (response[index] == '|' || response[index] == '\n' || response[index] == '\r'))
                    index++;
            }

            return result;
        }

        private static string ReadToken(string text, ref int index)
        {
            if (index >= text.Length)
                return null;

            int start = index;
            while (index < text.Length && text[index] != '|')
            {
                index++;
            }

            string token = text.Substring(start, index - start);

            // Skip separator if present
            if (index < text.Length && text[index] == '|')
                index++;

            return token;
        }

        public static string Recombine(List<AjaxDelta> deltas)
        {
            var sb = new StringBuilder();

            foreach (var d in deltas)
            {
                // Update payload length if we have a payload
                if (d.Payload != null && d.LengthTokenIndex.HasValue)
                {
                    d.Metadata[d.LengthTokenIndex.Value] =
                        d.Payload.Length.ToString(CultureInfo.InvariantCulture);
                }

                // Write command ID
                sb.Append(d.CommandId.ToString(CultureInfo.InvariantCulture));
                sb.Append('|');

                // Write metadata tokens
                foreach (string metadata in d.Metadata)
                {
                    sb.Append(metadata);
                    sb.Append('|');
                }

                // Write payload
                if (d.Payload != null)
                {
                    sb.Append(d.Payload);
                }

                // Add trailing pipe if original had one
                if (d.HadTrailingPipeAfterPayload)
                {
                    sb.Append('|');
                }
            }

            return sb.ToString();
        }
    }
}
