using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GroupProject.Tests;

/// <summary>
/// Tests for the Program class command-line argument processing.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void ProcessArgs_ConvertsToLowerCase()
    {
        // Arrange
        var args = new[] { "--BLACKJACK", "--CONFIG", "SHOW" };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Equal(new[] { "--blackjack", "--config", "show" }, result);
    }

    [Fact]
    public void ProcessArgs_HandlesEmptyArray()
    {
        // Arrange
        var args = Array.Empty<string>();
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ProcessArgs_HandlesMixedCase()
    {
        // Arrange
        var args = new[] { "--BlAcKjAcK", "--StAtS", "ShOw", "AlIcE" };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Equal(new[] { "--blackjack", "--stats", "show", "alice" }, result);
    }

    [Theory]
    [InlineData("--blackjack")]
    [InlineData("--config")]
    [InlineData("--stats")]
    [InlineData("--test")]
    [InlineData("--help")]
    [InlineData("--version")]
    [InlineData("--diagnostics")]
    public void ProcessArgs_HandlesValidCommands(string command)
    {
        // Arrange
        var args = new[] { command };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Single(result);
        Assert.Equal(command.ToLowerInvariant(), result[0]);
    }

    [Fact]
    public void ProcessArgs_HandlesComplexArguments()
    {
        // Arrange
        var args = new[] { "--CONFIG", "SET", "DECKS", "4" };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal("--config", result[0]);
        Assert.Equal("set", result[1]);
        Assert.Equal("decks", result[2]);
        Assert.Equal("4", result[3]);
    }

    [Fact]
    public void ProcessArgs_PreservesSpecialCharacters()
    {
        // Arrange
        var args = new[] { "--stats", "export", "C:\\Path\\To\\File.json" };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("--stats", result[0]);
        Assert.Equal("export", result[1]);
        Assert.Equal("c:\\path\\to\\file.json", result[2]);
    }

    [Theory]
    [InlineData(new string[] { }, true)]
    [InlineData(new[] { "--blackjack" }, false)]
    [InlineData(new[] { "--config", "show" }, false)]
    [InlineData(new[] { "--stats", "show", "alice" }, false)]
    [InlineData(new[] { "--help" }, false)]
    [InlineData(new[] { "--version" }, false)]
    [InlineData(new[] { "--diagnostics" }, false)]
    public void CommandLineArguments_ShowUsageForEmptyArgs(string[] args, bool shouldShowUsage)
    {
        // This test verifies the logic for when to show usage information
        // Empty args should show usage, all other valid commands should not
        
        // Arrange & Act
        var processedArgs = InvokeProcessArgs(args);
        var isEmpty = processedArgs.Length == 0;
        
        // Assert
        Assert.Equal(shouldShowUsage, isEmpty);
    }

    [Theory]
    [InlineData("--config", new[] { "--config" }, false)] // Missing sub-command
    [InlineData("--config", new[] { "--config", "show" }, true)] // Valid sub-command
    [InlineData("--config", new[] { "--config", "set", "decks", "4" }, true)] // Valid with parameters
    [InlineData("--stats", new[] { "--stats" }, false)] // Missing sub-command
    [InlineData("--stats", new[] { "--stats", "show" }, true)] // Valid sub-command
    [InlineData("--test", new[] { "--test" }, false)] // Missing sub-command
    [InlineData("--test", new[] { "--test", "connection" }, true)] // Valid sub-command
    public void CommandLineArguments_ValidateSubCommands(string command, string[] args, bool hasValidSubCommand)
    {
        // This test verifies the structure for commands that require sub-commands
        
        // Arrange & Act
        var processedArgs = InvokeProcessArgs(args);
        var hasSubCommand = processedArgs.Length > 1;
        
        // Assert
        if (command == "--config" || command == "--stats" || command == "--test")
        {
            Assert.Equal(hasValidSubCommand, hasSubCommand);
        }
    }

    [Fact]
    public void CommandLineArguments_HandleInvalidCommand()
    {
        // Arrange
        var args = new[] { "--invalid-command" };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("--invalid-command", result[0]);
        // The actual handling of invalid commands is done in the Main method
        // This test just verifies that the argument processing doesn't fail
    }

    [Theory]
    [InlineData("--help", "blackjack")]
    [InlineData("--help", "config")]
    [InlineData("--help", "stats")]
    [InlineData("--help", "test")]
    [InlineData("--help", "version")]
    [InlineData("--help", "diagnostics")]
    public void CommandLineArguments_HandleHelpSubCommands(string helpCommand, string subCommand)
    {
        // Arrange
        var args = new[] { helpCommand, subCommand };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(helpCommand.ToLowerInvariant(), result[0]);
        Assert.Equal(subCommand.ToLowerInvariant(), result[1]);
    }

    [Fact]
    public void CommandLineArguments_HandleLongArgumentLists()
    {
        // Arrange
        var args = new[] { "--config", "set", "decks", "4", "players", "6", "split", "true" };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Equal(8, result.Length);
        Assert.All(result, arg => Assert.Equal(arg, arg.ToLowerInvariant()));
    }

    [Theory]
    [InlineData("--stats", "show", "Player With Spaces")]
    [InlineData("--stats", "export", "file with spaces.json")]
    public void CommandLineArguments_HandleArgumentsWithSpaces(string command, string subCommand, string argument)
    {
        // Arrange
        var args = new[] { command, subCommand, argument };
        
        // Act
        var result = InvokeProcessArgs(args);
        
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(command.ToLowerInvariant(), result[0]);
        Assert.Equal(subCommand.ToLowerInvariant(), result[1]);
        Assert.Equal(argument.ToLowerInvariant(), result[2]);
    }

    /// <summary>
    /// Helper method to invoke the private ProcessArgs method using reflection.
    /// </summary>
    /// <param name="args">Arguments to process</param>
    /// <returns>Processed arguments</returns>
    private static string[] InvokeProcessArgs(string[] args)
    {
        var programType = typeof(Program);
        var method = programType.GetMethod("ProcessArgs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        if (method == null)
        {
            throw new InvalidOperationException("ProcessArgs method not found");
        }
        
        var result = method.Invoke(null, new object[] { args });
        return (string[])result!;
    }
}