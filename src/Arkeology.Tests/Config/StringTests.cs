using Arkeology.Production.Client;
using Xunit;

namespace Arkeology.Tests.Config;

public class StringTests {
    string complexString = "This, a test string: This.should return back ,similar : yes (test + test2) \" -- hello";
    private StringTableBuilder builder = new ();    
    
    [Fact]
    public void TestComplexString() {
        ushort id = builder.Add(complexString);
        var table = builder.Build();

        Assert.Equal(complexString, table.Resolve(id));
    }
}
