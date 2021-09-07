using System;
using Xunit;

namespace LaunchDarkly.TestHelpers
{
    public class BuilderBehaviorTest
    {
        [Fact]
        public void BuildTesterPropertySuccess()
        {
            var tester = BuilderBehavior.For(() => new MyBuilder(), b => b.Build());
            DoBuildTesterAssertions(tester);
        }

        [Fact]
        public void BuildTesterWithCopyConstructorPropertySuccess()
        {
            var tester = BuilderBehavior.For(() => new MyBuilder(), b => b.Build())
                .WithCopyConstructor(m => new MyBuilder(m));
            DoBuildTesterAssertions(tester);
        }

        private void DoBuildTesterAssertions(BuilderBehavior.BuildTester<MyBuilder, MyType> tester)
        {
            var aProp = tester.Property(m => m.A, (b, v) => b.A(v));
            var bProp = tester.Property(m => m.B, (b, v) => b.B(v));
            DoValidPropertyAssertions(aProp, bProp);
        }

        private void DoValidPropertyAssertions(BuilderBehavior.IPropertyAssertions<int> aProp,
            BuilderBehavior.IPropertyAssertions<int> bProp)
        {
            aProp.AssertDefault(MyBuilder.DefaultA);
            bProp.AssertDefault(MyBuilder.DefaultB);

            aProp.AssertCanSet(5);
            bProp.AssertCanSet(5);

            bProp.AssertSetIsChangedTo(MyBuilder.MinB - 1, MyBuilder.MinB);
            bProp.AssertSetIsChangedTo(MyBuilder.MaxB + 1, MyBuilder.MaxB);
        }

        [Fact]
        public void BuildTesterInternalStateSuccess()
        {
            var tester = BuilderBehavior.For(() => new MyBuilder());
            var aProp = tester.Property(b => b._a, (b, v) => b.A(v));
            var bProp = tester.Property(b => b._b, (b, v) => b.B(v));
            DoValidPropertyAssertions(aProp, bProp);
        }

        [Fact]
        public void BuildTesterFailsIfDefaultValueIsIncorrect()
        {
            var tester = BuilderBehavior.For(() => new BrokenBuilder(), b => b.Build());
            var aProp = tester.Property(m => m.A, (b, v) => b.A(v));
            var bProp = tester.Property(m => m.B, (b, v) => b.B(v));

            aProp.AssertDefault(BrokenBuilder.DefaultA);
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => bProp.AssertDefault(BrokenBuilder.DefaultB));
        }

        [Fact]
        public void BuildTesterFailsIfSetterDoesNotSetValue()
        {
            var tester = BuilderBehavior.For(() => new BrokenBuilder(), b => b.Build());
            var bProp = tester.Property(m => m.B, (b, v) => b.B(v));

            bProp.AssertCanSet(1);
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => bProp.AssertCanSet(BrokenBuilder.BValueThatFails));
        }

        [Fact]
        public void BuildTesterFailsIfSetterDoesNotChangeValueToDifferentValueAsExpected()
        {
            var tester = BuilderBehavior.For(() => new BrokenBuilder(), b => b.Build());
            var bProp = tester.Property(m => m.B, (b, v) => b.B(v));

            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => bProp.AssertSetIsChangedTo(
                BrokenBuilder.MinB - 1, BrokenBuilder.MinB));
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => bProp.AssertSetIsChangedTo(
                BrokenBuilder.MaxB + 1, BrokenBuilder.MaxB));
        }

        [Fact]
        public void BuildTesterFailsIfCopyConstructorDoesNotCopyValue()
        {
            var tester = BuilderBehavior.For(() => new BrokenBuilder(), b => b.Build());
            var bProp = tester.Property(m => m.B, (b, v) => b.B(v));

            var copyTester = tester.WithCopyConstructor(m => new BrokenBuilder(m));
            var bProp1 = copyTester.Property(m => m.B, (b, v) => b.B(v));
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => bProp1.AssertCanSet(1));
        }

        internal class MyBuilder
        {
            internal const int DefaultA = 2;
            internal const int DefaultB = 3;
            internal const int MinB = 0;
            internal const int MaxB = 10;

            internal int _a, _b;

            public MyBuilder()
            {
                _a = DefaultA;
                _b = DefaultB;
            }

            public MyBuilder(MyType m)
            {
                _a = m.A;
                _b = m.B;
            }

            public MyBuilder A(int a)
            {
                _a = a;
                return this;
            }

            public MyBuilder B(int b)
            {
                _b = b < MinB ? MinB : (b > MaxB ? MaxB : b);
                return this;
            }

            public MyType Build() => new MyType(_a, _b);
        }

        internal class MyType
        {
            internal int A { get; }
            internal int B { get; }

            internal MyType(int a, int b)
            {
                A = a;
                B = b;
            }
        }

        // This class has the following deliberate mistakes:
        // - the default for B is not DefaultB
        // - the setter for B sets the value incorrectly if it is BValueThatFails
        // - the copy constructor sets B incorrectly
        internal class BrokenBuilder
        {
            internal const int DefaultA = 2;
            internal const int DefaultB = 3;
            internal const int MinB = 0;
            internal const int MaxB = 10;
            internal const int BValueThatFails = 4;

            internal int _a, _b;

            public BrokenBuilder()
            {
                _a = DefaultA;
                _b = DefaultB + 1;
            }

            public BrokenBuilder(MyType m)
            {
                _a = m.A;
                _b = m.B + 1;
            }

            public BrokenBuilder A(int a)
            {
                _a = a;
                return this;
            }

            public BrokenBuilder B(int b)
            {
                _b = b == BValueThatFails ? (b + 1) : b;
                return this;
            }

            public MyType Build() => new MyType(_a, _b);
        }

    }
}
