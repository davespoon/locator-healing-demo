using System.CommandLine;
using System.CommandLine.Parsing;

namespace LocatorHealing.Agent.Cli;

internal static class FileArgumentValidator
{
    public static void Validate(
        ArgumentResult result,
        Argument<FileInfo> argument,
        string expectedExtension,
        string fileLabel)
    {
        var file = result.GetValue(argument);

        if (file is null)
        {
            result.AddError($"A {fileLabel} path is required.");
            return;
        }

        if (!file.Exists)
        {
            result.AddError($"File does not exist: {file.FullName}");
            return;
        }

        if (!string.Equals(file.Extension, expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            result.AddError($"File must have a {expectedExtension} extension.");
        }
    }

    public static void ValidateDirectory(
        ArgumentResult result,
        Argument<DirectoryInfo> argument)
    {
        var directory = result.GetValue(argument);

        if (directory is null)
        {
            result.AddError("A test results directory path is required.");
            return;
        }

        if (!directory.Exists)
        {
            result.AddError($"Directory does not exist: {directory.FullName}");
        }
    }
}