﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit.Sdk;

namespace LaunchDarkly.TestHelpers
{
    /// <summary>
    /// A simple wrapper for a string that can be parsed as JSON for tests.
    /// </summary>
    /// <remarks>
    /// This type provides strong typing so that it is clear when test matchers apply to JSON
    /// values versus strings, and hides the implementation details of parsing and serialization
    /// which are not relevant to the test logic.
    /// </remarks>
    /// <seealso cref="JsonAssertions"/>
    public struct JsonTestValue : IEquatable<JsonTestValue>
    {
        internal readonly string Raw;
        internal readonly JsonElement? Parsed;

        /// <summary>
        /// True if there is a value (that is, the original string was not a null reference).
        /// </summary>
        public bool IsDefined => Parsed.HasValue;

        /// <summary>
        /// Shortcut for creating an undefined value, equivalent to <c>JsonOf(null)</c>.
        /// </summary>
        public static JsonTestValue NoValue => JsonOf(null);

        private JsonTestValue(string raw, JsonElement? parsed)
        {
            Raw = raw;
            Parsed = parsed;
        }

        /// <summary>
        /// Creates a <c>JsonTestValue</c> from a string that should contain JSON.
        /// </summary>
        /// <remarks>
        /// This method fails immediately for any string that is not well-formed JSON. However, if
        /// it is a null reference, it returns an "undefined" instance that will return <c>false</c>
        /// from <see cref="IsDefined"/>.
        /// </remarks>
        /// <param name="raw">the input string</param>
        /// <returns>a <c>JsonTestValue</c></returns>
        /// <exception cref="FormatException">for malformed JSON</exception>
        public static JsonTestValue JsonOf(string raw)
        {
            if (raw is null)
            {
                return new JsonTestValue(null, null);
            }
            try
            {
                return new JsonTestValue(raw, JsonSerializer.Deserialize<JsonElement>(raw));
            }
            catch (Exception e)
            {
                throw new FormatException("not valid JSON (" + e + "): " + raw);
            }
        }

        internal static JsonTestValue OfParsed(JsonElement parsed)
        {
            return new JsonTestValue(JsonSerializer.Serialize(parsed), parsed);
        }

        /// <summary>
        /// Creates a <c>JsonTestValue</c> by serializing an arbitrary value to JSON.
        /// </summary>
        /// <remarks>
        /// For instance, <c>JsonFromValue(true)</c> is equivalent to <c>JsonOf("true")</c>.
        /// This only works for types that are supported by <c>System.Text.Json</c>'s default
        /// reflection-based serialization mechanism.
        /// </remarks>
        /// <param name="value">an arbitrary value</param>
        /// <returns>a <c>JsonTestValue</c></returns>
        public static JsonTestValue JsonFromValue(object value) =>
            OfParsed(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(value)));

        /// <summary>
        /// If this value is a JSON object, return the value of the specified property or
        /// an undefined value if there is no such property.
        /// </summary>
        /// <param name="name">the property name</param>
        /// <returns>the value, if any</returns>
        /// <exception cref="XunitException">if the current value is not an object</exception>
        public JsonTestValue Property(string name)
        {
            if (Parsed.HasValue && Parsed.Value.ValueKind == JsonValueKind.Object)
            {
                return Parsed.Value.TryGetProperty(name, out var v) ? OfParsed(v) : JsonOf(null);
            }
            throw new XunitException(string.Format("Expected a JSON object but got {0}", this));
        }

        /// <summary>
        /// If this value is a JSON object, return the value of the specified property.
        /// </summary>
        /// <param name="name">the property name</param>
        /// <returns>the value, if any</returns>
        /// <exception cref="XunitException">if the current value is not an object
        /// or it has no such property </exception>
        public JsonTestValue RequiredProperty(string name)
        {
            if (Parsed.HasValue && Parsed.Value.ValueKind == JsonValueKind.Object)
            {
                if (Parsed.Value.TryGetProperty(name, out var v))
                {
                    return OfParsed(v);
                }
                throw new XunitException(string.Format(@"Did not find property ""{0}"" in {1}",
                    name, this));
            }
            throw new XunitException(string.Format("Expected a JSON object but got {0}", this));
        }

        /// <summary>
        /// Returns the JSON as a string, or a "no value" message if undefined.
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            Raw is null ? "<no value>" : Raw;

        /// <summary>
        /// Compares two values for deep equality.
        /// </summary>
        /// <param name="obj">another JSON value</param>
        /// <returns>true if the values are deeply equal, or are both undefined</returns>
        public override bool Equals(object obj) =>
            obj is JsonTestValue other && Equals(other);

        /// <summary>
        /// Compares two values for deep equality.
        /// </summary>
        /// <param name="other">another JSON value</param>
        /// <returns>true if the values are deeply equal, or are both undefined</returns>
        public bool Equals(JsonTestValue other) =>
            IsDefined ? (other.IsDefined && DeepEqualJson(Parsed.Value, other.Parsed.Value)) : !other.IsDefined;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            IsDefined ? Parsed.GetHashCode() : 0;

