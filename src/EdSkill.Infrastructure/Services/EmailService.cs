using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EdSkill.Application.Common.Interfaces;
using EdSkill.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdSkill.Infrastructure.Services;

public class EmailService : IEmailService
{
	private const string ResendEmailsEndpoint = "https://api.resend.com/emails";

	private readonly EmailSettings _settings;
	private readonly HttpClient _httpClient;
	private readonly ILogger<EmailService> _logger;

	public EmailService(IOptions<EmailSettings> settings, HttpClient httpClient, ILogger<EmailService> logger)
	{
		_settings = settings.Value;
		_httpClient = httpClient;
		_logger = logger;
	}

	public async Task SendNotificationEmailAsync(string email, string subject, string message, CancellationToken cancellationToken = default)
	{
		try
		{
			await SendResendEmailAsync(
				email,
				$"EdSkill - {subject}",
				GetNotificationEmailTemplate(subject, message),
				cancellationToken);

			_logger.LogInformation("Notification email sent successfully to {Email}", email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send notification email to {Email}", email);
			throw;
		}
	}

	public async Task SendOtpEmailAsync(string email, string otp, CancellationToken cancellationToken = default)
	{
		try
		{
			await SendResendEmailAsync(
				email,
				"EdSkill - Email Verification Code",
				GetOtpEmailTemplate(otp),
				cancellationToken);

			_logger.LogInformation("OTP email sent successfully to {Email}", email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send OTP email to {Email}", email);
			throw;
		}
	}

	private async Task SendResendEmailAsync(
		string email,
		string subject,
		string html,
		CancellationToken cancellationToken)
	{
		EnsureResendSettingsConfigured();

		using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsEndpoint)
		{
			Content = JsonContent.Create(new
			{
				from = GetSenderAddress(),
				to = new[] { email },
				subject,
				html
			})
		};

		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

		using var response = await _httpClient.SendAsync(request, cancellationToken);

		if (response.IsSuccessStatusCode)
		{
			return;
		}

		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
		throw new InvalidOperationException(
			$"Resend email request failed with status {(int)response.StatusCode}: {responseBody}");
	}

	private void EnsureResendSettingsConfigured()
	{
		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
		{
			throw new InvalidOperationException("EmailSettings:ApiKey is required for Resend.");
		}

		if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
		{
			throw new InvalidOperationException("EmailSettings:SenderEmail is required for Resend.");
		}
	}

	private string GetSenderAddress()
	{
		if (string.IsNullOrWhiteSpace(_settings.SenderName))
		{
			return _settings.SenderEmail;
		}

		var senderName = _settings.SenderName.Replace("\"", string.Empty);
		return $"{senderName} <{_settings.SenderEmail}>";
	}

	// -------------------------------------------------------------------------
	// OTP TEMPLATE
	// -------------------------------------------------------------------------

	private static string GetOtpEmailTemplate(string otp)
	{
		var safeOtp = WebUtility.HtmlEncode(otp);

		return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Email Verification - EdSkill</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f7fb;font-family:Arial,'Segoe UI',Tahoma,sans-serif;color:#172033;"">
    <table role=""presentation"" style=""width:100%;border-collapse:collapse;"">
        <tr>
            <td align=""center"" style=""padding:32px 16px;"">
                <table role=""presentation"" style=""width:100%;max-width:620px;border-collapse:collapse;background:#ffffff;border:1px solid #dbe7f3;border-radius:16px;overflow:hidden;"">
                    <tr>
                        <td style=""padding:28px 32px 22px;background:#0f766e;"">
                            <table role=""presentation"" style=""width:100%;border-collapse:collapse;"">
                                <tr>
                                    <td>
                                        {GetBrandLogoHtml()}
                                    </td>
                                    <td align=""right"" style=""font-size:12px;font-weight:700;color:#ccfbf1;text-transform:uppercase;letter-spacing:1px;"">
                                        Learning account
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:34px 32px 8px;"">
                            <p style=""margin:0 0 10px;font-size:13px;font-weight:700;color:#0f766e;text-transform:uppercase;letter-spacing:1px;"">Verify your email</p>
                            <h1 style=""margin:0;color:#172033;font-size:28px;line-height:1.25;font-weight:800;"">Welcome to EdSkill</h1>
                            <p style=""margin:14px 0 0;color:#526173;font-size:15px;line-height:1.7;"">
                                Enter this code to finish setting up your learning account. The code is valid for <strong style=""color:#172033;"">5 minutes</strong>.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:24px 32px 10px;"">
                            <table role=""presentation"" style=""width:100%;border-collapse:collapse;background:#ecfdf5;border:1px solid #99f6e4;border-radius:14px;"">
                                <tr>
                                    <td align=""center"" style=""padding:26px 18px;"">
                                        <p style=""margin:0 0 10px;color:#0f766e;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:1.5px;"">Verification code</p>
                                        <div style=""font-family:'Courier New',Courier,monospace;font-size:40px;line-height:1;font-weight:800;letter-spacing:10px;color:#0f172a;"">{safeOtp}</div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:18px 32px 34px;"">
                            <table role=""presentation"" style=""width:100%;border-collapse:collapse;"">
                                <tr>
                                    <td style=""padding:14px 16px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;"">
                                        <p style=""margin:0;color:#526173;font-size:13px;line-height:1.7;"">
                                            For your safety, do not share this code. EdSkill will never ask for your OTP outside the app.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin:18px 0 0;color:#64748b;font-size:13px;line-height:1.7;"">
                                If you did not request this, you can safely ignore this email.
                            </p>
                        </td>
                    </tr>
                    {GetCommonFooterHtml()}
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
	}

	// -------------------------------------------------------------------------
	// NOTIFICATION TEMPLATE
	// -------------------------------------------------------------------------

	private static string GetNotificationEmailTemplate(string subject, string message)
	{
		var safeSubject = WebUtility.HtmlEncode(subject);
		var safeMessage = WebUtility.HtmlEncode(message).Replace("\n", "<br />");

		return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{safeSubject} - EdSkill</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f7fb;font-family:Arial,'Segoe UI',Tahoma,sans-serif;color:#172033;"">
    <table role=""presentation"" style=""width:100%;border-collapse:collapse;"">
        <tr>
            <td align=""center"" style=""padding:32px 16px;"">
                <table role=""presentation"" style=""width:100%;max-width:620px;border-collapse:collapse;background:#ffffff;border:1px solid #dbe7f3;border-radius:16px;overflow:hidden;"">
                    <tr>
                        <td style=""padding:28px 32px 22px;background:#2563eb;"">
                            <table role=""presentation"" style=""width:100%;border-collapse:collapse;"">
                                <tr>
                                    <td>
                                        {GetBrandLogoHtml()}
                                    </td>
                                    <td align=""right"" style=""font-size:12px;font-weight:700;color:#dbeafe;text-transform:uppercase;letter-spacing:1px;"">
                                        Course update
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:34px 32px 8px;"">
                            <p style=""margin:0 0 10px;font-size:13px;font-weight:700;color:#2563eb;text-transform:uppercase;letter-spacing:1px;"">Your learning timeline</p>
                            <h1 style=""margin:0;color:#172033;font-size:28px;line-height:1.25;font-weight:800;"">{safeSubject}</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:22px 32px 12px;"">
                            <table role=""presentation"" style=""width:100%;border-collapse:collapse;background:#eff6ff;border:1px solid #bfdbfe;border-radius:14px;"">
                                <tr>
                                    <td style=""padding:20px 22px;"">
                                        <p style=""margin:0;color:#1e3a8a;font-size:15px;line-height:1.8;"">{safeMessage}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:8px 32px 28px;"">
                            <table role=""presentation"" style=""width:100%;border-collapse:collapse;margin:0 0 24px;"">
                                <tr>
                                    <td style=""width:50%;padding:14px 14px 14px 0;vertical-align:top;"">
                                        <table role=""presentation"" style=""width:100%;border-collapse:collapse;background:#ffffff;border:1px solid #e2e8f0;border-radius:12px;"">
                                            <tr>
                                                <td style=""padding:16px;"">
                                                    <p style=""margin:0 0 6px;color:#2563eb;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:1px;"">Keep pace</p>
                                                    <p style=""margin:0;color:#526173;font-size:13px;line-height:1.6;"">Review your lessons and stay on track with your current course.</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                    <td style=""width:50%;padding:14px 0 14px 14px;vertical-align:top;"">
                                        <table role=""presentation"" style=""width:100%;border-collapse:collapse;background:#ffffff;border:1px solid #e2e8f0;border-radius:12px;"">
                                            <tr>
                                                <td style=""padding:16px;"">
                                                    <p style=""margin:0 0 6px;color:#0f766e;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:1px;"">Next action</p>
                                                    <p style=""margin:0;color:#526173;font-size:13px;line-height:1.6;"">Open EdSkill to continue learning or complete pending activities.</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            <table role=""presentation"" style=""border-collapse:collapse;"">
                                <tr>
                                    <td align=""left"">
                                        <a href=""https://edskill.vercel.app/""
                                           style=""display:inline-block;background:#2563eb;color:#ffffff;font-size:14px;font-weight:700;padding:13px 22px;text-decoration:none;border-radius:10px;"">
                                            Open EdSkill
                                        </a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    {GetCommonFooterHtml()}
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
	}

	// -------------------------------------------------------------------------
	// SHARED PARTIALS
	// -------------------------------------------------------------------------

	private static string GetBrandLogoHtml()
	{
		return @"<table role=""presentation"" style=""border-collapse:collapse;"">
    <tr>
        <td style=""width:38px;height:38px;background:#ffffff;border-radius:10px;text-align:center;vertical-align:middle;color:#0f766e;font-size:16px;font-weight:800;"">Ed</td>
        <td style=""padding-left:10px;color:#ffffff;font-size:21px;font-weight:800;letter-spacing:0;"">EdSkill</td>
    </tr>
</table>";
	}

	private static string GetCommonFooterHtml()
	{
		return @"<tr>
    <td style=""background:#f8fafc;padding:18px 32px;border-top:1px solid #e2e8f0;"">
        <table role=""presentation"" style=""width:100%;border-collapse:collapse;font-family:Arial,'Segoe UI',Tahoma,sans-serif;"">
            <tr>
                <td style=""font-size:12px;color:#64748b;line-height:1.6;"">EdSkill helps you keep lessons, practice, and progress in one place.</td>
                <td align=""right"" style=""font-size:12px;color:#94a3b8;line-height:1.6;"">Automated email. Do not reply.</td>
            </tr>
        </table>
    </td>
</tr>";
	}
}
