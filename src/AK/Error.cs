// Base: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/AK/Error.h
// Base: https://github.com/LadybirdBrowser/ladybird/blob/c2ab0dafb2fab580e593e2bdac22bb355eab5a22/AK/Error.cpp
namespace AK;

using System;

/// <summary>
/// Represents an error with an error code and optional message.
/// This is used to model C++ style error handling in the ported code.
/// </summary>
public class Error : Exception
{
    public int ErrorCode { get; }

    public Error(int errorCode) : base($"Error code: {errorCode}")
    {
        ErrorCode = errorCode;
    }

    public Error(int errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates an error from an errno value (Unix-style error code)
    /// </summary>
    public static Error FromErrno(int errno)
    {
        return new Error(errno, GetErrorMessage(errno));
    }

    private static string GetErrorMessage(int errno)
    {
        // Common errno values
        return errno switch
        {
            1 => "Operation not permitted",
            2 => "No such file or directory",
            4 => "Interrupted system call",
            9 => "Bad file descriptor",
            11 => "Resource temporarily unavailable",
            12 => "Out of memory",
            13 => "Permission denied",
            22 => "Invalid argument",
            125 => "Operation canceled",
            _ => $"Unknown error {errno}"
        };
    }
}