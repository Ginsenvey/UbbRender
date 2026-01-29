# UBBRender
#### 起点
这里是一个用于解析UBB代码为XAML元素的可视化调试工具。
内部包含：
语法分割器；解析器；渲染器；AST呈现器。
#### 从源开始构建
1打开Visual Studio 2022.

2安装WinUI3所需的工作负载，包括Win10.0.19041 SDK.

3将“https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-Labs/nuget/v3/index.json”添加到Nuget源。

4克隆本仓库，并打开项目，运行生成，等待自动补全所有Nuget包。

5现在，你已经完成所有调试准备。

## UBB语法定义

### 终结符 (Terminals)

终结符是语法的最小单元，不可再分。
|终结符	|定义	|说明|
|:-|:-|:-
|L_BRACKET	|[	|左中括号|
|R_BRACKET	|]	|右中括号|
|SLASH	|/	|斜杠，用于闭合标签|
|EQUAL	|=	|等号，用于属性赋值|
|COMMA	|,	|逗号，用于多属性分隔|
|DOLLAR	|$	|用于行内公式|
|DBL_DOLLAR	|$$	|用于行间公式|
|TAG_NAME	|[a-zA-Z]+|标签名，如 `b`, `url`, `align` 等|
|EMOTION_PREFIX	|[ac,em,cc98,tb,ms,CC98] |特有的表情包前缀|
|DIGITS	|[0-9]+	|数字，用于表情序号或索引|
|ATTR_TEXT	|[^\]=,]+	|属性值内容（不含中括号、等号和逗号）|
|PLAIN_TEXT	|[^\[$]+	|普通文本内容（不含左括号和公式符）|


### 非终结符与生产规则 (Rules & Non-terminals)

这是语法的核心逻辑，通过递归定义描述了标签是如何嵌套和组合的。

规则 1：文档结构 (Root)

UBB_DOC ::= (ELEMENT | PLAIN_TEXT)*

    说明：整个 UBB 文档是由多个“元素”或“普通文本”组成的序列。

规则 2：元素分支 (Element)

ELEMENT ::= PAIR_TAG | UNARY_TAG | EMOTION_TAG | MATH_BLOCK

    说明：元素可以是成对标签（如粗体）、单标签（如换行）、表情或数学公式。

1. 成对标签 (Pair Tags)

PAIR_TAG ::= OPEN_TAG UBB_DOC CLOSE_TAG

    说明：这是 UBB 的精髓。标签中间递归引用了 UBB_DOC，这意味着标签内部可以无限嵌套其他标签（例如 [b][i]文字[/i][/b]）。

4. 开始标签与属性 (Tags with Attributes)

OPEN_TAG ::= L_BRACKET TAG_NAME ATTR_LIST? R_BRACKET ATTR_LIST ::= EQUAL ATTR_TEXT (COMMA ATTR_TEXT)*

    说明：开始标签可以带属性。属性支持 [tag=value] 形式，也支持 CC98 中常见的逗号分隔多属性，如 [upload=jpg,1]。

5. 闭合标签 (Close Tags)

CLOSE_TAG ::= L_BRACKET SLASH TAG_NAME R_BRACKET

    说明：必须以 [/ 开头并以 ] 结尾，标签名应与 OPEN_TAG 匹配（解析逻辑中处理）。

6. 单标签与特化标签 (Special Tags)

UNARY_TAG ::= L_BRACKET ("hr" | "br" | "upload" ATTR_LIST?) R_BRACKET EMOTION_TAG ::= L_BRACKET EMOTION_PREFIX DIGITS R_BRACKET

    说明：

        UNARY_TAG 处理无需闭合的标签（如横线）。

        EMOTION_TAG 专门处理 CC98 的表情，如 [ac01]，它们在语法上被视为不可分割的整体。

7. 数学公式 (Math)

MATH_BLOCK ::= (DOLLAR LATEX_TEXT DOLLAR) | (DBL_DOLLAR LATEX_TEXT DBL_DOLLAR)

    说明：识别 Latex 和
    Latex

    结构。注意公式内部的文本通常不进行 UBB 解析。

### 补充说明 (Contextual Notes)

在实际解析 CC98 的 UBB 代码时，有几个“潜规则”需要注意：

- 容错性：如果遇到 [b]未闭合，解析器通常将其退化为普通文本处理，而不是直接报错。
- 大小写不敏感：[URL] 和 [url] 在 CC98 中通常是等效的。
- 自动链接：即使没有 [url] 标签，纯文本中的`http://`也会被识别。
- 段落处理：CC98 会自动将连续换行转换为`<p>`或`<br/>`，这在语法定义之外的渲染阶段处理。