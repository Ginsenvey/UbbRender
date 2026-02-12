namespace UBBParser.Scanner;

public enum TokenType
{
    // 基础符号
    LeftBracket,    // [
    RightBracket,   // ]
    Slash,          // /
    Equal,          // =
    Comma,          // ,

    // 公式符号
    Dollar,         // $
    DoubleDollar,   // $$

    // 内容类
    TagName,        // b, url, img, ac01 等
    AttrValue,      // 属性值
    Text,           // 普通文本
    EOF             // 结束符
}