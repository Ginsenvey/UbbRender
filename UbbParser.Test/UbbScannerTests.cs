
using UBBParser.Scanner;

namespace UbbParser.Test
{
    [TestClass]
    public class UbbScannerTests
    {
        // 辅助方法：将扫描结果转为列表并移除最后的 EOF 方便对比
        private List<Token> GetTokens(string input)
        {
            var scanner = new UBBScanner(input);
            return scanner.ScanTokens().Where(t => t.Type != TokenType.EOF).ToList();
        }

        #region 1. 基础标签测试 (Basic Tags)

        [TestMethod]
        public void Test_SimpleTag_Bold()
        {
            var tokens = GetTokens("[b]Bold[/b]");

            // 预期结构: [ (LB), b (Value), ] (RB), Bold (Text), [ (LB), / (Slash), b (Value), ] (RB)
            Assert.AreEqual(8, tokens.Count);
            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual("b", tokens[1].Value);
            Assert.AreEqual(TokenType.Text, tokens[3].Type);
            Assert.AreEqual(TokenType.Slash, tokens[5].Type);
        }

        [TestMethod]
        public void Test_EmotionTag_CC98()
        {
            var tokens = GetTokens("[ac01][em22]");

            Assert.AreEqual(6, tokens.Count);
            Assert.AreEqual("ac01", tokens[1].Value);
            Assert.AreEqual("em22", tokens[4].Value);
        }

        #endregion

        #region 2. 属性测试 (Attributes)

        [TestMethod]
        public void Test_TagWithSingleAttribute()
        {
            var tokens = GetTokens("[size=5]Text[/size]");

            // [ size = 5 ] ...
            Assert.AreEqual(TokenType.TagName, tokens[1].Type); // 实际上目前 Scanner 逻辑里是 AttrValue，Parsing 阶段会纠正
            Assert.AreEqual(TokenType.Equal, tokens[2].Type);
            Assert.AreEqual("5", tokens[3].Value);
        }

        [TestMethod]
        public void Test_TagWithMultipleAttributes_Upload()
        {
            // 测试 CC98 典型的 upload 标签: [upload=jpg,1]
            var tokens = GetTokens("[upload=jpg,1]");

            Assert.AreEqual(TokenType.Equal, tokens[2].Type);
            Assert.AreEqual("jpg", tokens[3].Value);
            Assert.AreEqual(TokenType.Comma, tokens[4].Type);
            Assert.AreEqual("1", tokens[5].Value);
        }

        [TestMethod]
        public void Test_UrlWithSpecialChars()
        {
            // 测试 URL 内部包含特殊字符的情况
            var tokens = GetTokens("[url=http://cc98.org/index.asp?id=1]Link[/url]");

            // 关键点在于 = 之后的内容直到 ] 为止都应被视为 AttrValue
            var attrToken = tokens.First(t => t.Value.Contains("http"));
            Assert.AreEqual("http://cc98.org/index.asp?id=1", attrToken.Value);
        }

        [TestMethod]
        public void Test_Distinguish_TagName_And_AttrValue()
        {
            // 格式：[TagName=AttrValue,AttrValue]
            var tokens = GetTokens("[url=http://cc98.org,1]");

            Assert.AreEqual(TokenType.TagName, tokens[1].Type);
            Assert.AreEqual("url", tokens[1].Value);

            Assert.AreEqual(TokenType.Equal, tokens[2].Type);

            Assert.AreEqual(TokenType.AttrValue, tokens[3].Type);
            Assert.AreEqual("http://cc98.org", tokens[3].Value);

            Assert.AreEqual(TokenType.Comma, tokens[4].Type);

            Assert.AreEqual(TokenType.AttrValue, tokens[5].Type);
            Assert.AreEqual("1", tokens[5].Value);
        }

        [TestMethod]
        public void Test_ClosingTag_Is_TagName()
        {
            // 格式：[/TagName]
            var tokens = GetTokens("[/img]");

            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual(TokenType.Slash, tokens[1].Type);
            Assert.AreEqual(TokenType.TagName, tokens[2].Type);
            Assert.AreEqual("img", tokens[2].Value);
        }

        [TestMethod]
        public void Test_Emotion_Is_TagName()
        {
            // CC98表情 [ac01] 按照规则，紧跟在 [ 后，应识别为 TagName
            var tokens = GetTokens("[ac01]");

            Assert.AreEqual(TokenType.TagName, tokens[1].Type);
            Assert.AreEqual("ac01", tokens[1].Value);
        }

