using System;
using Arkeology.Production.Client;
using Xunit;
using Xunit.Abstractions;

namespace Arkeology.Tests.Config;

public class StringTests {
    private readonly ITestOutputHelper _output;
    string complexString = "This, a test string: This.should return back ,similar : yes (test + test2) \" -- hello";
    private StringTableBuilder builder = new ();    
    
    public StringTests(ITestOutputHelper output) {
        _output = output;
    }
    
    [Fact]
    public void TestComplexString() {
        ushort id = builder.Add(complexString);
        var table = builder.Build();

        PrintTokens(table);
        
        Assert.Equal(complexString, table.Resolve(id));
    }

    private void PrintTokens(StringTable table) {
        foreach (string s in table.UserTokens) {
            _output.WriteLine(s);
        }
    }
}
