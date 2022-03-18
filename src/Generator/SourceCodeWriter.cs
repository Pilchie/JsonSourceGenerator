using System;
using System.CodeDom.Compiler;
using System.IO;

namespace Generator;

public sealed class SourceCodeWriter : IDisposable
{
    private static string StartBlockText => "{";
    private static string EndBlockText => "}";

    private readonly IndentedTextWriter _indentingStringWriter;

    public SourceCodeWriter()
    {
        var stringWriter = new StringWriter();
        _indentingStringWriter = new IndentedTextWriter(stringWriter, "    "); ;
    }

    public void WriteLine(string s)
    {
        _indentingStringWriter.WriteLine(s);
    }

    public IDisposable WriteBlock(string statement)
    {
        WriteLine(statement);
        return BeginWriteBlock();
    }

    public IDisposable BeginWriteBlock(string statement = "")
    {
        if (StartBlockText is not null)
        {
            _indentingStringWriter.WriteLine(StartBlockText);
        }

        return IndentAndWriteLineAtEndOfIndent(EndBlockText + statement);
    }

    public void WriteNewLine()
    {
        var currentIndentation = _indentingStringWriter.Indent;
        _indentingStringWriter.Indent = 0;
        _indentingStringWriter.WriteLine();
        _indentingStringWriter.Indent = currentIndentation;
    }

    public IDisposable Indent()
    {
        _indentingStringWriter.Indent++;
        return new IndentDisposable(_indentingStringWriter);
    }

    public IDisposable IndentAndWriteLineAtEndOfIndent(string s)
    {
        _indentingStringWriter.Indent++;
        return new IndentDisposable(_indentingStringWriter, s);
    }

    public void Dispose()
    {
        _indentingStringWriter.Dispose();
    }

    public override string ToString()
    {
        return _indentingStringWriter.InnerWriter.ToString() ?? "";
    }

    private class IndentDisposable : IDisposable
    {
        private readonly IndentedTextWriter _indentingStringWriter;
        private readonly string? _line;

        public IndentDisposable(IndentedTextWriter indentingStringWriter, string? line = null)
        {
            _indentingStringWriter = indentingStringWriter;
            _line = line;
        }

        public void Dispose()
        {
            _indentingStringWriter.Indent--;
            if (_line is not null)
            {
                _indentingStringWriter.WriteLine(_line);
            }
        }
    }
}