        [TestMethod]
        public void Test_Mixed_Content()
        {
            var tokens = GetTokens("Text[b]Bold[/b]");

            // "Text"
            Assert.AreEqual(TokenType.Text, tokens[0].Type);
            // "[", "b", "]"
            Assert.AreEqual(TokenType.TagName, tokens[2].Type);
            // "Bold"
            Assert.AreEqual(TokenType.Text, tokens[4].Type);
            // "[", "/", "b", "]"
            Assert.AreEqual(TokenType.TagName, tokens[7].Type);
        }

        [TestMethod]
        public void Test_Abnormal_NoValueAfterEqual()
        {
            // [size=]
            var tokens = GetTokens("[size=]");

            Assert.AreEqual(TokenType.TagName, tokens[1].Type);
            Assert.AreEqual(TokenType.Equal, tokens[2].Type);
            Assert.AreEqual(TokenType.RightBracket, tokens[3].Type);
            // 确保没有生成空的 AttrValue
        }
        #endregion

        #region 3. 数学公式测试 (Math)

        [TestMethod]
        public void Test_Math_InlineAndBlock()
        {
            var tokens = GetTokens("$x+y$ and $$E=mc^2$$");

            Assert.AreEqual(TokenType.Dollar, tokens[0].Type);
            Assert.AreEqual(TokenType.Text, tokens[1].Type); //x+y
            Assert.AreEqual(TokenType.Dollar, tokens[2].Type);
            Assert.AreEqual(TokenType.Text, tokens[3].Type); // " and "
            Assert.AreEqual(TokenType.DoubleDollar, tokens[4].Type);
            Assert.AreEqual(TokenType.Text, tokens[5].Type); // e=mc^2
            Assert.AreEqual(TokenType.DoubleDollar, tokens[4].Type);
        }

        #endregion

        #region 4. 异常与边界情况 (Abnormal Cases)

        [TestMethod]
        public void Test_UnclosedBracket_ShouldBeText()
        {
            // 只有左括号没有右括号的情况
            // 根据当前 Scanner 逻辑，它会进入 InTag 模式直到字符串结束或遇到下一个 ]
            var tokens = GetTokens("Hello [b PlainText");

            Assert.IsTrue(tokens.Any(t => t.Value == "b PlainText"));
        }

        [TestMethod]
        public void Test_EmptyInput()
        {
            var scanner = new UBBScanner("");
            var tokens = scanner.ScanTokens().ToList();

            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.EOF, tokens[0].Type);
        }

        [TestMethod]
        public void Test_NestedBrackets_Illegal()
        {
            // 畸形输入：[[b]]
            var tokens = GetTokens("[[b]]");

            // 第一个 [ 令 _inTag = true
            // 第二个 [ 在 InTag 模式下被 ScanTagContent 识别为 AttrValue
            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual("[b", tokens[1].Value);
            Assert.AreEqual(TokenType.RightBracket, tokens[2].Type);
        }

        [TestMethod]
        public void Test_Attribute_MissingValue()
        {
            // [size=]
            var tokens = GetTokens("[size=]");

            Assert.AreEqual(TokenType.Equal, tokens[2].Type);
            Assert.AreEqual(TokenType.RightBracket, tokens[3].Type);
            // 确保中间没有空的 AttrValue 被挤出来，或者根据需求定义其行为
        }
        [TestMethod]
        public void Test_Text_With_RightBracket()
        {
            var tokens = GetTokens("[size]this is a test that contains a right bracket] and a slach/,[/size]");
            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual(TokenType.TagName, tokens[1].Type);
            Assert.AreEqual(TokenType.RightBracket, tokens[2].Type);
            Assert.AreEqual(TokenType.Text, tokens[3].Type);
            Assert.AreEqual("this is a test that contains a right bracket] and a slach/,", tokens[3].Value);
            Assert.AreEqual(TokenType.LeftBracket, tokens[4].Type);
            Assert.AreEqual(TokenType.Slash, tokens[5].Type);
            Assert.AreEqual(TokenType.TagName, tokens[6].Type);
            Assert.AreEqual(TokenType.RightBracket, tokens[7].Type);
        }
        #endregion
    }
}