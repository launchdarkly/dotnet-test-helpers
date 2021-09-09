using Xunit;
using Xunit.Sdk;

using static LaunchDarkly.TestHelpers.TypeBehavior;

namespace LaunchDarkly.TestHelpers
{
    public class TypeBehaviorTest
    {
        [Fact]
        public void TestValueFactoryFromInstances()
        {
            var f = ValueFactoryFromInstances("a", "b", "c");
            Assert.Equal("a", f());
            Assert.Equal("b", f());
            Assert.Equal("c", f());
            Assert.Equal("a", f());
        }

        [Fact]
        public void CheckEqualsAndHashCodeSuccess()
        {
            CheckEqualsAndHashCode(
                () => new TypeWithValueAndHashCode("a", 1),
                () => new TypeWithValueAndHashCode("b", 2),
                () => new TypeWithValueAndHashCode("c", 2) // hash codes deliberately the same as b - that is allowed             
                );
        }

        [Fact]
        public void CheckEqualsAndHashCodeFailureForIncorrectEquality()
        {
            Assert.ThrowsAny<XunitException>(() =>
                CheckEqualsAndHashCode(
                    () => new TypeThatEqualsEveryObjectAndAlwaysHasSameHashCode(),
                    () => new TypeThatEqualsEveryObjectAndAlwaysHasSameHashCode()
                    ));
        }

        [Fact]
        public void CheckEqualsAndHashCodeFailureForEqualingNull()
        {
            Assert.ThrowsAny<XunitException>(() =>
                CheckEqualsAndHashCode(
                    () => new TypeThatEqualsEveryObjectOrNullAndAlwaysHasSameHashCode()
                    ));
        }

        [Fact]
        public void CheckEqualsAndHashCodeFailureForIncorrectInequality()
        {
            Assert.ThrowsAny<XunitException>(() =>
                CheckEqualsAndHashCode(
                    () => new TypeThatEqualsOnlyItself()
                    ));
        }

        [Fact]
        public void CheckEqualsAndHashCodeFailureForObjectNotEqualingItself()
        {
            Assert.ThrowsAny<XunitException>(() =>
                CheckEqualsAndHashCode(
                    () => new TypeThatEqualsNothing()
                    ));
        }

        [Fact]
        public void CheckEqualsAndHashCodeFailureForNonTransitiveEquality()
        {
            Assert.ThrowsAny<XunitException>(() =>
                CheckEqualsAndHashCode(
                    ValueFactoryFromInstances(
                        new TypeThatEqualsSameOrHigherValue(1),
                        new TypeThatEqualsSameOrHigherValue(2))
                    ));
        }

        [Fact]
        public void CheckEqualsAndHashCodeFailureForNonTransitiveInequality()
        {
            Assert.ThrowsAny<XunitException>(() =>
                CheckEqualsAndHashCode(
                    ValueFactoryFromInstances(
                        new TypeThatEqualsSameOrHigherValue(1),
                        new TypeThatEqualsSameOrHigherValue(1)),
                    ValueFactoryFromInstances(
                        new TypeThatEqualsSameOrHigherValue(2),
                        new TypeThatEqualsSameOrHigherValue(2))
                    ));
        }

        [Fact]
        public void CheckEqualsAndHashCodeFailureForInconsistentHashCode()
        {
            Assert.ThrowsAny<XunitException>(() =>
                CheckEqualsAndHashCode(
                    ValueFactoryFromInstances(
                        new TypeWithValueAndHashCode("a", 1),
                        new TypeWithValueAndHashCode("a", 2))
                    ));
        }

        internal class TypeWithValueAndHashCode
        {
            private readonly string _value;
            private readonly int _hashCode;

            public TypeWithValueAndHashCode(string value, int hashCode)
            {
                _value = value;
                _hashCode = hashCode;
            }

            public override bool Equals(object other) =>
                other is TypeWithValueAndHashCode o && o._value == this._value;

            public override int GetHashCode() => _hashCode;

            public override string ToString() => _value + "/" + _hashCode;
            }
        }

        internal class TypeThatEqualsEveryObjectAndAlwaysHasSameHashCode
        {
            public override bool Equals(object o) => o != null;

            public override int GetHashCode() => 1;
        }

        internal class TypeThatEqualsEveryObjectOrNullAndAlwaysHasSameHashCode
        {
            public override bool Equals(object o) => true;

            public override int GetHashCode() => 1;
        }

        internal class TypeThatEqualsOnlyItself
        {
            public override bool Equals(object o) => this == o;

            public override int GetHashCode() => 1;
        }

        internal class TypeThatEqualsNothing
        {
            public override bool Equals(object o) => false;

            public override int GetHashCode() => 1;
        }

        internal class TypeThatEqualsSameOrHigherValue
        {
            private readonly int _index;

            public TypeThatEqualsSameOrHigherValue(int index)
            {
                _index = index;
            }

            public override bool Equals(object other) =>
                other is TypeThatEqualsSameOrHigherValue o &&
                    o._index >= this._index;

            public override int GetHashCode() => _index;
    }
}
