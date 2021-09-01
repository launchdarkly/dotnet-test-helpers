using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace LaunchDarkly.TestHelpers
{
    /// <summary>
    /// Test assertions that may be helpful in testing generic type behavior.
    /// </summary>
    public static class TypeBehavior
    {
        public static void AssertEqual<T>(T a, T b)
        {
            Assert.Equal(a, b);
            Assert.Equal(b, a);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        public static void AssertNotEqual<T>(T a, T b)
        {
            Assert.NotEqual(a, b);
            Assert.NotEqual(b, a);
        }

        /// <summary>
        /// Implements a standard test suite for custom implementations of <c>Equals()</c>
        /// and <c>GetHashCode()</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <paramref name="valueFactories"/> parameter is a list of value factories. Each
        /// factory must produce only instances that are equal to each other, and not equal to
        /// the instances produced by any of the other factories. The test suite verifies the
        /// following:
        /// </para>
        /// <list type="bullet">
        /// <item> For any instance <c>a</c> created by any of the factories, <c>a.Equals(a)</c>
        /// is true, <c>a.Equals(null)</c> is false, and <c>a.equals(x)</c> where <c>x</c> is an
        /// an instance of a different class is false. </item>
        /// <item> For any two instances <c>a</c> and <c>b</c> created by the same factory,
        /// <c>a.Equals(b)</c>, <c>b.Equals(a)</c>, and <c>a.GetHashCode() == b.GetGashCode()</c>
        /// are all true. </item>
        /// <item> For any two instances <c>a</c> and <c>b</c> created by different factories,
        /// <c>a.Equals(b)</c> and <c>b.Equals(a)</c> are false (there is no requirement that
        /// the hash codes are different). </item>
        /// </list>
        /// </remarks>
        /// <param name="valueFactories">list of factories for distinct values</param>
        public static void CheckEqualsAndHashCode<T>(params Func<T>[] valueFactories)
        {
            for (int i = 0; i < valueFactories.Length; i++)
            {
                for (int j = 0; j < valueFactories.Length; j++)
                {
                    T value1 = valueFactories[i]();
                    T value2 = valueFactories[j]();
                    if (Object.ReferenceEquals(value1, value2))
                    {
                        Assert.False(true, "value factory must not return the same instance twice");
                    }
                    if (i == j)
                    {
                        // instance is equal to itself
                        Assert.True(value1.Equals(value1), "value was not equal to itself: " + value1);

                        // commutative equality
                        Assert.True(value1.Equals(value2), "(" + value1 + ").equals(" + value2 + ") was false");
                        Assert.True(value2.Equals(value1),
                            "(" + value1 + ").equals(" + value2 + ") was true, but (" +
                            value2 + ").equals(" + value1 + ") was false");

                        // equal hash code
                        if (value1.GetHashCode() != value2.GetHashCode())
                        {
                            Assert.True(false, "(" + value1 + ").GetHashCode() was " + value1.GetHashCode() + " but ("
                                + value2 + ").GetHashCode() was " + value2.GetHashCode());
                        }

                        // unequal to null, unequal to value of wrong class
                        Assert.False(value1.Equals(null), "value was equal to null: " + value1);

                        Assert.False(value1.Equals(new Object()), "value was equal to Object: " + value1);
                    }
                    else
                    {
                        // commutative inequality
                        Assert.False(value1.Equals(value2), "(" + value1 + ").equals(" + value2 + ") was true");
                        Assert.False(value2.Equals(value1), "(" + value2 + ").equals(" + value1 + ") was true");
                    }
                }
            }
        }

        /// <summary>
        /// Creates a factory that returns the specified instances in order each time it
        /// is called. After all instances are used, it starts over at the first.
        /// </summary>
        /// <remarks>
        /// This is for use with <see cref="CheckEqualsAndHashCode{T}(Func{T}[])"/>.
        /// </remarks>
        /// <typeparam name="T">the value type</typeparam>
        /// <param name="values">instances of the value</param>
        /// <returns>a factory function</returns>
        public static Func<T> ValueFactoryFromInstances<T>(params T[] values)
        {
            int counter = 0;
            return () =>
            {
                int i = Interlocked.Increment(ref counter);
                if (i > values.Length)
                {
                    i = 1;
                }
                return values[i - 1];
            };
        }
    }
}
