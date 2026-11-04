namespace EasyPark.Platform.Iam.Domain.Model.Commands;

public record SignUpCommand(string? Email, string? Password, string? Role);

public record SignInCommand(string? Email, string? Password);

public record ChangePasswordCommand(int UserAccountId, string? CurrentPassword, string? NewPassword);
