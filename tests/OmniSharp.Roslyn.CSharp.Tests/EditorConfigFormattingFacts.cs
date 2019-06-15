using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OmniSharp.Models.CodeFormat;
using OmniSharp.Options;
using OmniSharp.Roslyn.CSharp.Services.Formatting;
using TestUtility;
using Xunit;
using Xunit.Abstractions;

namespace OmniSharp.Roslyn.CSharp.Tests
{
    public class EditorConfigFormattingFacts : AbstractTestFixture
    {
        public EditorConfigFormattingFacts(ITestOutputHelper output)
            : base(output)
        {
            // the shared host is initialized without .editorconfig
            // but each request uses a file path that is points to a folder
            // with .editorconfig file causing it be picked up irrespective of the global host settings
        }

        [Theory]
        [InlineData("dummy.cs")]
        [InlineData("dummy.csx")]
        public async Task RespectsDefaultFormatSettings(string filename)
        {
            var testFile = new TestFile(filename, "class Foo\n{\n public Foo()\n}\n}");
            var expected = "class Foo\n{\n    public Foo()\n}\n}";

            using (var host = CreateOmniSharpHost(new[] { testFile }, new Dictionary<string, string>
            {
                ["FormattingOptions:EnableEditorConfigSupport"] = "true"
            }, TestAssets.Instance.TestFilesFolder))
            {
                var requestHandler = host.GetRequestHandler<CodeFormatService>(OmniSharpEndpoints.CodeFormat);

                var request = new CodeFormatRequest { FileName = testFile.FileName };
                var response = await requestHandler.Handle(request);

                Assert.Equal(expected, response.Buffer);
            }
        }

        [Theory]
        [InlineData("dummy.cs")]
        [InlineData("dummy.csx")]
        public async Task RespectsSharedFormatSettings(string filename)
        {
            var testFile = new TestFile(Path.Combine(TestAssets.Instance.TestFilesFolder, filename), "class Foo\n{\n    public Foo()\n}\n}");
            var expected = "class Foo\n{\n public Foo()\n}\n}";

            using (var host = CreateOmniSharpHost(new[] { testFile }, new Dictionary<string, string>
            {
                ["FormattingOptions:EnableEditorConfigSupport"] = "true"
            }, TestAssets.Instance.TestFilesFolder))
            {
                var requestHandler = host.GetRequestHandler<CodeFormatService>(OmniSharpEndpoints.CodeFormat);

                var request = new CodeFormatRequest { FileName = testFile.FileName };
                var response = await requestHandler.Handle(request);

                Assert.Equal(expected, response.Buffer);
            }
        }

        [Theory]
        [InlineData("dummy.cs")]
        [InlineData("dummy.csx")]
        public async Task RespectsCSharpFormatSettings(string filename)
        {
            var testFile = new TestFile(Path.Combine(TestAssets.Instance.TestFilesFolder, filename), @"
class Foo { }
class Bar : Foo { }
");
            var expected = @"
class Foo { }
class Bar:Foo { }
";

            using (var host = CreateOmniSharpHost(new[] { testFile }, new Dictionary<string, string>
            {
                ["FormattingOptions:EnableEditorConfigSupport"] = "true"
            }, TestAssets.Instance.TestFilesFolder))
            {
                var requestHandler = host.GetRequestHandler<CodeFormatService>(OmniSharpEndpoints.CodeFormat);

                var request = new CodeFormatRequest { FileName = testFile.FileName };
                var response = await requestHandler.Handle(request);

                Assert.Equal(expected, response.Buffer);
            }
        }

        [Theory]
        [InlineData("dummy.cs")]
        [InlineData("dummy.csx")]
        public async Task RespectCSharpCodingConventions(string filename)
        {
            var testFile = new TestFile(Path.Combine(TestAssets.Instance.TestFilesFolder, filename), @"
class Foo
{
    public Foo()
    {
        var number1 = 0
        int number2 = 0;
    }
}");

            using (var host = CreateOmniSharpHost(new[] { testFile }, new Dictionary<string, string> {
                ["FormattingOptions:EnableEditorConfigSupport"] = "true",
                ["RoslynExtensionsOptions:EnableAnalyzersSupport"] = "true"
            }, TestAssets.Instance.TestFilesFolder))
            {
                var result = await host.RequestCodeCheckAsync();

                Assert.Contains(result.QuickFixes.Where(x => x.FileName == testFile.FileName), f => f.Text.Contains("IDE0049"));
                Assert.Contains(result.QuickFixes.Where(x => x.FileName == testFile.FileName), f => f.Text.Contains("IDE0008"));
            }
        }
    }
}
