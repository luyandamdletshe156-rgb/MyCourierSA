using MyCourierSA.Constants;
using MyCourierSA.Models;
using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MyCourierSA.Services
{
    public class ShipmentEmailService
    {
        // <summary>
        /// Sends the password reset email asynchronously. This method reads SMTP settings
        /// directly from your application's Web.config file.
        /// </summary>
        /// <param name="emailAddress">The recipient's email address.</param>
        /// <param name="callbackUrl">The unique password reset link.</param>
        /// 


        public async Task SendCustomerWelcomeEmailAsync(ApplicationUser user)
        {
            string subject = "Welcome to MyCourierSA - Let's Get Shipping!";
            string body = $@"
        <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; max-width: 600px;'>
            <h2 style='color: #2c3e50;'>Welcome to the Family, {user.Name}!</h2>
            <p>We're thrilled to have you join MyCourierSA. Your account is now active and ready to use.</p>
            
            <h3 style='color: #2980b9;'>What's Next?</h3>
            <ol>
                <li><strong>Top Up Your Wallet:</strong> Go to your dashboard and add funds to your wallet.</li>
                <li><strong>Book a Shipment:</strong> Fill in the pickup and delivery details.</li>
                <li><strong>Track in Real-Time:</strong> Watch your parcel move from your dashboard.</li>
            </ol>

            <div style='background-color: #f4f4f4; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                <p style='margin: 0;'><strong>Account Login:</strong> {user.Email}</p>
            </div>

            <p style='margin-top: 20px;'>If you have any questions, simply reply to this email or visit our support hub.</p>
            
            <hr />
            <p style='font-size: 12px; color: #888;'>Happy Shipping,<br/><strong>The MyCourierSA Team</strong></p>
        </div>";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendWelcomeEmailAsync(ApplicationUser user, string password, string role)
        {
            string subject = "Welcome to MyCourierSA - Your Staff Account Details";

            // Create a professional welcome body
            string body = $@"
        <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; max-width: 600px;'>
            <h2 style='color: #2c3e50;'>Welcome to the Team, {user.Name}!</h2>
            <p>An administrative account has been created for you on the MyCourierSA platform.</p>
            
            <div style='background-color: #f9f9f9; padding: 15px; border-radius: 5px; border-left: 5px solid #3498db;'>
                <p><strong>Access Level:</strong> {role}</p>
                <p><strong>Login Email:</strong> {user.Email}</p>
                <p><strong>Temporary Password:</strong> <span style='font-family: monospace; background: #eee; padding: 2px 5px;'>{password}</span></p>
            </div>

            <p style='margin-top: 20px;'>Please log in to your dashboard to begin your duties.</p>
            <p style='color: #e74c3c; font-size: 13px;'><em>Note: For security reasons, please change your password after your first login.</em></p>
            
            <hr />
            <p style='font-size: 12px; color: #888;'>MyCourierSA Administration System</p>
        </div>";

            await SendEmailAsync(user.Email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpEmail = ConfigurationManager.AppSettings["SmtpEmail"];
                var smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
                var smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
                var smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");

                // Security Fix: Ensures modern encryption is used (TLS 1.2)
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (var smtpClient = new SmtpClient(smtpHost))
                {
                    smtpClient.Port = smtpPort;
                    smtpClient.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpEmail, "MyCourierSA Support"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // If it fails, we see the error in the "Output" window of Visual Studio
                System.Diagnostics.Debug.WriteLine("CRITICAL EMAIL ERROR: " + ex.ToString());
            }
        }

        public async Task SendShipmentCreatedEmailAsync(Shipment shipment, string customerEmail)
        {
            string subject = $"Shipment Created - #{shipment.TrackingNumber}";
            string body = $@"
                <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee;'>
                    <h2>Hello {shipment.SenderName},</h2>
                    <p>Your booking for tracking number <strong>{shipment.TrackingNumber}</strong> is confirmed.</p>
                    <p><strong>Destination:</strong> {shipment.ReceiverCity}</p>
                    <p><strong>Total Paid:</strong> R{shipment.Price:F2}</p>
                    <p>Thank you for using MyCourierSA.</p>
                </div>";

            await SendEmailAsync(customerEmail, subject, body);
        }

        public async Task SendStatusUpdateEmailAsync(Shipment shipment, string customerEmail, string newStatus)
        {
            string statusDescription;

            // Use a traditional switch statement instead of the modern switch expression
            switch (newStatus)
            {
                case AppConstants.ShipmentStatuses.Approved:
                    statusDescription = "has been <strong>Approved</strong> and is waiting for collection.";
                    break;

                case "At Warehouse": // Ensure this matches your Warehouse constant
                    statusDescription = "has arrived at our <strong>Warehouse</strong> for sorting.";
                    break;

                case AppConstants.ShipmentStatuses.Delivered:
                    statusDescription = "has been <strong>Successfully Delivered</strong>!";
                    break;

                default:
                    statusDescription = $"status is now: <strong>{newStatus}</strong>.";
                    break;
            }

            string subject = $"Update for Shipment #{shipment.TrackingNumber}";
            string body = $@"
        <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee;'>
            <h3>Status Update</h3>
            <p>Hello {shipment.SenderName},</p>
            <p>Your parcel (Tracking: {shipment.TrackingNumber}) {statusDescription}</p>
            <p>Login to your portal to see more details.</p>
            <hr />
            <p style='font-size: 12px; color: #888;'>MyCourierSA Automated System</p>
        </div>";

            await SendEmailAsync(customerEmail, subject, body);
        }

            public async Task SendPasswordResetEmailAsync(string emailAddress, string callbackUrl)
            {
                // Build the email content with a nice button
                string subject = "Reset Your MyCourierSA Password";
                string body = $@"
                <div style='font-family: Arial, sans-serif; font-size: 16px; color: #333;'>
                    <h2>Password Reset Request</h2>
                    <p>You recently requested to reset your password for your MyCourierSA account.</p>
                    <p>Please reset your password by clicking the link below. This link is only valid for a short time.</p>
                    <p style='margin: 25px 0;'>
                        <a href='{callbackUrl}' style='display: inline-block; padding: 12px 25px; font-size: 16px; color: white; background-color: #6a11cb; text-decoration: none; border-radius: 5px;'>
                            Reset Password Now
                        </a>
                    </p>
                    <p>If you did not request a password reset, please ignore this email.</p>
                    <hr>
                    <p style='font-size: 12px; color: #999;'>MyCourierSA Automation</p>
                </div>";

                // SmtpClient will AUTOMATICALLY use the settings from your Web.config file
                using (var smtp = new SmtpClient())
                {
                    // The 'from' address will also be picked up from Web.config,
                    // so you don't need to specify it here unless you want to override it.
                    var mailMessage = new MailMessage
                    {
                        To = { emailAddress },
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };

                    // Send the email asynchronously
                    await smtp.SendMailAsync(mailMessage);
                }
            }

            // You can update your other email methods to use this same logic
            
        }


    }
