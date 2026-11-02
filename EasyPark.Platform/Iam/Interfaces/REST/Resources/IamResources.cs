namespace EasyPark.Platform.Iam.Interfaces.REST.Resources;

/// <summary>Body of POST /api/v1/authentication/sign-up.</summary>
public record SignUpResource(string? Email, string? Password, string? Role);

/// <summary>Body of POST /api/v1/authentication/sign-in.</summary>
public record SignInResource(string? Email, string? Password);

/// <summary>Body of PUT /api/v1/authentication/password.</summary>
public record ChangePasswordResource(string? CurrentPassword, string? NewPassword);

/// <summary>Response of a successful sign-up.</summary>
public record UserAccountResource(int Id, string Email, string Role);

/// <summary>Response of a successful sign-in. <c>token</c> is the JWT to send as <c>Authorization: Bearer</c>.</summary>
public record AuthenticatedUserAccountResource(int Id, string Email, string Role, string Token, string? RefreshToken = null);

/// <summary>Generic message response.</summary>
public record MessageResource(string Message);
