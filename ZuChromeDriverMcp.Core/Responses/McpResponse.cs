using System.Text;
using ModelContextProtocol.Protocol;

namespace ZuChromeDriverMcp.Core.Responses;

public sealed class McpResponse
{
    readonly StringBuilder _text = new();
    string _errorMessage = null!;

    public void AppendLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;

        if (_text.Length > 0)
            _text.AppendLine();

        _text.Append(line);
    }

    public void SetError(Exception exception)
    {
        _errorMessage = exception?.Message ?? "Unknown error.";
    }

    public void SetError(string message)
    {
        _errorMessage = message;
    }

    public bool IsError => !string.IsNullOrEmpty(_errorMessage);

    public string GetText()
    {
        return _text.ToString();
    }

    public CallToolResult ToCallToolResult()
    {
        if (IsError)
        {
            var text = GetText();
            if (!string.IsNullOrEmpty(text))
                text += Environment.NewLine + _errorMessage;
            else
                text = _errorMessage;

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = text }],
                IsError = true,
            };
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = GetText() }],
            IsError = false,
        };
    }
}
