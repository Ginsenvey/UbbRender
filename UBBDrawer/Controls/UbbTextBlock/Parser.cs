using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using UBBParser.Parser;
using UBBParser.Scanner;

namespace UbbRender.Common;


/// <summary>
/// 解析入口点
/// </summary>
public class UbbParser
{
    public static UbbDocument Parse(string ubbText)
    {
        // 词法分析
        var scanner = new UBBScanner(ubbText);
        var tokens = scanner.ScanTokens();
        // 语法分析
        var parser = new UBBParser.Parser.UBBParser(tokens);
        var document = parser.Parse();

        return document;
    }
}

