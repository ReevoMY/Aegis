﻿namespace Reevo.License.EntityFrameworkCore.DTOs;

public class LicenseDeactivationResult(bool isSuccessful, Exception? exception = null)
{
    public bool IsSuccessful { get; } = isSuccessful;
    public Exception? Exception { get; } = exception;
}