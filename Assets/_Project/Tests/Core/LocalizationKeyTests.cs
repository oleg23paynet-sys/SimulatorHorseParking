using System;
using HorseParking.Core.Localization;
using NUnit.Framework;

namespace HorseParking.Tests.Core
{
    public sealed class LocalizationKeyTests
    {
        [Test]
        public void Constructor_EmptyValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LocalizationKey(""));
        }

        [Test]
        public void Constructor_ValidValue_PreservesKey()
        {
            var key = new LocalizationKey("interaction.unavailable");

            Assert.That(key.Value, Is.EqualTo("interaction.unavailable"));
        }
    }
}
