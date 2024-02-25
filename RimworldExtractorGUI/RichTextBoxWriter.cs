using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimworldExtractorInternal;

namespace RimworldExtractorGUI
{
    internal class RichTextBoxWriter : TextWriter
    {
        private readonly RichTextBox _richTextBox;
        private readonly StreamWriter _logFileWriter;
        private readonly object _lock = new object();
        public override Encoding Encoding { get; } = Encoding.UTF8;

        public RichTextBoxWriter(RichTextBox richTextBox)
        {
            this._richTextBox = richTextBox;
            this._logFileWriter = File.CreateText("log.txt");
        }
        public override void WriteLine(string? value)
        {
            if (value == null)
            {
                return;
            }

            lock (_lock)
            {
                _logFileWriter.WriteLine(value);
                _logFileWriter.Flush();


                var line = value + Environment.NewLine;
                if (_richTextBox.InvokeRequired)
                {
                    _richTextBox.Invoke(() =>
                    {
                        AppendToRichTextBox(line);
                    });
                }
                else
                {
                    AppendToRichTextBox(line);
                }
            }
        }

        private void AppendToRichTextBox(string line)
        {
            if (_richTextBox.Text.Length + line.Length > 327670)
            {
                _richTextBox.Clear(); // Clean up if text is too long
                _richTextBox.AppendText("Log clean-up done!" + Environment.NewLine);
            }

            _richTextBox.SelectionColor = line.Split(Log.Separator).First() switch
            {
                Log.PrefixError => Color.Red,
                Log.PrefixWarning => Color.Yellow,
                _ => Color.White
            };
            _richTextBox.AppendText(line);
            _richTextBox.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            lock (_lock)
            {
                _logFileWriter.Flush();
                _logFileWriter.Close();
                _logFileWriter.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
