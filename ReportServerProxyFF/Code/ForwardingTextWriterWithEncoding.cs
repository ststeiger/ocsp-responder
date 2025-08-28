
namespace ReportServerProxyFF
{


    internal class ForwardingTextWriterWithEncoding
        : System.IO.TextWriter
    {
        private readonly System.IO.TextWriter m_innerWriter;
        private readonly System.Text.Encoding m_encoding;

        public ForwardingTextWriterWithEncoding(
            System.IO.TextWriter innerWriter,
            System.Text.Encoding encoding
        )
        {
            this.m_innerWriter = innerWriter;
            this.m_encoding = encoding;
        }

        public override System.Text.Encoding Encoding
        {
            get { return this.m_encoding; }
        }

        public override void Write(char value)
        {
            this.m_innerWriter.Write(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            this.m_innerWriter.Write(buffer, index, count);
        }

        public override void Write(string value)
        {
            this.m_innerWriter.Write(value);
        }


    } // End Class TextWriterWithEncoding



}