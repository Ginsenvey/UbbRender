namespace UBBParser.Scanner;

public class UBBScanner(string input)
{
    private readonly string _input = input ?? "";
    private int _pos = 0;
    private bool _inTag = false;
    private TokenType _lastType = TokenType.EOF; // 记录上一个 Token 类型以判断上下文

    public IEnumerable<Token> ScanTokens()
    {
        while (_pos < _input.Length)
        {
            Token token;
            if (_inTag)
            {
                token = ScanInTag();
            }
            else
            {
                char c = Peek();
                if (c == '[')
                {
                    _inTag = true;
                    token = new Token(TokenType.LeftBracket, Advance().ToString(), _pos - 1);
                }
                else if (c == '$')
                {
                    token = ScanMathDelimiter();
                }
                else
                {
                    token = ScanText();
                }
            }

            _lastType = token.Type;
            yield return token;
        }

        yield return new Token(TokenType.EOF, "", _pos);
    }

    private Token ScanInTag()
    {
        char c = Peek();

        switch (c)
        {
            case ']':
                _inTag = false;
                return new Token(TokenType.RightBracket, Advance().ToString(), _pos - 1);
            case ',':
                return new Token(TokenType.Comma, Advance().ToString(), _pos - 1);
            case '=':
                // 关键修复：只有在 TagName 之后，等号才是属性开始的分隔符
                if (_lastType == TokenType.TagName)
                {
                    return new Token(TokenType.Equal, Advance().ToString(), _pos - 1);
                }
                // 否则（例如在 URL 内部），它是属性内容的一部分，交给 ScanTagContent 处理
                return ScanTagContent();
            case '/':
                if (_lastType == TokenType.LeftBracket)
                {
                    return new Token(TokenType.Slash, Advance().ToString(), _pos - 1);
                }
                return ScanTagContent();
            default:
                return ScanTagContent();
        }
    }



    private Token ScanTagContent()
    {
        int start = _pos;
        while (_pos < _input.Length && !IsTagDelimiter(Peek()))
        {
            Advance();
        }

        string value = _input[start.._pos];

        // 核心逻辑：根据上一个 Token 判断当前内容的性质
        // 如果前面是 '[' 或 '[/'，则当前是标签名
        if (_lastType == TokenType.LeftBracket || _lastType == TokenType.Slash)
        {
            return new Token(TokenType.TagName, value, start);
        }

        // 否则（前面是 '=' 或 ','），视为属性值
        return new Token(TokenType.AttrValue, value, start);
    }

    private Token ScanText()
    {
        int start = _pos;
        while (_pos < _input.Length && Peek() != '[' && Peek() != '$')
        {
            Advance();
        }
        return new Token(TokenType.Text, _input[start.._pos], start);
    }

    private Token ScanMathDelimiter()
    {
        int start = _pos;
        Advance();
        if (Peek() == '$')
        {
            Advance();
            return new Token(TokenType.DoubleDollar, "$$", start);
        }
        return new Token(TokenType.Dollar, "$", start);
    }

    private char Peek() => _pos < _input.Length ? _input[_pos] : '\0';
    private char Advance() => _input[_pos++];

    // 修改判定逻辑：= 是否作为分隔符取决于当前上下文
    private bool IsTagDelimiter(char c)
    {
        // ] , 和 结束符 永远是分隔符
        if (c == ']' || c == ',' || c == '\0') return true;

        // 只有在寻找 TagName 的阶段，= 才是分隔符
        if (c == '=' && (_lastType == TokenType.LeftBracket || _lastType == TokenType.Slash))
        {
            return true;
        }

        return false;
    }
}