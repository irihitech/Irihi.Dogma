using Irihi.Dogma.Controls;
using Irihi.Dogma.Controls.Lexers;
using Xunit;

namespace Irihi.Dogma.Tests;

public class CSharpTokenClassifierTests
{
    private static IReadOnlyList<CodeToken> Lex(string code) => CSharpLexer.Tokenize(code);

    private static IReadOnlyList<CodeToken> Classify(string code) => CSharpTokenClassifier.Classify(Lex(code));

    private static void AssertRoundTripUnchanged(string code)
    {
        Assert.Equal(string.Concat(Lex(code).Select(t => t.Text)), string.Concat(Classify(code).Select(t => t.Text)));
    }

    private static void AssertTypeNamed(string code, params string[] expectedTypeNames)
    {
        var types = Classify(code).Where(t => t.Kind == TokenKind.Type).Select(t => t.Text).ToList();
        Assert.Equal(expectedTypeNames, types);
    }

    [Fact]
    public void Class_Declaration_Name_Is_Type()
    {
        AssertTypeNamed("public sealed class Greeter { }", "Greeter");
        AssertRoundTripUnchanged("public sealed class Greeter { }");
    }

    [Fact]
    public void Record_Interface_Struct_Enum_Names_Are_Types()
    {
        AssertTypeNamed("record Point(int X, int Y);", "Point");
        AssertTypeNamed("interface IThing { }", "IThing");
        AssertTypeNamed("struct Vec3 { }", "Vec3");
        AssertTypeNamed("enum Color { Red, Green }", "Color");
    }

    [Fact]
    public void Delegate_Name_Is_Type_Even_With_Void_Return()
    {
        AssertTypeNamed("delegate void Handler(object sender);", "Handler");
        AssertTypeNamed("delegate int Comparator<T>(T a, T b);", "Comparator");
    }

    [Fact]
    public void New_Keyword_Target_Is_Type()
    {
        AssertTypeNamed("var x = new Foo();", "Foo");
        AssertTypeNamed("var list = new List<string>();", "List");
        AssertTypeNamed("var p = new Point(1, 2);", "Point");
    }

    [Fact]
    public void Typeof_Parameter_Is_Type()
    {
        AssertTypeNamed("var t = typeof(Foo);", "Foo");
    }

    [Fact]
    public void Nameof_Upper_Is_Type_Lower_Is_Not()
    {
        AssertTypeNamed("var t = nameof(Foo);", "Foo");
        // 小写参数（变量/成员）不应误判为类型
        var tokens = Classify("var t = nameof(localVar);");
        Assert.DoesNotContain(tokens, t => t.Kind == TokenKind.Type);
    }

    [Fact]
    public void Local_Variables_And_Method_Calls_Are_Not_Types()
    {
        var code = """
            var foo = new Foo();
            foo.DoWork();
            var count = foo.Count;
            """;
        AssertRoundTripUnchanged(code);

        var types = Classify(code).Where(t => t.Kind == TokenKind.Type).Select(t => t.Text).ToList();
        Assert.Equal(new[] { "Foo" }, types);
    }

    [Fact]
    public void Usings_And_Namespaces_Not_Affected()
    {
        AssertTypeNamed("using System;", System.Array.Empty<string>());
        AssertTypeNamed("namespace Irihi.Dogma.Demo;", System.Array.Empty<string>());
    }

    [Fact]
    public void Keywords_Like_Class_Inside_String_Not_Affected()
    {
        var code = "var s = \"class Foo\";";
        AssertRoundTripUnchanged(code);
        AssertTypeNamed(code, System.Array.Empty<string>());
    }

    [Fact]
    public void Generic_Declaration_Name_Is_Type()
    {
        AssertTypeNamed("public class Repository<T> { }", "Repository");
    }
}
