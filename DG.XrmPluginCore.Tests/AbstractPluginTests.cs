using FluentAssertions;
using System;
using Xunit;

namespace DG.XrmPluginCore.Tests
{
    public class AbstractPluginTests
    {
        [Fact]
        public void Constructor_ShouldSetChildClassName()
        {
            // Arrange & Act
            var plugin = new TestAbstractPlugin();

            // Assert
            plugin.GetChildClassName().Should().Be(typeof(TestAbstractPlugin).ToString());
        }

        [Fact]
        public void Execute_IsAbstract_ShouldRequireImplementation()
        {
            // Arrange
            var plugin = new TestAbstractPlugin();

            // Act & Assert
            // This test verifies that the Execute method is implemented in the derived class
            Assert.Throws<NotImplementedException>(() => plugin.Execute(null));
        }
    }

    // Test implementation of AbstractPlugin
    public class TestAbstractPlugin : AbstractPlugin
    {
        public string GetChildClassName() => ChildClassName;

        public override void Execute(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException("Test implementation");
        }
    }
}