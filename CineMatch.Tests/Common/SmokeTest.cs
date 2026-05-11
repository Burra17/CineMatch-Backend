using System;
using System.Collections.Generic;
using System.Text;

namespace CineMatch.Tests.Common
{
    [TestFixture]
    public class SmokeTest
    {
        [Test]
        public void TestPipeline_IsConfiguredCorrectly()
        {
            // Arrange
            var expected = true;
            // Act
            var actual = true;
            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void CiVerificationTest_ShouldFail()
        {
            Assert.That(true, Is.EqualTo(false));
        }
    }
}
