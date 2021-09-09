using System;
using Xunit;

namespace LaunchDarkly.TestHelpers
{
    public class JsonAssertionsTest
    {
        [Fact]
        public void AssertJsonEqualSuccess()
        {
            JsonEqualShouldSucceed("null", "null");
            JsonEqualShouldSucceed("true", "true");
            JsonEqualShouldSucceed("1", "1");
            JsonEqualShouldSucceed("\"x\"", "\"x\"");
            JsonEqualShouldSucceed("{\"a\":1,\"b\":{\"c\":2}}", "{\"b\":{\"c\":2},\"a\":1}");
            JsonEqualShouldSucceed("[1,2,[3,4]]", "[1,2,[3,4]]");
        }

        private static void JsonEqualShouldSucceed(string expected, string actual)
        {
            JsonAssertions.AssertJsonEqual(expected, actual);
            JsonAssertions.AssertJsonEqual(actual, expected);
            JsonAssertions.AssertJsonEqual(JsonTestValue.JsonOf(expected), JsonTestValue.JsonOf(actual));
        }

        [Fact]
        public void JsonEqualFailureWithNoDetailedDiff()
        {
            JsonEqualShouldFail("{\"a\":1,\"b\":2}", "{\"a\":1,\"b\":3}",
                "at \"b\": expected = 2, actual = 3");

            JsonEqualShouldFail("{\"a\":1,\"b\":2}", "{\"a\":1}",
                "at \"b\": expected = 2, actual = <absent>");

            JsonEqualShouldFail("{\"a\":1}", "{\"a\":1,\"b\":2}",
                "at \"b\": expected = <absent>, actual = 2");

            JsonEqualShouldFail("{\"a\":1,\"b\":{\"c\":2}}", "{\"a\":1,\"b\":{\"c\":3}}",
                "at \"b.c\": expected = 2, actual = 3");

            JsonEqualShouldFail("{\"a\":1,\"b\":[2,3]}", "{\"a\":1,\"b\":[3,3]}",
                "at \"b\\[0\\]\": expected = 2, actual = 3");

            JsonEqualShouldFail("[100,200,300]", "[100,201,300]",
                "at \"\\[1\\]\": expected = 200, actual = 201");

            JsonEqualShouldFail("[100,[200,210],300]", "[100,[201,210],300]",
                "at \"\\[1\\]\\[0\\]\": expected = 200, actual = 201");

            JsonEqualShouldFail("[100,{\"a\":1},300]", "[100,{\"a\":2},300]",
                "at \"\\[1\\].a\": expected = 1, actual = 2");
        }

        [Fact]
        public void JsonEqualFailureWithDetailedDiff()
        {
            JsonEqualShouldFail("null", null, "no value");
            JsonEqualShouldFail("null", "{", "not valid JSON");
            JsonEqualShouldFail("null", "true", "Expected: *null\nActual: *true");
            JsonEqualShouldFail("false", "true", "Expected: *false\nActual: *true");
            JsonEqualShouldFail("{\"a\":1}", "3", "Expected: *{\"a\":1}\nActual: *3");
            JsonEqualShouldFail("[1,2]", "3", "Expected: *\\[1,2\\]\nActual: *3");
            JsonEqualShouldFail("[1,2]", "[1,2,3]", "Expected: *\\[1,2\\]\nActual: *\\[1,2,3\\]");
        }

        private static void JsonEqualShouldFail(string expected, string actual, string expectedMessage)
        {
            ShouldFailWithMessage(expectedMessage, () => JsonAssertions.AssertJsonEqual(expected, actual));
            ShouldFailWithMessage(expectedMessage, () => JsonAssertions.AssertJsonEqual(
                JsonTestValue.JsonOf(expected), JsonTestValue.JsonOf(actual)));
        }

        [Fact]
        public void AssertJsonIncludesSuccess()
        {
            JsonIncludesShouldSucceed("{\"a\":1,\"b\":2}", "{\"b\":2,\"a\":1}");
            JsonIncludesShouldSucceed("{\"a\":1,\"b\":2}", "{\"b\":2,\"a\":1,\"c\":3}");
            JsonIncludesShouldSucceed("{\"a\":1,\"b\":{\"c\":2}}", "{\"b\":{\"c\":2,\"d\":3},\"a\":1}");
        }

        private static void JsonIncludesShouldSucceed(string expected, string actual)
        {
            JsonAssertions.AssertJsonIncludes(expected, actual);
            JsonAssertions.AssertJsonIncludes(JsonTestValue.JsonOf(expected), JsonTestValue.JsonOf(actual));
        }

        [Fact]
        public void AssertJsonIncludesFailure()
        {
            JsonIncludesShouldFail("null", null, "no value");
            JsonIncludesShouldFail("null", "{", "not valid JSON");

            JsonIncludesShouldFail("{\"a\":1}", "{\"a\":0,\"b\":2,\"c\":3}",
                "at \"a\": expected = 1, actual = 0");

            JsonIncludesShouldFail("{\"a\":1}", "{\"b\":2,\"c\":3}",
                "at \"a\": expected = 1, actual = <absent>");

            JsonIncludesShouldFail("{\"b\":2,\"a\":1,\"c\":3}", "{\"a\":1,\"b\":2}",
                "at \"c\": expected = 3, actual = <absent>");

            JsonIncludesShouldFail("{\"b\":{\"c\":2,\"d\":3},\"a\":1}", "{\"a\":1,\"b\":{\"c\":2}}",
                "at \"b.d\": expected = 3, actual = <absent>");
        }

        private static void JsonIncludesShouldFail(string expected, string actual, string expectedMessage)
        {
            ShouldFailWithMessage(expectedMessage, () => JsonAssertions.AssertJsonIncludes(expected, actual));
            ShouldFailWithMessage(expectedMessage, () => JsonAssertions.AssertJsonIncludes(
                JsonTestValue.JsonOf(expected), JsonTestValue.JsonOf(actual)));
        }

        private static void ShouldFailWithMessage(string expectedMessage, Action action)
        {
            var ex = Assert.ThrowsAny<Exception>(action);
            Assert.Matches(expectedMessage, ex.Message);
        }
    }

    public class JsonTestValueTest
    {
        [Fact]
        public void ParseUndefined()
        {
            var v = JsonTestValue.JsonOf(null);
            Assert.False(v.IsDefined);
            Assert.Equal("<no value>", v.ToString());
        }

        [Fact]
        public void ParseMalformed()
        {
            Assert.ThrowsAny<FormatException>(() => JsonTestValue.JsonOf("{no"));
        }

        [Fact]
        public void ParseSuccess()
        {
            var v = JsonTestValue.JsonOf("123");
            Assert.True(v.IsDefined);
            Assert.Equal("123", v.ToString());
        }

        [Fact]
        public void FromValue()
        {
            Assert.Equal(JsonTestValue.JsonOf("123"), JsonTestValue.JsonFromValue(123));
            Assert.Equal(JsonTestValue.JsonOf("true"), JsonTestValue.JsonFromValue(true));
        }

        [Fact]
        public void DeepEquality()
        {
            Assert.Equal(JsonTestValue.JsonOf(@"{""a"":[1,2],""b"":true}"),
                JsonTestValue.JsonOf(@"{""b"":true,""a"":[1,2]}"));

            Assert.NotEqual(JsonTestValue.JsonOf(@"{""a"":[1,2],""b"":true}"),
                JsonTestValue.JsonOf(@"{""b"":true,""a"":[1,3]}"));
        }
    }
}
