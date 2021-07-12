using System;
using Xunit;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Botframework.LUParser.parser;

namespace Microsoft.Botframework.LUParser.Tests.parser
{
    public class LuParserTests
    {
        [Fact]
        public void ParseLuContent()
        {
            // var luContent = "# Help"+ Environment.NewLine + "- help" + Environment.NewLine + "- I need help" + Environment.NewLine + "- please help";

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Fixtures", "ImportAllLu.txt");
            Console.WriteLine(path);

            var luContent = File.ReadAllText(path);
            luContent = luContent.Substring(0, luContent.Length - 1);
            var result = LuParser.parse(luContent);
            LuResource expected = JsonConvert.DeserializeObject<LuResource>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Fixtures", "ImportAllLu.json")));

            var serializedResult = JsonConvert.SerializeObject(result).Replace("\\r", "");
            var serializedExpected = JsonConvert.SerializeObject(expected).Replace("\\r", "");

            Assert.Equal(serializedResult, serializedExpected);
        }
    }
}