        internal static bool DeepEqualJson(JsonElement a, JsonElement b)
        {
            if (a.ValueKind != b.ValueKind)
            {
                return false;
            }
            switch (a.ValueKind)
            {
                case JsonValueKind.Number:
                    return a.GetDouble() == b.GetDouble();
                case JsonValueKind.String:
                    return a.GetString() == b.GetString();
                case JsonValueKind.Array:
                    List<JsonElement> aList = a.EnumerateArray().ToList(), bList = b.EnumerateArray().ToList();
                    if (aList.Count != bList.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < aList.Count; i++)
                    {
                        if (!DeepEqualJson(aList[i], bList[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                case JsonValueKind.Object:
                    var aProps = a.EnumerateObject().ToList();
                    var bProps = b.EnumerateObject().ToList();
                    if (aProps.Count != bProps.Count)
                    {
                        return false;
                    }
                    foreach (var ap in aProps)
                    {
                        if (!b.TryGetProperty(ap.Name, out var bv) || !DeepEqualJson(ap.Value, bv))
                        {
                            return false;
                        }
                    }
                    return true;
                default:
                    return true;
            }
        }
    }

    /// <summary>
    /// Test assertions related to JSON.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <code>
    ///     using static LaunchDarkly.TestHelpers.JsonAssertions;
    ///     using static LaunchDarkly.TestHelpers.JsonTestValue;
    ///
    ///     AssertJsonEquals(@"{""a"":1, ""b"":2}", @"{""b"":2, ""a"":1}");
    ///     AssertJsonIncludes(@"{""a"":1}", @"{""b"":2, ""a"":1}");
    ///     AssertJsonEquals(JsonFromValue(true), JsonOf(@"{""a"":true}").Property("a"));
    /// </code>
    /// </remarks>
    public class JsonAssertions
    {
        /// <summary>
        /// Parses two strings as JSON and compares them for deep equality. If they are unequal,
        /// it tries to describe the difference as specifically as possible by recursing into
        /// object properties or array elements.
        /// </summary>
        /// <param name="expected">the expected value</param>
        /// <param name="actual">the actual value</param>
        /// <exception cref="FormatException">for malformed JSON</exception>
        public static void AssertJsonEqual(string expected, string actual) =>
            AssertJsonEqual(JsonTestValue.JsonOf(expected), JsonTestValue.JsonOf(actual));

        /// <summary>
        /// Compares two JSON values for deep equality. If they are unequal, it tries to describe
        /// the difference as specifically as possible by recursing into object properties or
        /// array elements.
        /// </summary>
        /// <param name="expected">the expected value</param>
        /// <param name="actual">the actual value</param>
        public static void AssertJsonEqual(JsonTestValue expected, JsonTestValue actual)
        {
            if (!actual.IsDefined)
            {
                if (!expected.IsDefined)
                {
                    return;
                }
                throw new AssertActualExpectedException(expected, actual, "AssertJsonEqual failed");
            }
            if (!expected.IsDefined)
            {
                throw new AssertActualExpectedException(expected, actual, "AssertJsonEqual failed");
            }
            if (actual.Equals(expected))
            {
                return;
            }
            var diff = DescribeJsonDifference(expected.Parsed.Value, actual.Parsed.Value, "", false);
            if (diff is null)
            {
                throw new AssertActualExpectedException(expected, actual, "AssertJsonEqual failed");
            }
            throw new XunitException(string.Format(
                "AssertJSONEqual failed:{0}{1}{0}full JSON was: {2}", Environment.NewLine, diff, actual));
        }

        /// <summary>
        /// Similar to <see cref="AssertJsonEqual(string, string)"/> except that when comparing JSON
        /// objects (at any level) it allows the actual data to contain extra properties that are not in
        /// the expected data, and when comparing JSON arrays (at any level) it only checks whether the
        /// expected elements appear somewhere in the actual array in any order.
        /// </summary>
        /// <param name="expected">the expected value</param>
        /// <param name="actual">the actual value</param>
        public static void AssertJsonIncludes(string expected, string actual) =>
            AssertJsonIncludes(JsonTestValue.JsonOf(expected), JsonTestValue.JsonOf(actual));

        /// <summary>
        /// Similar to <see cref="AssertJsonEqual(JsonTestValue, JsonTestValue)"/> except that when comparing JSON
        /// objects (at any level) it allows the actual data to contain extra properties that are not in
        /// the expected data, and when comparing JSON arrays (at any level) it only checks whether the
        /// expected elements appear somewhere in the actual array in any order.
        /// </summary>
        /// <param name="expected">the expected value</param>
        /// <param name="actual">the actual value</param>
        public static void AssertJsonIncludes(JsonTestValue expected, JsonTestValue actual)
        {
            if (!actual.IsDefined)
            {
                if (!expected.IsDefined)
                {
                    return;
                }
                throw new AssertActualExpectedException(expected, actual, "AssertJsonIncludes failed");
            }
            if (!expected.IsDefined)
            {
                throw new AssertActualExpectedException(expected, actual, "AssertJsonIncludes failed");
            }
            if (IsJsonSubset(expected.Parsed.Value, actual.Parsed.Value))
            {
                return;
            }
            var diff = DescribeJsonDifference(expected.Parsed.Value, actual.Parsed.Value, "", true);
            if (diff is null)
            {
                throw new AssertActualExpectedException(expected, actual, "AssertJsonIncludes failed");
            }
            throw new XunitException(string.Format(
                "AssertJsonIncludes failed:{0}{1}{0}full JSON was: {2}", Environment.NewLine, diff, actual));
        }

        private static bool IsJsonSubset(JsonElement expected, JsonElement actual)
        {
            if (expected.ValueKind == JsonValueKind.Object && actual.ValueKind == JsonValueKind.Object) {
                foreach (var e in expected.EnumerateObject())
                {
                    if (!actual.TryGetProperty(e.Name, out var av) || !IsJsonSubset(e.Value, av))
                    {
                        return false;
                    }
                }
                return true;
            }
            if (expected.ValueKind == JsonValueKind.Array && actual.ValueKind == JsonValueKind.Array) {
                foreach (var ev in expected.EnumerateArray())
                {
                    bool found = false;
                    foreach (var av in actual.EnumerateArray())
                    {
                        if (IsJsonSubset(ev, av))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                return true;
            }
            return JsonTestValue.DeepEqualJson(expected, actual);
        }

        private static string DescribeJsonDifference(JsonElement expected, JsonElement actual, string prefix, bool allowExtraProps)
        {
            if (actual.ValueKind == JsonValueKind.Object && expected.ValueKind == JsonValueKind.Object)
            {
                return DescribeJsonObjectDifference(expected, actual, prefix, allowExtraProps);
            }
            if (actual.ValueKind == JsonValueKind.Array && expected.ValueKind == JsonValueKind.Array)
            {
                return DescribeJsonArrayDifference(expected, actual, prefix, allowExtraProps);
            }
            return null;
        }

        private static string DescribeJsonObjectDifference(JsonElement expected, JsonElement actual, string prefix, bool allowExtraProps)
        {
            var diffs = new List<string>();
            foreach (var key in expected.EnumerateObject().Select(p => p.Name).Union(actual.EnumerateObject().Select(p => p.Name)))
            {
                var prefixedKey = prefix + (prefix == "" ? "" : ".") + key;
                string expectedDesc = null, actualDesc = null, detailDiff = null;
                if (expected.TryGetProperty(key, out var expectedValue))
                {
                    if (actual.TryGetProperty(key, out var actualValue))
                    {
                        if (!JsonTestValue.DeepEqualJson(actualValue, expectedValue))
                        {
                            expectedDesc = expectedValue.ToString();
                            actualDesc = actualValue.ToString();
                            detailDiff = DescribeJsonDifference(expectedValue, actualValue, prefixedKey, allowExtraProps);
                        }
                    }
                    else
                    {
                        expectedDesc = JsonSerializer.Serialize(expectedValue);
                        actualDesc = "<absent>";
                    }
                }
                else if (!allowExtraProps)
                {
                    actualDesc = JsonSerializer.Serialize(actual.GetProperty(key));
                    expectedDesc = "<absent>";
                }
                if (expectedDesc != null || actualDesc != null)
                {
                    if (detailDiff != null)
                    {
                        diffs.Add(detailDiff);
                    }
                    else
                    {
                        diffs.Add(string.Format(@"at ""{0}"": expected = {1}, actual = {2}", prefixedKey,
                            expectedDesc, actualDesc));
                    }
                }
            }
            return string.Join(Environment.NewLine, diffs);
        }

        private static string DescribeJsonArrayDifference(JsonElement expected, JsonElement actual, string prefix, bool allowExtraProps)
        {
            List<JsonElement> eList = expected.EnumerateArray().ToList(), aList = actual.EnumerateArray().ToList();
            if (eList.Count != aList.Count)
            {
                return null; // can't provide a detailed diff, just show the whole values
            }
            var diffs = new List<string>();
            for (int i = 0; i < eList.Count; i++)
            {
                var prefixedIndex = string.Format("{0}[{1}]", prefix, i);
                JsonElement actualValue = actual[i], expectedValue = expected[i];
                if (!JsonTestValue.DeepEqualJson(actualValue, expectedValue))
                {
                    var detailDiff = DescribeJsonDifference(expectedValue, actualValue, prefixedIndex, allowExtraProps);
                    if (detailDiff != null)
                    {
                        diffs.Add(detailDiff);
                    }
                    else
                    {
                        diffs.Add(string.Format(@"at ""{0}"": expected = {1}, actual = {2}", prefixedIndex,
                            expectedValue, actualValue));
                    }
                }
            }
            return string.Join(Environment.NewLine, diffs);
        }
    }
}
